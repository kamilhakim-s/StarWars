using Microsoft.EntityFrameworkCore;
using StarWarsAPI.Models;

namespace StarWarsAPI
{
    public class StarWarsContext : DbContext
    {
        public StarWarsContext(DbContextOptions<StarWarsContext> options) : base(options)
        {
        }

        public DbSet<Planet> Planets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Planet>().HasKey(p => p.Name);
        }
    }
}
