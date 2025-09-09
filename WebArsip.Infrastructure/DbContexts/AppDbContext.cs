using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            // User ↔ Role (Many-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            // Role ↔ Permission (One-to-Many)
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithOne(p => p.Role)
                .HasForeignKey(p => p.RoleId);

            // Document ↔ Permission (One-to-Many)
            modelBuilder.Entity<Document>()
                .HasMany(d => d.Permissions)
                .WithOne(p => p.Document)
                .HasForeignKey(p => p.DocId);

            // Document ↔ Archive (One-to-One)
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Archive)
                .WithOne(a => a.Document)
                .HasForeignKey<Archive>(a => a.DocId);

            base.OnModelCreating(modelBuilder);

            // 🔹 Seeding Role
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Compliance" },
                new Role { RoleId = 3, RoleName = "Audit" },
                new Role { RoleId = 4, RoleName = "Policy" }
            );

            // 🔹 Seeding User (default admin)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Name = "Administrator",
                    Email = "admin@company.com",
                    Password = "admin123", // ⚠️ nanti lebih baik pakai hashing
                    RoleId = 1
                }
            );
        }
    }
}
