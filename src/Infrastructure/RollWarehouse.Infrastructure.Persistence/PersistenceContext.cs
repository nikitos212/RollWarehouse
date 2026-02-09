using Microsoft.EntityFrameworkCore;
using RollWarehouse.Domain.Entities;

namespace RollWarehouse.Infrastructure.Persistence
{
    public class PersistenceContext : DbContext
    {
        public PersistenceContext(DbContextOptions<PersistenceContext> options) : base(options) { }
        public DbSet<Roll> Rolls { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Roll>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.Length).IsRequired();
                b.Property(r => r.Weight).IsRequired();
                b.Property(r => r.DateAdded).IsRequired();
                b.Property(r => r.DateRemoved).IsRequired(false);
            });
        }
    }
}
