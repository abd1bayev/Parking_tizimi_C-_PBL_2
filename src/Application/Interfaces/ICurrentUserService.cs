using Domain.Entities;

namespace Application.Interfaces;

public interface ICurrentUserService
{
    User? CurrentUser { get; }
    void SetCurrentUser(User? user);
    void SignOut();
}
