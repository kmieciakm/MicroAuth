using Domain.Contracts;
using Domain.Exceptions;
using Domain.Infrastructure;
using Domain.Models;

namespace Domain.Services;

public class AuthenticationService : IAuthenticationService
{
    private IUserRegistry _UserRepository { get; }
    private ITokenService _TokenService { get; }

    public AuthenticationService(
        IUserRegistry userRepository, ITokenService tokenService)
    {
        _UserRepository = userRepository;
        _TokenService = tokenService;
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

        return _TokenService.GenerateSecurityToken(signIn.Email);
    }

    public async Task<User> SignUpAsync(SignUpRequest signUp)
    {
        if (!Validator.IsValidEmail(signUp.Email))
        {
            throw new RegistrationException(
                $"Cannot register new user. Not valid email address ({signUp.Email}) was given.",
                ExceptionCause.IncorrectData);
        }

        var user = await _UserRepository.GetAsync(signUp.Email);
        if (user != null)
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
        return await _UserRepository.GetAsync(signUp.Email);
    }
}