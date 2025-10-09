using Accura_MES.Models;
using Microsoft.EntityFrameworkCore;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Accura_MES.Controllers
{
    public class DatabaseContext : DbContext
    {
        //依賴注入
        //public Db(DbContextOptions<Db> options) : base(options) { }

        public DbSet<Calendar> calendar { get; set; }

        private string ConnectionString { get; }

        public DatabaseContext(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(ConnectionString);
            }
        }
    }
}
