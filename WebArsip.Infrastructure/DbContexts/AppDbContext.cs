using Microsoft.EntityFrameworkCore;
using WebArsip.Core.Entities;

namespace WebArsip.Infrastructure.DbContexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Archive> Archives { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 🔹 Relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithOne(p => p.Role)
                .HasForeignKey(p => p.RoleId);

            modelBuilder.Entity<Document>()
                .HasMany(d => d.Permissions)
                .WithOne(p => p.Document)
                .HasForeignKey(p => p.DocId);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Archive)
                .WithOne(a => a.Document)
                .HasForeignKey<Archive>(a => a.DocId);

            base.OnModelCreating(modelBuilder);

            // =====================================================
            // 🔹 SEEDING DATA AWAL
            // =====================================================

            // 1️⃣ Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Compliance" },
                new Role { RoleId = 3, RoleName = "Audit" },
                new Role { RoleId = 4, RoleName = "Policy" }
            );

            // 2️⃣ Default Admin User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Name = "Administrator",
                    Email = "admin@company.com",
                    Password = "admin123", // ⚠️ TODO: ganti ke hash
                    RoleId = 1
                }
            );

            // 3️⃣ Sample Document (dummy awal supaya Permission FK aman)
            modelBuilder.Entity<Document>().HasData(
                new Document
                {
                    DocId = 1,
                    Title = "Panduan Arsip Awal",
                    Description = "Dokumen contoh awal untuk permission",
                    FilePath = "/uploads/docs/sample.pdf",
                    CreatedDate = DateTime.UtcNow,
                    Status = "Active"
                }
            );

            // 4️⃣ Permissions default sesuai UML
            modelBuilder.Entity<Permission>().HasData(
                // Admin: full access
                new Permission { PermissionId = 1, RoleId = 1, DocId = 1, CanView = true, CanEdit = true, CanDelete = true, CanDownload = true, CanUpload = true },

                // Compliance: view, upload, edit, archive
                new Permission { PermissionId = 2, RoleId = 2, DocId = 1, CanView = true, CanEdit = true, CanDelete = true, CanDownload = false, CanUpload = true },

                // Audit: view, download
                new Permission { PermissionId = 3, RoleId = 3, DocId = 1, CanView = true, CanEdit = false, CanDelete = false, CanDownload = true, CanUpload = false },

                // Policy: view, edit, upload revisi
                new Permission { PermissionId = 4, RoleId = 4, DocId = 1, CanView = true, CanEdit = true, CanDelete = false, CanDownload = false, CanUpload = true }
            );
        }
    }
}