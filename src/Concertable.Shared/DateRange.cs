namespace Concertable.Shared;

public record DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public DateRange(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new DomainException("End must be after Start.");

        Start = start;
        End = end;
    }

    public DateRange ChangeStart(DateTime start) => new(start, End);

    public DateRange ChangeEnd(DateTime end) => new(Start, end);
}
