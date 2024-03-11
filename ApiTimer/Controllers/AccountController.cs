using ApiTimer.DbModels;
using ApiTimer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace ApiTimer.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TimerDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, TimerDbContext shopDbContext, IWebHostEnvironment webHostEnvironment, RoleManager<IdentityRole<int>> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = shopDbContext;
        _webHostEnvironment = webHostEnvironment;
        _roleManager = roleManager;
    }
    private string GenerateJwtToken(User user)
    {
        var jwtHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(System.IO.File.ReadAllText("D:\\SecureFiles/jwtsecret.txt"));
        var jwtDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.Now.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        try {
            var token = jwtHandler.CreateToken(jwtDescriptor);
            var jwtToken = jwtHandler.WriteToken(token);
            return jwtToken;
        }
      catch(Exception e)
        {
            Console.WriteLine(e);
            return null; 
        }
    }

    public string GetFirstPartOfEmail(string email)
    {

        if (email != null)
        {
            string[] emailParts = email.Split('@');

            if (emailParts.Length >= 1)
            {
                // The first part of the email is in emailParts[0]
                string firstPartOfEmail = emailParts[0];

                return firstPartOfEmail;
            }
        }
        return "";
    }

    [HttpPost("Register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        foreach (var key in ModelState.Keys)
        {
            var entry = ModelState[key];
            if (entry.Errors.Any())
            {
                foreach (var error in entry.Errors)
                {
                    Console.WriteLine($"Property: {key}, Error: {error.ErrorMessage} ");
                }
            }
        }

        if (ModelState.IsValid)
        {
            var existingLogin = await _userManager.FindByNameAsync(model.Login);
            if (existingLogin != null)
            {
                ModelState.AddModelError(string.Empty, "The login is already in use.");
            }

            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError(string.Empty, "The email is already in use.");
            }
            
            if (ModelState.ErrorCount > 0)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { Errors = errors });
            }

            var user = new User { VisibleName = model.Login, UserName = model.Login, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var existing = _context.Users.First(x => x.UserName == user.UserName);
                var jwtToken = GenerateJwtToken(existing);
                return Ok(new 
                {
                    Success = true,
                    Error = String.Empty,
                    Token = jwtToken
                });

            }


            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return BadRequest(new { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
    }


    [HttpGet]
    public IActionResult Login()
    {
        return Ok();

    }
    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (ModelState.IsValid)
        {
            var existingEmail = await _userManager.FindByEmailAsync(model.LoginOrEmail);
            var existingLogin = await _userManager.FindByNameAsync(model.LoginOrEmail);
            if (existingLogin == null && existingEmail == null)
            {
                return BadRequest(new { Message = "", Error = "No Such Login Exists" });
            }
            if (existingEmail != null) { model.LoginOrEmail = existingEmail.UserName; }

            var result = await _signInManager.PasswordSignInAsync(model.LoginOrEmail, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = _context.Users.FirstOrDefault(x => x.UserName == model.LoginOrEmail);
                
                var editPageUrl = "/";

                if (!string.IsNullOrEmpty(editPageUrl))
                {
                  //  var token = GenerateJwtToken(user);
                   // Response.Cookies.Append("jwt", token);

                    return Ok(new 
                    {
                        Success = true,
                        Error = String.Empty,
                        Token = "",
                        Message = "Success!"
                    });
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return BadRequest(new { Message = "", Error = "Wrong Password!" });
            }
        }

        return Ok(model);
    }

    [HttpPost("Logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { Message = "Success!" });
    }
    [HttpPost("GoogleLogin")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action("GoogleLoginCallback", "Account", null, protocol: HttpContext.Request.Scheme);
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return Challenge(properties, "Google");
    }

    [HttpGet("GoogleLoginCallback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLoginCallback()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction("Login");
        }
        var user = await _userManager.FindByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email));
        var login = GetFirstPartOfEmail(info.Principal.FindFirstValue(ClaimTypes.Email));
        if (user == null)
        {
            user = new User
            {
                UserName = login,
                Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                VisibleName = info.Principal.FindFirstValue(ClaimTypes.Name)
            };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return RedirectToAction("Login");
            }
        }
        await _signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Timer", "Timer");
    }


}
