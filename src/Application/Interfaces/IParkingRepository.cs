using Application.Models;

namespace Application.Interfaces;

public interface IParkingRepository
{
    Task<ParkingState> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ParkingState state, CancellationToken cancellationToken = default);
}
