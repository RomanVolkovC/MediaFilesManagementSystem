using MediaFilesManagementSystem.Data;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

namespace MediaFilesManagementSystem.Pages;

public class LoginModel : PageModel
{
    private readonly ApplicationContext _context;

    public LoginModel(ApplicationContext context) => _context = context;

    public new User User { get; }

    public IActionResult OnGet() => base.User.Identity?.IsAuthenticated == true ? RedirectToPage("Index") : Page();

    public async Task<IActionResult> OnPost(
        [Bind($"{nameof(Data.User.Name)},{nameof(Data.User.Password)}")] User user)
    {
        if (!ModelState.IsValid)
            return Page();

        var dbUser = _context.Users.Select(u => new { u.Name, u.Password, u.Role })
            .AsNoTracking()
            .FirstOrDefault(u => u.Name == user.Name);

        if (dbUser == null || dbUser.Password != user.Password)
        {
            ModelState.AddModelError("", "Неверный логин или пароль.");
            return Page();
        }

        List<Claim> claims = new()
        {
            new(ClaimsIdentity.DefaultNameClaimType, dbUser.Name),
            new(ClaimsIdentity.DefaultRoleClaimType, dbUser.Role.ToString())
        };
        ClaimsIdentity identity = new(claims, "ApplicationCookie");
        ClaimsPrincipal principal = new(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToPage("Index");
    }
}
