using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Models
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public Guid PlayerId { get; set; }

        [Required]
        public decimal Balance { get; set; }

        public List<Transaction> Transactions { get; set; } = new List<Transaction>();

        [Timestamp]
        public byte[]? RowVersion { get; set; } // used as concurrency token
    }
}
