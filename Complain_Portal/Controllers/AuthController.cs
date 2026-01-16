using Complain_Portal.Data;
using Complain_Portal.DTOs;
using Complain_Portal.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace Complain_Portal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration configuration;
        private readonly AppDbContext context;

        public AuthController(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            AppDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.configuration = configuration;
            this.context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterVM payload)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var userExist = await userManager.FindByEmailAsync(payload.Email);
                if (userExist != null)
                {
                    return BadRequest("User already exists");
                }
                var user = new ApplicationUser
                {
                    UserName = payload.Username,
                    Email = payload.Email,
                    FullName = payload.FullName,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                var result = await userManager.CreateAsync(user, payload.Password);
                if (!result.Succeeded)
                    return BadRequest("User creation failed! Please check user details and try again.");

                if (await roleManager.RoleExistsAsync(payload.Role))
                {
                    await userManager.AddToRoleAsync(user, payload.Role);
                }
                else
                {
                    await userManager.AddToRoleAsync(user, "Citizen");
                }

                return Ok(new { Status = "Success", Message = "User created successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginVM payload)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var user = await userManager.FindByNameAsync(payload.Username);
                if (user != null && await userManager.CheckPasswordAsync(user, payload.Password))
                {
                    //create claims and generate JWT token
                    var authClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name,user.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.Id)
                    };
                    var userRoles = await userManager.GetRolesAsync(user);
                    foreach (var role in userRoles)
                    {
                        if (role != null)
                        {
                            authClaims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }

                    var token = CreateJwtToken(authClaims);
                    var refreshToken = GenerateRefreshToken();

                    var refreshtokenEntity = new RefreshToken
                    {
                        JwtId = token.Id,
                        UserId = user.Id,
                        DateAdded = DateTime.UtcNow,
                        DateExpire = DateTime.UtcNow.AddDays(7),
                        IsRevoked = false,
                        Token = refreshToken
                    };
                    await context.RefreshTokens.AddAsync(refreshtokenEntity);
                    await context.SaveChangesAsync();

                    var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
                    return Ok(new
                    {
                        token = jwtToken,
                        RefreshToken = refreshToken,
                        expiration = token.ValidTo
                    });
                }
                else
                {
                    return Unauthorized("Invalid credentials");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        private JwtSecurityToken CreateJwtToken(List<Claim> authClaims)
        {
            //generate signing key 
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]));

            //create the token
            var token = new JwtSecurityToken(
                        issuer: configuration["JwtSettings:Issuer"],
                        audience: configuration["JwtSettings:Audience"],
                        claims: authClaims,
                        expires: DateTime.Now.AddHours(1),
                        signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

            return token;
        }

        public static string GenerateRefreshToken()
        {
            var randomeNumber = new byte[64];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomeNumber);
                return Convert.ToBase64String(randomeNumber);
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody]TokenApiDto tokenModel)
        {
            if (tokenModel is null) return BadRequest("Invalid client request");

            var accessToken = tokenModel.AccessToken;
            var refreshtoken = tokenModel.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null) return BadRequest("Invalid access token or refresh token");

            var storedRefreshToken = context.RefreshTokens.FirstOrDefault(T => T.Token == refreshtoken);
            if(storedRefreshToken == null || storedRefreshToken.DateExpire < DateTime.UtcNow || storedRefreshToken.IsRevoked)
            {
                return BadRequest("Invalid refresh token or it can be revoked or expired");
            }

            var jti = principal.Claims.FirstOrDefault(X => X.Type == JwtRegisteredClaimNames.Jti).Value;
            if(storedRefreshToken.JwtId != jti)
            {
                return BadRequest("Token does not match");
            }

            //delete old refreshtoken
             context.RefreshTokens.Remove(storedRefreshToken);
            await context.SaveChangesAsync();

            var user = await userManager.FindByIdAsync(storedRefreshToken.UserId);
            var newAccessToken = CreateJwtToken(principal.Claims.ToList());
            var newRefreshToken = GenerateRefreshToken();

            var newRefreshTokenEntity = new RefreshToken
            {
                JwtId = newAccessToken.Id,
                IsRevoked = false,
                UserId = user.Id,
                DateAdded = DateTime.UtcNow,
                DateExpire = DateTime.UtcNow.AddDays(7),
                Token = newRefreshToken
            };
            await context.RefreshTokens.AddAsync(newRefreshTokenEntity);
            await context.SaveChangesAsync();
            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                RefreshToken = newRefreshToken,
                Expiration = newAccessToken.ValidTo
            });
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!))
            };

            var tokenhandler = new JwtSecurityTokenHandler();
            var principal = tokenhandler.ValidateToken(token,tokenValidationParameters,out SecurityToken securityToken);

            if(securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid Token"); 
            }
            return principal;
        }

        [HttpGet("test")]
        [Authorize]
        public IActionResult TestToken()
        {
            return Ok("You have successfully entered the VIP zone! Your Token is valid.");
        }
    }
}
