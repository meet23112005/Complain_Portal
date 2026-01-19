using Complain_Portal.Middleware;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Complain_Portal.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next,ILogger<ExceptionMiddleware> logger,IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
               //or context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                /*
                 //if we have any custome exception then we can use this approach like we create BaseException for handle certain type of exception.
                context.Response.StatusCode = ex switch
                {
                    BaseException e => (int)e.StatusCode,
                    _ => StatusCodes.Status500InternalServerError
                };
                */
                context.Response.ContentType = "application/json";

                var response = _env.IsDevelopment()
                    ?new {StatusCode = context.Response.StatusCode, Message = ex.Message, StackTrace = ex.StackTrace?.ToString()}
                    :new { StatusCode = context.Response.StatusCode, Message = "Internal Server Error. Please try again later.", StackTrace = "" };

                var json = JsonSerializer.Serialize(response);
                
                await context.Response.WriteAsync(json);
            }
        }

    }
}
