using Application.Interfaces;
using Application.Internal;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class ParkingStateStore : IParkingStateStore
{
    private readonly IClock _clock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IParkingRepository _repository;
    private ParkingState _state = new();
    private bool _isInitialized;

    public ParkingStateStore(IParkingRepository repository, IPasswordHasher passwordHasher, IClock clock)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
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
        ParkingStateHelper.SeedZonesAndSlotsIfNeeded(_state, _clock);
        var demoSeeded = DemoDataSeeder.SeedIfEmpty(_state, _passwordHasher, _clock);
        _isInitialized = true;

        if (demoSeeded || (_state.Zones.Count > 0 && (_state.Slots.Count > slotsBefore || slotsBefore == 0)))
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
