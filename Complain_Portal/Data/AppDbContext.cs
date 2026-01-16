using Complain_Portal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Complain_Portal.Data
{
    public class AppDbContext:IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        {
        }

        public DbSet<Issue> Issues { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Additional configurations can be added here if needed

            builder.Entity<Issue>().
                HasOne(i => i.ReporterUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Issue>().
                HasOne(i => i.AssignedOfficial)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}
