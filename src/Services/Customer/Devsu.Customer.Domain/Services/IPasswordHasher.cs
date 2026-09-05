namespace Devsu.Customer.Domain.Services;


public interface IPasswordHasher
{
    (string Hash, string Salt) Hash(string passwordPlano);

    bool Verify(string passwordPlano, string hash, string salt);
}
