using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebArsip.Core.Entities;

namespace WebArsip.Infrastructure.DbContexts
{
    public class AppDbContext : IdentityDbContext<User, Role, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Document> Documents { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Archive> Archives { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<SerialNumberFormat> SerialNumberFormats { get; set; }
        public DbSet<SerialNumberMonthlyCounter> SerialNumberMonthlyCounters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Document>()
                .HasKey(d => d.DocId);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Document)
                .WithMany(d => d.UserPermissions)
                .HasForeignKey(up => up.DocId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Permission>()
                .HasOne(p => p.Role)
                .WithMany()
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
