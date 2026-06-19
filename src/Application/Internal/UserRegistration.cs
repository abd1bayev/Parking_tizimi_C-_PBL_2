using Application.Common;
using Application.Interfaces;
using Application.Internal;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Internal;

internal static class UserRegistration
{
    public static OperationResult<User> CreateUser(
        ParkingState state,
        IPasswordHasher passwordHasher,
        IClock clock,
        string username,
        string password,
        string phoneNumber,
        UserRole role,
        string successMessage)
    {
        if (!InputNormalizer.TryNormalizeRequired(username, out var normalizedUsername))
        {
            return OperationResult<User>.Failure("Username bo'sh bo'lmasligi kerak.");
        }

        if (state.Users.Any(user =>
                string.Equals(user.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase)))
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
            PasswordHash = passwordHasher.Hash(password),
            PhoneNumber = PhoneNumberValidator.Normalize(phoneNumber),
            Role = role,
            CreatedAtUtc = clock.UtcNow,
            IsActive = true
        };

        state.Users.Add(user);
        return OperationResult<User>.Success(user, successMessage);
    }
}
