namespace AvansAfvalAPI.Interfaces;

public interface IAuthenticationService
{
    string? GetCurrentAuthenticatedUserId();
}