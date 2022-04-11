using Domain.Contracts;
using Domain.Exceptions;
using Domain.Infrastructure;
using Domain.Models;
using Microsoft.Extensions.Options;

namespace Domain.Services;

public class AuthenticationService : IAuthenticationService
{
    private IUserRegistry _UserRepository { get; }
    private ITokenService _TokenService { get; }
    private IAuthorizationService _AuthorizationService { get; }
    private AuthenticationSettings _AuthenticationSettings { get; }

    public AuthenticationService(
        IOptions<AuthenticationSettings> authenticationOptions,
        IUserRegistry userRepository,
        ITokenService tokenService,
        IAuthorizationService authorizationService)
    {
        _AuthenticationSettings = authenticationOptions.Value;
        _UserRepository = userRepository;
        _TokenService = tokenService;
        _AuthorizationService = authorizationService;
    }

    public Task<User?> GetIdentityAsync(string email)
    {
        return _UserRepository.GetAsync(email);
    }

    public async Task<Token> SignInAsync(SignInRequest signIn)
    {
        var authenticated = await _UserRepository.AuthenticateAsync(signIn.Email, signIn.Password);
        if (!authenticated)
        {
            throw new AuthenticationException(
                "The email or password is incorrect.",
                ExceptionCause.IncorrectData);
        }

        var user = await _UserRepository.GetAsync(signIn.Email);
        if (user is null)
        {
            throw new AuthenticationException(
                "There is no user associated with given email address.",
                ExceptionCause.IncorrectData);
        }

        var claims = new Claims()
        {
            Email = user.Email,
            Roles = user.Roles
        };

        return _TokenService.GenerateSecurityToken(claims);
    }

    public async Task<User> SignUpAsync(SignUpRequest signUp, string? key = null)
    {
        EnsureAuthorizedRegistration(key);

        if (!Validator.IsValidEmail(signUp.Email))
        {
            throw new RegistrationException(
                $"Cannot register new user. Not valid email address ({signUp.Email}) was given.",
                ExceptionCause.IncorrectData);
        }

        var user = await _UserRepository.GetAsync(signUp.Email);
        if (user is not null)
        {
            throw new RegistrationException(
                $"Cannot register new user. Given Email: {signUp.Email} is already used.",
                ExceptionCause.IncorrectData);
        }

        if (signUp.Password != signUp.ConfirmationPassword)
        {
            throw new RegistrationException(
                "Cannot register new user. Given Password and Confirmation Password does not match.",
                ExceptionCause.IncorrectData);
        }

        var validationResult = await _UserRepository.ValidatePasswordAsync(signUp.Password);
        if (!validationResult.IsValid)
        {
            throw new RegistrationException(
                    "Cannot register new user. Given password is not valid, check details for more information.",
                    validationResult.Errors,
                    ExceptionCause.IncorrectData);
        }

        User newUser = new(
            Guid.NewGuid(),
            signUp.Firstname,
            signUp.Lastname,
            signUp.Email
        );

        var createdSuccessfully = await _UserRepository.CreateAsync(newUser, signUp.Password);
        if (!createdSuccessfully)
        {
            throw new RegistrationException("User registration failed unexpectedly.");
        }

        await _AuthorizationService.AssignDefaultRoles(newUser.Guid);

        return await _UserRepository.GetAsync(signUp.Email);
    }

    private void EnsureAuthorizedRegistration(string? key)
    {
        var mode = _AuthenticationSettings.RegistrationMode;
        switch (mode)
        {
            case RegistrationMode.PUBLIC:
                break;
            case RegistrationMode.KEY_BASED:
                if (key != _AuthenticationSettings.RegistrationKey)
                {
                    goto case RegistrationMode.BLOCKED;
                }
                break;
            case RegistrationMode.BLOCKED:
            default:
                throw new RegistrationException(
                    "The registration is prohibited.",
                    ExceptionCause.SystemConfiguration);
        }
    }
}