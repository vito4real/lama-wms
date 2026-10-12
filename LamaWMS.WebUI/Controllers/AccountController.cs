using LamaWMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LamaWMS.WebUI.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Rolls");

        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string userName,
        string password)
    {
        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Введите логин и пароль.");
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(
            userName,
            password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.Succeeded)
            return RedirectToAction("Index", "Rolls");

        ModelState.AddModelError(
            "", "Неверный логин или пароль.");

        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}