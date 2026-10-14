using ApplyFlow.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApplyFlow.Web.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<JobApplication> JobApplications
            => Set<JobApplication>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Identity configures its account tables first.
            base.OnModelCreating(builder);

            var application = builder.Entity<JobApplication>();

            application.Property(item => item.SalaryAmount)
                .HasPrecision(11, 2);

            application.Property(item => item.Currency)
                .IsUnicode(false);

            application.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            application.HasIndex(item => new
            {
                item.UserId,
                item.Status
            });
        }
    }
}