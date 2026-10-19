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

        public DbSet<ApplicationStatusHistory> ApplicationStatusHistories
            => Set<ApplicationStatusHistory>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
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

            var history = builder.Entity<ApplicationStatusHistory>();

            history.HasOne(item => item.JobApplication)
                .WithMany()
                .HasForeignKey(item => item.JobApplicationId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            history.HasIndex(item => new
            {
                item.JobApplicationId,
                item.ChangedAt
            });
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            RecordStatusChanges();

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            RecordStatusChanges();

            return base.SaveChangesAsync(
                acceptAllChangesOnSuccess,
                cancellationToken);
        }

        private void RecordStatusChanges()
        {
            ChangeTracker.DetectChanges();

            var entries = ChangeTracker.Entries<JobApplication>()
                .Where(entry =>
                    entry.State == EntityState.Added
                    || entry.State == EntityState.Modified)
                .ToList();

            var now = DateTimeOffset.UtcNow;

            foreach (var entry in entries)
            {
                var status = entry.Property(application => application.Status);
                var isNew = entry.State == EntityState.Added;

                if (!isNew || entry.Entity.Id != 0)
                {
                    // Avoid adding the same pending event if saving is retried.
                    var alreadyPending = ChangeTracker
                        .Entries<ApplicationStatusHistory>()
                        .Any(history =>
                            history.State == EntityState.Added
                            && ReferenceEquals(
                                history.Entity.JobApplication,
                                entry.Entity)
                            && history.Entity.NewStatus == status.CurrentValue);

                    if (alreadyPending)
                    {
                        continue;
                    }
                }

                if (!isNew && status.OriginalValue == status.CurrentValue)
                {
                    continue;
                }

                ApplicationStatusHistories.Add(new ApplicationStatusHistory
                {
                    JobApplication = entry.Entity,
                    PreviousStatus = isNew ? null : status.OriginalValue,
                    NewStatus = status.CurrentValue,
                    ChangedAt = now
                });
            }
        }
    }
}