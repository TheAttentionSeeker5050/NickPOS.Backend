using Microsoft.EntityFrameworkCore;
using NickPOS.Backend.Models;

namespace NickPOS.Backend.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
        public DbSet<UserModel> Users { get; set; } = default!;
    }
}