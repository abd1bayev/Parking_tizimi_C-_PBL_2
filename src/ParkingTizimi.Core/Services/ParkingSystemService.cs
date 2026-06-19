using ParkingTizimi.Core.Interfaces;
using ParkingTizimi.Core.Models;
using ParkingTizimi.Domain.Entities;
using ParkingTizimi.Domain.Enums;
using ParkingTizimi.Shared.Results;
using ParkingTizimi.Shared.Security;
using ParkingTizimi.Shared.Time;
using ParkingTizimi.Shared.Validation;

namespace ParkingTizimi.Core.Services;

public class ParkingSystemService
{
    private readonly IClock _clock;
    private readonly IParkingRepository _repository;
    private ParkingState _state = new();
    private bool _isInitialized;

    public ParkingSystemService(IParkingRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        _state = await _repository.LoadAsync(cancellationToken);
        SeedSlotsIfNeeded();
        _isInitialized = true;
        await _repository.SaveAsync(_state, cancellationToken);
    }

    public bool HasAdmin()
    {
        EnsureInitialized();
        return _state.Users.Any(user => user.Role == UserRole.Admin);
    }

    public IReadOnlyList<ParkingSlot> GetSlots()
    {
        EnsureInitialized();
        return _state.Slots.OrderBy(slot => slot.Code).ToList();
    }

    public IReadOnlyList<User> GetAllUsers()
    {
        EnsureInitialized();
        return _state.Users.OrderBy(user => user.Role).ThenBy(user => user.Username).ToList();
    }

    public IReadOnlyList<Reservation> GetAllReservations()
    {
        EnsureInitialized();
        return _state.Reservations.OrderByDescending(reservation => reservation.CreatedAtUtc).ToList();
    }

    public IReadOnlyList<ParkingSession> GetActiveSessions()
    {
        EnsureInitialized();
        return _state.Sessions.Where(session => !session.IsClosed).OrderBy(session => session.CheckInUtc).ToList();
    }

    public IReadOnlyList<Payment> GetPayments()
    {
        EnsureInitialized();
        return _state.Payments.OrderByDescending(payment => payment.CreatedAtUtc).ToList();
    }

    public IReadOnlyList<Vehicle> GetUserVehicles(Guid userId)
    {
        EnsureInitialized();
        return _state.Vehicles.Where(vehicle => vehicle.OwnerUserId == userId).OrderBy(vehicle => vehicle.PlateNumber).ToList();
    }

    public IReadOnlyList<Reservation> GetUserReservations(Guid userId)
    {
        EnsureInitialized();
        return _state.Reservations.Where(reservation => reservation.UserId == userId).OrderByDescending(reservation => reservation.CreatedAtUtc).ToList();
    }

    public IReadOnlyList<Payment> GetUserPayments(Guid userId)
    {
        EnsureInitialized();
        var sessionIds = _state.Sessions.Where(session => session.UserId == userId).Select(session => session.Id).ToHashSet();
        return _state.Payments.Where(payment => sessionIds.Contains(payment.SessionId)).OrderByDescending(payment => payment.CreatedAtUtc).ToList();
    }

    public OperationResult<User> BootstrapAdmin(string username, string password, string phoneNumber)
    {
        EnsureInitialized();

        if (HasAdmin())
        {
            return OperationResult<User>.Failure("Admin allaqachon mavjud.");
        }

        return CreateUser(username, password, phoneNumber, UserRole.Admin, "Admin yaratildi.");
    }

    public OperationResult<User> RegisterUser(string username, string password, string phoneNumber)
    {
        EnsureInitialized();
        return CreateUser(username, password, phoneNumber, UserRole.User, "Foydalanuvchi yaratildi.");
    }

    public OperationResult<User> CreateOperator(Guid adminUserId, string username, string password, string phoneNumber)
    {
        EnsureInitialized();

        var admin = _state.Users.FirstOrDefault(user => user.Id == adminUserId && user.Role == UserRole.Admin && user.IsActive);
        if (admin is null)
        {
            return OperationResult<User>.Failure("Faqat admin operator yarata oladi.");
        }

        return CreateUser(username, password, phoneNumber, UserRole.Operator, "Operator yaratildi.");
    }

    public async Task PersistAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await _repository.SaveAsync(_state, cancellationToken);
    }

    public OperationResult<User> Login(string username, string password)
    {
        EnsureInitialized();

        if (!TryNormalizeRequired(username, out var normalizedUsername))
        {
            return OperationResult<User>.Failure("Username bo'sh bo'lmasligi kerak.");
        }

        var user = _state.Users.FirstOrDefault(candidate => string.Equals(candidate.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase) && candidate.IsActive);

        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return OperationResult<User>.Failure("Login yoki parol noto'g'ri.");
        }

        return OperationResult<User>.Success(user, "Muvaffaqiyatli login qilindi.");
    }

    public OperationResult<Vehicle> AddVehicle(Guid userId, string plateNumber, string model, string color)
    {
        EnsureInitialized();

        var user = GetActiveUser(userId);
        if (user is null)
        {
            return OperationResult<Vehicle>.Failure("Foydalanuvchi topilmadi.");
        }

        if (!TryNormalizeRequired(plateNumber, out var normalizedPlate))
        {
            return OperationResult<Vehicle>.Failure("Davlat raqami bo'sh bo'lmasligi kerak.");
        }

        if (!TryNormalizeRequired(model, out var normalizedModel))
        {
            return OperationResult<Vehicle>.Failure("Model bo'sh bo'lmasligi kerak.");
        }

        if (!TryNormalizeRequired(color, out var normalizedColor))
        {
            return OperationResult<Vehicle>.Failure("Rang bo'sh bo'lmasligi kerak.");
        }

        normalizedPlate = normalizedPlate.ToUpperInvariant();
        if (_state.Vehicles.Any(vehicle => string.Equals(vehicle.PlateNumber, normalizedPlate, StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult<Vehicle>.Failure("Bunday davlat raqami allaqachon mavjud.");
        }

        var vehicle = new Vehicle
        {
            OwnerUserId = user.Id,
            PlateNumber = normalizedPlate,
            Model = normalizedModel,
            Color = normalizedColor,
            CreatedAtUtc = _clock.UtcNow
        };

        _state.Vehicles.Add(vehicle);
        return OperationResult<Vehicle>.Success(vehicle, "Avtomobil qo'shildi.");
    }

    public OperationResult<Reservation> CreateReservation(Guid userId, Guid vehicleId, string slotCode, DateTime reservedFromUtc, DateTime reservedToUtc)
    {
        EnsureInitialized();

        var user = GetActiveUser(userId);
        if (user is null)
        {
            return OperationResult<Reservation>.Failure("Foydalanuvchi topilmadi.");
        }

        var vehicle = _state.Vehicles.FirstOrDefault(candidate => candidate.Id == vehicleId && candidate.OwnerUserId == userId);
        if (vehicle is null)
        {
            return OperationResult<Reservation>.Failure("Avtomobil topilmadi.");
        }

        if (reservedToUtc <= reservedFromUtc)
        {
            return OperationResult<Reservation>.Failure("Bron vaqti noto'g'ri.");
        }

        if (reservedFromUtc < _clock.UtcNow.AddMinutes(-1))
        {
            return OperationResult<Reservation>.Failure("Bron vaqti o'tmishda bo'lmasligi kerak.");
        }

        var slot = GetSlotByCode(slotCode);
        if (slot is null)
        {
            return OperationResult<Reservation>.Failure("Parking slot topilmadi.");
        }

        if (slot.Status == SlotStatus.Occupied || slot.Status == SlotStatus.OutOfService)
        {
            return OperationResult<Reservation>.Failure("Bu slot hozir bron uchun yaroqsiz.");
        }

        var hasOverlap = _state.Reservations.Any(reservation =>
            reservation.SlotId == slot.Id &&
            reservation.Status == ReservationStatus.Active &&
            reservedFromUtc < reservation.ReservedToUtc &&
            reservation.ReservedFromUtc < reservedToUtc);

        if (hasOverlap)
        {
            return OperationResult<Reservation>.Failure("Bu slot ko'rsatilgan vaqtda bron qilingan.");
        }

        var reservation = new Reservation
        {
            UserId = userId,
            VehicleId = vehicleId,
            SlotId = slot.Id,
            ReservedFromUtc = reservedFromUtc,
            ReservedToUtc = reservedToUtc,
            Status = ReservationStatus.Active,
            CreatedAtUtc = _clock.UtcNow
        };

        _state.Reservations.Add(reservation);
        RecalculateSlotStatus(slot.Id);

        return OperationResult<Reservation>.Success(reservation, "Bron yaratildi.");
    }

    public OperationResult CancelReservation(Guid userId, Guid reservationId)
    {
        EnsureInitialized();

        var reservation = _state.Reservations.FirstOrDefault(candidate => candidate.Id == reservationId && candidate.UserId == userId);
        if (reservation is null)
        {
            return OperationResult.Failure("Bron topilmadi.");
        }

        if (reservation.Status != ReservationStatus.Active)
        {
            return OperationResult.Failure("Faqat faol bron bekor qilinadi.");
        }

        reservation.Status = ReservationStatus.Cancelled;
        RecalculateSlotStatus(reservation.SlotId);

        return OperationResult.Success("Bron bekor qilindi.");
    }

    public OperationResult<ParkingSession> CheckIn(Guid operatorUserId, Guid userId, Guid vehicleId, string slotCode)
    {
        EnsureInitialized();

        if (!HasStaffAccess(operatorUserId))
        {
            return OperationResult<ParkingSession>.Failure("Faqat admin yoki operator check-in qila oladi.");
        }

        var user = GetActiveUser(userId);
        if (user is null)
        {
            return OperationResult<ParkingSession>.Failure("Foydalanuvchi topilmadi.");
        }

        var vehicle = _state.Vehicles.FirstOrDefault(candidate => candidate.Id == vehicleId && candidate.OwnerUserId == userId);
        if (vehicle is null)
        {
            return OperationResult<ParkingSession>.Failure("Avtomobil topilmadi.");
        }

        if (_state.Sessions.Any(session => session.VehicleId == vehicleId && !session.IsClosed))
        {
            return OperationResult<ParkingSession>.Failure("Bu avtomobil allaqachon parkingda.");
        }

        var slot = GetSlotByCode(slotCode);
        if (slot is null)
        {
            return OperationResult<ParkingSession>.Failure("Parking slot topilmadi.");
        }

        if (slot.Status == SlotStatus.Occupied || slot.Status == SlotStatus.OutOfService)
        {
            return OperationResult<ParkingSession>.Failure("Bu slotga check-in qilib bo'lmaydi.");
        }

        if (slot.Status == SlotStatus.Reserved)
        {
            var hasReservation = _state.Reservations.Any(reservation =>
                reservation.SlotId == slot.Id &&
                reservation.UserId == userId &&
                reservation.VehicleId == vehicleId &&
                reservation.Status == ReservationStatus.Active);

            if (!hasReservation)
            {
                return OperationResult<ParkingSession>.Failure("Slot boshqa foydalanuvchi uchun bron qilingan.");
            }
        }

        foreach (var reservation in _state.Reservations.Where(reservation =>
                     reservation.SlotId == slot.Id &&
                     reservation.UserId == userId &&
                     reservation.VehicleId == vehicleId &&
                     reservation.Status == ReservationStatus.Active))
        {
            reservation.Status = ReservationStatus.Completed;
        }

        var session = new ParkingSession
        {
            UserId = userId,
            VehicleId = vehicleId,
            SlotId = slot.Id,
            CheckInUtc = _clock.UtcNow,
            IsClosed = false,
            TotalAmount = 0m
        };

        _state.Sessions.Add(session);
        RecalculateSlotStatus(slot.Id);

        return OperationResult<ParkingSession>.Success(session, "Check-in muvaffaqiyatli bajarildi.");
    }

    public OperationResult<Payment> CheckOut(Guid operatorUserId, Guid sessionId)
    {
        EnsureInitialized();

        if (!HasStaffAccess(operatorUserId))
        {
            return OperationResult<Payment>.Failure("Faqat admin yoki operator check-out qila oladi.");
        }

        var session = _state.Sessions.FirstOrDefault(candidate => candidate.Id == sessionId && !candidate.IsClosed);
        if (session is null)
        {
            return OperationResult<Payment>.Failure("Faol parking sessiya topilmadi.");
        }

        var slot = _state.Slots.FirstOrDefault(candidate => candidate.Id == session.SlotId);
        if (slot is null)
        {
            return OperationResult<Payment>.Failure("Sessiya uchun slot topilmadi.");
        }

        var checkoutTime = _clock.UtcNow;
        var duration = checkoutTime - session.CheckInUtc;
        var billableHours = Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes / 60d));
        var amount = billableHours * slot.HourlyRate;

        session.CheckOutUtc = checkoutTime;
        session.IsClosed = true;
        session.TotalAmount = amount;

        var payment = new Payment
        {
            SessionId = session.Id,
            Amount = amount,
            Status = PaymentStatus.Paid,
            CreatedAtUtc = checkoutTime,
            PaidAtUtc = checkoutTime
        };

        _state.Payments.Add(payment);
        RecalculateSlotStatus(slot.Id);

        return OperationResult<Payment>.Success(payment, $"Check-out yakunlandi. To'lov: {amount:N0} UZS");
    }

    public User? FindUserByUsername(string username)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return _state.Users.FirstOrDefault(user => string.Equals(user.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public Vehicle? FindVehicle(Guid vehicleId)
    {
        EnsureInitialized();
        return _state.Vehicles.FirstOrDefault(vehicle => vehicle.Id == vehicleId);
    }

    public ParkingSlot? FindSlot(Guid slotId)
    {
        EnsureInitialized();
        return _state.Slots.FirstOrDefault(slot => slot.Id == slotId);
    }

    public User? FindUser(Guid userId)
    {
        EnsureInitialized();
        return _state.Users.FirstOrDefault(user => user.Id == userId);
    }

    public OperationResult<User> CreateUser(string username, string password, string phoneNumber, UserRole role, string successMessage)
    {
        if (!TryNormalizeRequired(username, out var normalizedUsername))
        {
            return OperationResult<User>.Failure("Username bo'sh bo'lmasligi kerak.");
        }

        if (_state.Users.Any(user => string.Equals(user.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult<User>.Failure("Bu username band.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Trim().Length < 6)
        {
            return OperationResult<User>.Failure("Parol kamida 6 ta belgidan iborat bo'lishi kerak.");
        }

        if (!PhoneNumberValidator.IsValid(phoneNumber))
        {
            return OperationResult<User>.Failure("Telefon raqam +998XXXXXXXXX formatida bo'lishi kerak.");
        }

        var user = new User
        {
            Username = normalizedUsername,
            PasswordHash = PasswordHasher.Hash(password),
            PhoneNumber = PhoneNumberValidator.Normalize(phoneNumber),
            Role = role,
            CreatedAtUtc = _clock.UtcNow,
            IsActive = true
        };

        _state.Users.Add(user);
        return OperationResult<User>.Success(user, successMessage);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Service ishlatishdan oldin InitializeAsync chaqirilishi kerak.");
        }
    }

    private User? GetActiveUser(Guid userId) => _state.Users.FirstOrDefault(user => user.Id == userId && user.IsActive);

    private bool HasStaffAccess(Guid userId)
    {
        var user = GetActiveUser(userId);
        return user is not null && (user.Role == UserRole.Admin || user.Role == UserRole.Operator);
    }

    private ParkingSlot? GetSlotByCode(string slotCode)
    {
        if (!TryNormalizeRequired(slotCode, out var normalizedCode))
        {
            return null;
        }

        normalizedCode = normalizedCode.ToUpperInvariant();
        return _state.Slots.FirstOrDefault(slot => string.Equals(slot.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryNormalizeRequired(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        normalized = value.Trim();
        return true;
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Qiymat bo'sh bo'lmasligi kerak.", nameof(value));
        }

        return value.Trim();
    }

    private void RecalculateSlotStatus(Guid slotId)
    {
        var slot = _state.Slots.FirstOrDefault(candidate => candidate.Id == slotId);
        if (slot is null || slot.Status == SlotStatus.OutOfService)
        {
            return;
        }

        var hasOpenSession = _state.Sessions.Any(session => session.SlotId == slotId && !session.IsClosed);
        if (hasOpenSession)
        {
            slot.Status = SlotStatus.Occupied;
            return;
        }

        var hasActiveReservation = _state.Reservations.Any(reservation => reservation.SlotId == slotId && reservation.Status == ReservationStatus.Active);
        slot.Status = hasActiveReservation ? SlotStatus.Reserved : SlotStatus.Available;
    }

    private void SeedSlotsIfNeeded()
    {
        if (_state.Slots.Count > 0)
        {
            return;
        }

        var prefixes = new[] { "A", "B" };
        foreach (var prefix in prefixes)
        {
            for (var number = 1; number <= 5; number++)
            {
                _state.Slots.Add(new ParkingSlot
                {
                    Code = $"{prefix}{number}",
                    Status = SlotStatus.Available,
                    HourlyRate = _state.DefaultHourlyRate,
                    CreatedAtUtc = _clock.UtcNow
                });
            }
        }
    }
}