using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using ApiTimer.DbModels;
using ApiTimer.Models;

namespace ApiTimer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MainController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly TimerDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MainController(UserManager<User> userManager, TimerDbContext shopDbContext, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = shopDbContext;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("timer")]
        public async Task<IActionResult> GetTimers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
               return Ok(_context.Timers.Include(x => x.Sound).Where(x => x.User.Id == user.Id).OrderByDescending(x => x.Id).ToList());
            }
            return Ok(new { }   );
        }

        [HttpPost("timer")]
        public async Task<IActionResult> CreateTimer([FromBody] AddTimerModel model)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(0);
            var user = await _userManager.GetUserAsync(User);
            if (model.Date == null)
            {
                timeSpan = new TimeSpan(model.Days, model.Hours, model.Minutes, model.Seconds);
            }
            else
            {
                timeSpan = (TimeSpan)(model.Date - DateTime.Now) + TimeSpan.FromHours(3);
            }

            var timer = new LiveTimer()
            {
                TimeSpan = timeSpan,
                Title = model.Title,
                User = user
            };

            _context.Timers.Add(timer);
            await _context.SaveChangesAsync();

            return Ok(new { time = timeSpan.TotalSeconds, id = timer.Id });
        }

        [HttpDelete("timer/{id}")]
        public async Task<IActionResult> RemoveTimer(int id)
        {
            var timer = await _context.Timers.FirstOrDefaultAsync(x => x.Id == id);

            if (timer == null)
            {
                return NotFound();
            }

            _context.Timers.Remove(timer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Timer removed successfully" });
        }

        [HttpPost("timer/{id}/sound")]
        public async Task<IActionResult> SetTimerSound([FromRoute] int id, [FromForm] IFormFile file)
        {
            var sound = await CreateSound(file);

            var timer = await _context.Timers.Include(x => x.Sound).FirstOrDefaultAsync(x => x.Id == id);

            if (timer == null)
            {
                return NotFound();
            }

            timer.Sound = sound;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Timer sound updated successfully" });
        }

        [HttpGet("timer/{id}")]
        public async Task<IActionResult> GetTimer([FromRoute] int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !_context.Timers.Any(x => x.User.Id == user.Id))
            {
                return Ok(new LiveTimer
                {
                    Sound = null,
                    Title = "",
                    TimeSpan = TimeSpan.FromSeconds(0)
                });
            }

            var timer = await _context.Timers
                .Where(x => x.User.Id == user.Id)
                .Include(x => x.Sound)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (timer == null)
            {
                return NotFound();
            }

            return Ok(timer);
        }

        private async Task<SoundFile> CreateSound(IFormFile file)
        {
            var filename = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var dbFile = new SoundFile
            {
                Filename = filename,
                RootDirectory = "uploads",
            };

            var localFilename = Path.Combine(_webHostEnvironment.WebRootPath, dbFile.RootDirectory, dbFile.Filename);

            using (var localFile = System.IO.File.Open(localFilename, FileMode.OpenOrCreate))
            {
                await file.CopyToAsync(localFile);
            }

            _context.Sounds.Add(dbFile);
            await _context.SaveChangesAsync();

            return dbFile;
        }
    }
}
