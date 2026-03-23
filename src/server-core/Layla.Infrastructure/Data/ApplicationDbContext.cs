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
        }
    }
}