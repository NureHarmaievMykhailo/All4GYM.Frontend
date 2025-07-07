using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace All4GYM.Frontend.Pages;

[Authorize]
public class SubscriptionCancelModel : PageModel
{
    public void OnGet()
    {
        // Тут також можна реалізувати логіку або залишити порожнім
    }
}