namespace Concertable.Kernel;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? LastModifiedAt { get; set; }
    string? LastModifiedBy { get; set; }
}
