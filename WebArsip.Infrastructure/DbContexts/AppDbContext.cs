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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(d => d.DocId);

                // One-to-Many: Document → Archives
                entity.HasMany(d => d.Archives)
                      .WithOne(a => a.Document)
                      .HasForeignKey(a => a.DocId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Archive>(entity =>
            {
                entity.HasKey(a => a.ArchiveId);
            });
        }

    }
}
