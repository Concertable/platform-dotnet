namespace Concertable.Kernel.Identity;

public interface IUser
{
    Guid Id { get; }
    string Email { get; }
    Role Role { get; }
    double? Latitude { get; }
    double? Longitude { get; }
    string? County { get; }
    string? Town { get; }
    string BaseUrl { get; }
    bool IsEmailVerified { get; }
}
