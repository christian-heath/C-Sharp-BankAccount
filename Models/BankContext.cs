using Microsoft.EntityFrameworkCore;

namespace bankaccount.Models
{
    public class BankContext : DbContext
    {
        public BankContext(DbContextOptions<BankContext> options) : base(options) {}
        public  DbSet<User> Users { get; set;}
        public  DbSet<Transaction> Transactions { get; set;}
    }
}