using OpsBoard.Application.Abstractions;
using OpsBoard.Application.Common;

namespace OpsBoard.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserLookup _userLookup;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUser _currentUser;

    public AuthService(
        IUserLookup userLookup,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ICurrentUser currentUser)
    {
        _userLookup = userLookup;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _currentUser = currentUser;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userLookup.FindActiveByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw AppException.Unauthorized("Invalid email or password.");
        }

        var token = _jwtTokenService.CreateToken(user);
        return new LoginResponse(
            token.AccessToken,
            token.ExpiresAtUtc,
            new CurrentUserResponse(user.Id, user.FullName, user.Email, user.Role));
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw AppException.Unauthorized();
        }

        var user = await _userLookup.FindActiveByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user is null)
        {
            throw AppException.Unauthorized();
        }

        return new CurrentUserResponse(user.Id, user.FullName, user.Email, user.Role);
    }
}
