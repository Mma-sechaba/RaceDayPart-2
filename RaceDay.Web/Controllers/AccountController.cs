using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public sealed class AccountController(RaceDayApiClient api) : Controller
{
    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<object, LoginResponseViewModel>("api/auth/login", new { model.Email, model.Password });
        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError("", result.Error ?? "Unable to sign in.");
            return View(model);
        }
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("UserId", result.Value.UserId);
        HttpContext.Session.SetString("Role", result.Value.Role);
        HttpContext.Session.SetString("Email", result.Value.Email);
        HttpContext.Session.SetString("FullName", result.Value.FullName ?? model.Email);
        if (!string.IsNullOrWhiteSpace(result.Value.ProfilePictureUrl))
            HttpContext.Session.SetString("ProfilePictureUrl", result.Value.ProfilePictureUrl);
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == "Participant" && model.DateOfBirth is null)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth is required for participants.");
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<RegisterViewModel, object>("api/auth/register", model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error ?? "Unable to create your account.");
            return View(model);
        }
        TempData["Notice"] = "Account created. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, SessionAuthorize]
    public async Task<IActionResult> Logout()
    {
        await api.PostAsync<object, object>("api/auth/logout", new { });
        HttpContext.Session.Clear();
        Response.Cookies.Delete(".RaceDay.Session", new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Path = "/" });
        TempData["Notice"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, SessionAuthorize]
    public async Task<IActionResult> Profile()
    {
        var result = await api.GetAsync<ProfileViewModel>("api/profile");
        if (!result.Succeeded || result.Value is null)
        {
            TempData["Notice"] = result.Error ?? "Profile unavailable.";
            return RedirectToAction("Index", "Home");
        }
        return View(result.Value);
    }

    [HttpPost, SessionAuthorize]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PutAsync<ProfileViewModel, object>("api/profile", model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error ?? "Unable to update your profile.");
            return View(model);
        }
        HttpContext.Session.SetString("Email", model.Email);
        HttpContext.Session.SetString("FullName", $"{model.FirstName} {model.LastName}");
        model.ProfilePictureUrl = HttpContext.Session.GetString("ProfilePictureUrl");
        TempData["Notice"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, SessionAuthorize]
    [RequestSizeLimit(5 * 1024 * 1024 + 1024)]
    public async Task<IActionResult> UploadProfilePicture(ImageUploadViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.File is null || model.File.Length == 0)
        {
            TempData["Notice"] = "Choose a valid image to upload.";
            return RedirectToAction(nameof(Profile));
        }
        var result = await api.UploadFileAsync<ProfilePictureResponseViewModel>("api/users/me/profile-picture", model.File, cancellationToken);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value?.ProfilePictureUrl))
        {
            TempData["Notice"] = result.Error ?? "Unable to upload your profile picture.";
            return RedirectToAction(nameof(Profile));
        }
        HttpContext.Session.SetString("ProfilePictureUrl", result.Value.ProfilePictureUrl);
        TempData["Notice"] = "Your profile picture has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet, SessionAuthorize]
    public IActionResult Password() => View(new PasswordFormViewModel());

    [HttpPost, SessionAuthorize]
    public async Task<IActionResult> Password(PasswordFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PutAsync<PasswordFormViewModel, object>("api/profile/password", model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error ?? "Unable to change your password.");
            return View(model);
        }
        TempData["Notice"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }

}
