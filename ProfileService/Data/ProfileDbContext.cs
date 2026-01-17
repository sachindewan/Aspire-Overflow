using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace ProfileService.Data
{
    public class ProfileDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Models.UserProfile> Profiles { set;get; }
    }
}
