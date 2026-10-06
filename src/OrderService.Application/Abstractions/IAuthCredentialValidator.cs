namespace OrderService.Application.Abstractions;

public interface IAuthCredentialValidator
{
    bool Validate(string username, string password);
}
