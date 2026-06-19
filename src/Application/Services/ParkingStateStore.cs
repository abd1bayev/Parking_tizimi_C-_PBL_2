using Application.Interfaces;
using Application.Internal;
using Application.Models;

namespace Application.Services;

public sealed class ParkingStateStore : IParkingStateStore
{
    private readonly IClock _clock;
    private readonly IParkingRepository _repository;
    private ParkingState _state = new();
    private bool _isInitialized;

    public ParkingStateStore(IParkingRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public ParkingState State => _state;

    public bool IsInitialized => _isInitialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        _state = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var slotsBefore = _state.Slots.Count;
        ParkingStateHelper.SeedSlotsIfNeeded(_state, _clock);
        _isInitialized = true;

        if (_state.Slots.Count > slotsBefore)
        {
            await _repository.SaveAsync(_state, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task PersistAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await _repository.SaveAsync(_state, cancellationToken).ConfigureAwait(false);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("StateStore ishlatishdan oldin InitializeAsync chaqirilishi kerak.");
        }
    }
}
