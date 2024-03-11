using ApiTimer.DbModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApiTimer.Controllers;

public class ContentController : Controller
{
    private readonly TimerDbContext _context;
    private readonly UserManager<User> _userManager;

    public ContentController(TimerDbContext context, UserManager<User> userManager)
    {
        _userManager = userManager;
        _context = context;
    }
    [HttpGet("Content/logout-navbar")]
    public async Task<IActionResult> LogoutNavbar()
    {
        return PartialView("~/Views/Content/NavBar.cshtml");
    }
    [HttpGet("Content/auth-navbar")]
    public async Task<IActionResult> AuthNavbar()
    {
        var user = _userManager.GetUserAsync(User);
        return PartialView("~/Views/Content/NavBarAuthorize.cshtml", user);
    }
}
