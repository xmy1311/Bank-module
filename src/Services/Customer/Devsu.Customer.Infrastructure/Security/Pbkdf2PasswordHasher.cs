using System.Security.Cryptography;
using Devsu.Customer.Domain.Services;

namespace Devsu.Customer.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256 con la BCL, sin dependencias externas.
///
/// Estos parámetros deben coincidir EXACTAMENTE con los que generaron los hashes
/// del seed: cambiar cualquiera invalida las contraseñas sembradas.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;      // 128 bits
    private const int KeySize = 32;       // 256 bits
    private const int Iterations = 100_000;

    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public (string Hash, string Salt) Hash(string passwordPlano)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derivar(passwordPlano, salt);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string passwordPlano, string hash, string salt)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
        {
            return false;
        }

        byte[] saltBytes;
        byte[] hashEsperado;

        try
        {
            saltBytes = Convert.FromBase64String(salt);
            hashEsperado = Convert.FromBase64String(hash);
        }
        catch (FormatException)
        {
            return false;
        }

        var hashCalculado = Derivar(passwordPlano, saltBytes);

        // Comparación en tiempo constante: evita timing attacks.
        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }

    private static byte[] Derivar(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algoritmo, KeySize);
}
