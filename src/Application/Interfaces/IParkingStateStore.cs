using Application.Models;

namespace Application.Interfaces;

public interface IParkingStateStore
{
    ParkingState State { get; }
    bool IsInitialized { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task PersistAsync(CancellationToken cancellationToken = default);
}
