namespace ParkingTizimi.Shared.Time;

public interface IClock
{
    DateTime UtcNow { get; }
}