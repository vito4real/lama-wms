using LamaWMS.Infrastructure;
using LamaWMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LamaWMS.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private static readonly string[] Roles = ["Admin", "Operator", "Viewer"];

    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _context;

    public UsersController(
        UserManager<ApplicationUser> users,
        ApplicationDbContext context)
    {
        _users = users;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _users.Users
            .OrderBy(x => x.UserName)
            .ToListAsync();

        var roles = new Dictionary<string, string>();

        foreach (var user in users)
        {
            roles[user.Id] =
                (await _users.GetRolesAsync(user)).FirstOrDefault() ?? "—";
        }

        ViewBag.Roles = roles;

        return View(users);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string fullName,
        string login,
        string password,
        string role)
    {
        fullName = fullName?.Trim() ?? "";
        login = login?.Trim() ?? "";

        if (fullName.Length == 0 ||
            login.Length == 0 ||
            string.IsNullOrEmpty(password) ||
            !Roles.Contains(role))
        {
            ModelState.AddModelError(
                "",
                "Заполните ФИО, логин, пароль и выберите роль.");

            return View();
        }

        if (await _users.FindByNameAsync(login) != null)
        {
            ModelState.AddModelError("", "Этот логин уже занят.");
            return View();
        }

        var user = new ApplicationUser
        {
            UserName = login,
            FullName = fullName
        };

        var result = await _users.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View();
        }

        result = await _users.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            await _users.DeleteAsync(user);
            AddErrors(result);
            return View();
        }

        TempData["Success"] = "Сотрудник создан.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _users.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        ViewBag.Role =
            (await _users.GetRolesAsync(user)).FirstOrDefault() ?? "Viewer";

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        string fullName,
        string role)
    {
        var user = await _users.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(fullName) ||
            !Roles.Contains(role))
        {
            ModelState.AddModelError(
                "",
                "Укажите ФИО и корректную роль.");

            ViewBag.Role = role;

            return View(user);
        }

        var oldRoles = await _users.GetRolesAsync(user);

        if (oldRoles.Contains("Admin") &&
            role != "Admin" &&
            (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier) ||
             !await HasOtherActiveAdminAsync(user.Id)))
        {
            ModelState.AddModelError(
                "",
                "Нельзя снять роль с собственного или последнего активного администратора.");

            ViewBag.Role = oldRoles.FirstOrDefault();

            return View(user);
        }

        user.FullName = fullName.Trim();

        var result = await _users.UpdateAsync(user);

        if (!result.Succeeded)
        {
            AddErrors(result);
            ViewBag.Role = role;
            return View(user);
        }

        if (!oldRoles.Contains(role))
        {
            result = await _users.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                AddErrors(result);
                ViewBag.Role = role;
                return View(user);
            }

            result = await _users.RemoveFromRolesAsync(user, oldRoles);

            if (!result.Succeeded)
            {
                AddErrors(result);
                ViewBag.Role = role;
                return View(user);
            }
        }

        TempData["Success"] = "Данные обновлены.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _users.FindByIdAsync(id);

        return user == null ? NotFound() : View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        string id,
        string password)
    {
        var user = await _users.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        if (string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Укажите новый пароль.");
            return View(user);
        }

        var token = await _users.GeneratePasswordResetTokenAsync(user);

        var result = await _users.ResetPasswordAsync(
            user,
            token,
            password);

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(user);
        }

        await _users.UpdateSecurityStampAsync(user);

        TempData["Success"] = "Пароль изменён.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _users.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["Error"] =
                "Нельзя блокировать собственную учётную запись.";

            return RedirectToAction(nameof(Index));
        }

        var locked = await _users.IsLockedOutAsync(user);

        if (!locked &&
            await _users.IsInRoleAsync(user, "Admin") &&
            !await HasOtherActiveAdminAsync(user.Id))
        {
            TempData["Error"] =
                "Нельзя блокировать последнего активного администратора.";

            return RedirectToAction(nameof(Index));
        }

        var result = await _users.SetLockoutEndDateAsync(
            user,
            locked ? null : DateTimeOffset.MaxValue);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(
                " ",
                result.Errors.Select(x => x.Description));
        }
        else
        {
            if (!locked)
                await _users.UpdateSecurityStampAsync(user);

            TempData["Success"] = locked
                ? "Учётная запись разблокирована."
                : "Учётная запись заблокирована.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _users.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        // Запрещаем удаление собственной учётной записи.
        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["Error"] =
                "Нельзя удалить собственную учётную запись.";

            return RedirectToAction(nameof(Index));
        }

        // Запрещаем удаление последнего активного администратора.
        if (await _users.IsInRoleAsync(user, "Admin") &&
            !await HasOtherActiveAdminAsync(user.Id))
        {
            TempData["Error"] =
                "Нельзя удалить последнего активного администратора.";

            return RedirectToAction(nameof(Index));
        }

        // Проверяем наличие складских транзакций.
        var hasTransactions = await _context.RollTransactions
            .AnyAsync(x => x.UserId == user.Id);

        // Проверяем наличие действующих резервов.
        var hasReservations = await _context.RollReservations
            .AnyAsync(x => x.CreatedByUserId == user.Id);

        if (hasTransactions || hasReservations)
        {
            TempData["Error"] =
                "Нельзя удалить сотрудника, у которого есть " +
                "складские операции или действующие резервы. " +
                "Вместо удаления заблокируйте его учётную запись.";

            return RedirectToAction(nameof(Index));
        }

        // Удаляем пользователя через ASP.NET Core Identity.
        var result = await _users.DeleteAsync(user);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(
                " ",
                result.Errors.Select(x => x.Description));

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            $"Пользователь «{user.UserName}» удалён.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasOtherActiveAdminAsync(string excludedId)
    {
        var admins = await _users.GetUsersInRoleAsync("Admin");

        foreach (var admin in admins)
        {
            if (admin.Id != excludedId &&
                !await _users.IsLockedOutAsync(admin))
            {
                return true;
            }
        }

        return false;
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }
    }
}
