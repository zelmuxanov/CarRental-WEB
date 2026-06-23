using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using CarRental.Web.ViewModels.Account;
using CarRental.BLL.Interfaces.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RateLimiting;

namespace CarRental.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IUserService userService,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
    }

    #region Регистрация
    
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!model.BirthDate.HasValue)
            {
                ModelState.AddModelError("BirthDate", "Дата рождения обязательна");
                return View(model);
            }

            if (!model.DrivingExperience.HasValue)
            {
                ModelState.AddModelError("DrivingExperience", "Стаж вождения обязателен");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Пароль обязателен");
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MiddleName = model.MiddleName,
                PhoneNumber = model.PhoneNumber,
                BirthDate = new DateTime(model.BirthDate.Value.Year, model.BirthDate.Value.Month, model.BirthDate.Value.Day, 0, 0, 0, DateTimeKind.Utc),
                DrivingExperience = model.DrivingExperience.Value,
                Status = UserStatus.Pending,
                RegistrationDate = DateTime.UtcNow,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Пользователь зарегистрирован: {Email}", model.Email);
                var savedUser = await _userManager.FindByEmailAsync(model.Email);

                if (savedUser != null)
                {
                    await _userManager.AddToRoleAsync(savedUser, "User");
                    TempData["NewUserId"] = savedUser.Id.ToString();
                    TempData["RegistrationEmail"] = savedUser.Email;
                    return RedirectToAction("ProcessNewRegistration", "EmailConfirmation");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Пользователь не был сохранен в базу данных");
                }
            }
            else
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при регистрации пользователя {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при регистрации. Попробуйте позже.");
            return View(model);
        }
    }

    #endregion

    #region Вход 
    
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        try
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Пароль обязателен");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Намеренно расплывчатое сообщение — не раскрываем существование email
                ModelState.AddModelError(string.Empty, "Неверный email или пароль");
                return View(model);
            }

            if (user.Status == UserStatus.Blocked)
            {
                ModelState.AddModelError(string.Empty, "Ваш аккаунт заблокирован. Обратитесь к администратору.");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Email не подтвержден. Проверьте вашу почту или запросите новое письмо.");
                TempData["ShowResendLink"] = true;
                TempData["UserEmail"] = user.Email;
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Пользователь {Email} вошёл в систему", model.Email);
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("Аккаунт {Email} заблокирован после неудачных попыток входа", model.Email);
                ModelState.AddModelError(string.Empty, "Аккаунт временно заблокирован. Попробуйте через 15 минут.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Неверный email или пароль");
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при входе пользователя {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при входе в систему");
            return View(model);
        }
    }

    #endregion

    #region Выход
    
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        try
        {
            Console.WriteLine("🔄 GET LOGOUT CALLED - User: " + User?.Identity?.Name);
            
            await _signInManager.SignOutAsync();
            
            Console.WriteLine("✅ GET LOGOUT COMPLETED - User signed out");
            
            // ✅ ДОБАВЛЯЕМ ЗАЩИТНЫЕ ЗАГОЛОВКИ
            Response.Headers["Cache-Control"] = "no-cache, no-store";
            Response.Headers["Pragma"] = "no-cache";
            
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 GET LOGOUT ERROR: {ex.Message}");
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutPost()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Пользователь вышел из системы");
        return RedirectToAction("Index", "Home");
    }

    #endregion

    #region Вспомогательные методы
    
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        else
            return RedirectToAction("Index", "Home");
    }

    #endregion
}