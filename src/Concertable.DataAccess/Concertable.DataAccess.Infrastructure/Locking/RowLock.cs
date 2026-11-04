namespace Concertable.DataAccess.Infrastructure.Locking;

public enum RowLock
{
    KeyShare,
    Share,
    NoKeyUpdate,
    Update
}
