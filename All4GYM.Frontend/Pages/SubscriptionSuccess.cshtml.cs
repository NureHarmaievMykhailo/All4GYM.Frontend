using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

[Authorize(Roles = "User")]
public class SubscriptionSuccessModel : PageModel
{
    public bool IsConfirmed { get; set; } = false;

    public void OnGet()
    {
        // Тут можна показати повідомлення "Оплата успішна"
        IsConfirmed = true;
    }
}