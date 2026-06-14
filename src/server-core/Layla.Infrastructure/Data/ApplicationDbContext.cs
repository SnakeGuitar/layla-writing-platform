using Layla.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Layla.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectRole> ProjectRoles { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
        public DbSet<Donation> Donations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProjectRole>()
                .HasKey(pr => new { pr.ProjectId, pr.AppUserId });

            builder.Entity<ProjectRole>()
                .HasOne(pr => pr.Project)
                .WithMany(p => p.Roles)
                .HasForeignKey(pr => pr.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a project removes all its roles

            builder.Entity<ProjectRole>()
                .HasOne(pr => pr.AppUser)
                .WithMany()
                .HasForeignKey(pr => pr.AppUserId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a user removes their project memberships

            builder.Entity<Project>()
                .HasIndex(p => p.IsPublic);

            builder.Entity<Project>()
                .HasIndex(p => p.UpdatedAt);

            builder.Entity<ProjectRole>()
                .HasIndex(pr => pr.AppUserId);

            builder.Entity<ProjectRole>()
                .HasIndex(pr => pr.ProjectId);

            builder.Entity<OutboxMessage>()
                .HasIndex(om => om.Processed);

            builder.Entity<Donation>()
                .HasOne(d => d.Project)
                .WithMany(p => p.Donations)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Donation>()
                .HasOne(d => d.DonorUser)
                .WithMany()
                .HasForeignKey(d => d.DonorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Donation>()
                .Property(d => d.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Donation>()
                .Property(d => d.Currency)
                .HasMaxLength(3);

            builder.Entity<Donation>()
                .Property(d => d.Status)
                .HasMaxLength(24);

            builder.Entity<Donation>()
                .Property(d => d.PayPalOrderId)
                .HasMaxLength(128);

            builder.Entity<Donation>()
                .Property(d => d.PayPalCaptureId)
                .HasMaxLength(128);

            builder.Entity<Donation>()
                .HasIndex(d => d.ProjectId);

            builder.Entity<Donation>()
                .HasIndex(d => d.DonorUserId);

            builder.Entity<Donation>()
                .HasIndex(d => d.PayPalOrderId)
                .IsUnique();
        }
    }
}
