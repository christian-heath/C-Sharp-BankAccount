using Microsoft.EntityFrameworkCore;

namespace bankaccount.Models
{
    public class BankContext : DbContext
    {
        // Created a BankContex to encompass all of the models into one Context which can be linked to the Db.
        public BankContext(DbContextOptions<BankContext> options) : base(options) {}
        public  DbSet<User> Users { get; set;}
        public  DbSet<Transaction> Transactions { get; set;}
    }
}