using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    public User? CurrentUser { get; private set; }

    public void SetCurrentUser(User? user) => CurrentUser = user;

    public void SignOut() => CurrentUser = null;
}
