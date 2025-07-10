using System.Security.Claims;

namespace All4GYM.Frontend.Helpers
{
    public static class SubscriptionHelper
    {
        public static SubscriptionTier GetTierFromClaims(ClaimsPrincipal user)
        {
            var tierClaim = user.FindFirst("CustomTier")?.Value;
            return Enum.TryParse(tierClaim, true, out SubscriptionTier tier)
                ? tier
                : SubscriptionTier.Basic;
        }

        public static bool HasMinimumTier(ClaimsPrincipal user, SubscriptionTier required)
        {
            return GetTierFromClaims(user) >= required;
        }
        
        public static bool HasActiveSubscription(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.UserData)?.Value;
            return bool.TryParse(claim, out var result) && result;
        }
    }
}