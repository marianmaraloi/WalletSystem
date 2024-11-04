using Microsoft.EntityFrameworkCore;
using WalletSystem.Models;

namespace WalletSystem.Data
{
    public class WalletContext : DbContext
    {
        public WalletContext(DbContextOptions<WalletContext> options) : base(options) { }

        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Wallet>()
                .Property(w => w.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.WalletId);

            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.PlayerId)
                .IsUnique();

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.IdempotencyKey)
                .IsUnique();
        }
    }
}
