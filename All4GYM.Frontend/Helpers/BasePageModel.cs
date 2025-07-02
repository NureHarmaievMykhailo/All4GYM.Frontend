using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace All4GYM.Frontend.Helpers;

public class BasePageModel : PageModel
{
    public int? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }

    public override void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context)
    {
        var jwt = Request.Cookies["jwt"];

        if (!string.IsNullOrEmpty(jwt))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);

                var idClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
                var emailClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

                if (idClaim != null && int.TryParse(idClaim.Value, out var id))
                {
                    UserId = id;
                }

                Email = emailClaim?.Value;
                Role = roleClaim?.Value;

                Console.WriteLine($"🧠 Extracted from token: ID={UserId}, Email={Email}, Role={Role}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Failed to parse JWT: " + ex.Message);
            }
        }

        base.OnPageHandlerExecuting(context);
    }
}