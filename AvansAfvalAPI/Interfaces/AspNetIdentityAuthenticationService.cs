using System.Security.Claims;

namespace AvansAfvalAPI.Interfaces;

public class AspNetIdentityAuthenticationService : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AspNetIdentityAuthenticationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string GetCurrentAuthenticatedUserId()
    {
        // Returns the aspnet_User.Id of the authenticated user
        return _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}