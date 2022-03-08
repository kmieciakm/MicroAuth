using Domain.Contracts;
using Domain.Infrastructure;

namespace Domain.Services;

public class AccountService : IAccountService
{
    private IUserRegistry _UserRepository { get; }

    public AccountService(IUserRegistry userRepository)
    {
        _UserRepository = userRepository;
    }

    public async Task DeleteAccountAsync(Guid guid)
    {
        await _UserRepository.DeleteAsync(guid);
    }
}