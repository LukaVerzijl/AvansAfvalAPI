using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AvansAfvalAPI.Models;

namespace AvansAfvalAPI.Database;

    public class DatabaseContext(DbContextOptions<DatabaseContext> options) : IdentityDbContext<IdentityUser>(options)
    {
        public DbSet<Trash> Trash { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<IdentityUser>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique();

            modelBuilder.Entity<Trash>()
                .Property(t => t.ExternalParameters)
                .HasColumnType("jsonb");
        }
    }
