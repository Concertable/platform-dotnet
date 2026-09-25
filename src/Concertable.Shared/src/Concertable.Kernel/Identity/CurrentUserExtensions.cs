namespace Concertable.Kernel.Identity;

public static class CurrentUserExtensions
{
    extension(ICurrentUser currentUser)
    {
        public Guid GetId() =>
            currentUser.Id ?? throw new UnauthorizedAccessException("User not authenticated.");
    }
}
