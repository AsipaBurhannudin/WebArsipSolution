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

                // Relasi One to Many: Document ke archive
                entity.HasMany(d => d.Archives)
                      .WithOne(a => a.Document)
                      .HasForeignKey(a => a.DocId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Archive>(entity =>
            {
                entity.HasKey(a => a.ArchiveId);
            });

            // Migrasi Seeding data ke DB
            modelBuilder.Entity<Permission>().HasData(
                // Admin (full akses)
                new Permission { PermissionId = 1, RoleId = 1, DocId = 2, CanView = true, CanUpload = true, CanEdit = true, CanDelete = true },

                // Compliance (view + archive/delete)
                new Permission { PermissionId = 2, RoleId = 2, DocId = 2, CanView = true, CanUpload = false, CanEdit = false, CanDelete = true },

                // Audit (hanya view)
                new Permission { PermissionId = 3, RoleId = 3, DocId = 2, CanView = true, CanUpload = false, CanEdit = false, CanDelete = false },

                // Policy (view + upload + edit)
                new Permission { PermissionId = 4, RoleId = 4, DocId = 2, CanView = true, CanUpload = true, CanEdit = true, CanDelete = false }
            );
        }
    }

    }
