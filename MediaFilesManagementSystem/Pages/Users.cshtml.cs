using MediaFilesManagementSystem.Data;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MediaFilesManagementSystem.Pages;

public class UsersModel : PageModel
{
    public UsersModel(ApplicationContext context) => Context = context;

    public ApplicationContext Context { get; }

    public new User User { get; }

    public IActionResult OnPostAdd(
        [Bind(nameof(Data.User.Name), nameof(Data.User.Password), nameof(Data.User.Role))] User user)
    {
        if (ModelState.IsValid)
        {
            Context.Entry(user).State = EntityState.Added;

            try
            {
                _ = Context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", $"Ошибка добавления нового пользователя: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        return Page();
    }

    public IActionResult OnPostEdit([FromRoute] int? id,
        [Bind(nameof(Data.User.Name), nameof(Data.User.Password), nameof(Data.User.Role))] User user)
    {
        if (!id.HasValue)
            return NotFound();

        if (ModelState.IsValid)
        {
            user.Id = id.Value;
            Context.Entry(user).State = EntityState.Modified;

            try
            {
                _ = Context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", $"Ошибка обновления пользователя {user.Id}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        return Page();
    }

    public IActionResult OnGetDelete([FromRoute] int? id)
    {
        if (!id.HasValue)
            return NotFound();

        Context.Entry<User>(new() { Id = id.Value }).State = EntityState.Deleted;

        try
        {
            _ = Context.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError("", $"Ошибка удаления пользователя {id}: {ex.InnerException?.Message ?? ex.Message}");
        }

        return Page();
    }
}
