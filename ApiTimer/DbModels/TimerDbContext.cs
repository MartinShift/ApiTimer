using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApiTimer.DbModels
{
    public class TimerDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {

        public TimerDbContext(DbContextOptions<TimerDbContext> options) : base(options)
        {

        }
        public TimerDbContext() : base() { }

        public virtual DbSet<SoundFile> Sounds { get; set; }
        public virtual DbSet<LiveTimer> Timers { get; set; }
        public virtual DbSet<ImageFile> Images { get; set; }
    }
}
