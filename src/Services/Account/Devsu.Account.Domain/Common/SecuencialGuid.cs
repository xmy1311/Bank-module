namespace Devsu.Account.Domain.Common;

public static class SecuencialGuid
{
    /// <summary>
    /// GUID v7: creciente en el tiempo, por lo que como clave clustered no
    /// fragmenta el índice como sí haría un GUID v4 aleatorio.
    /// </summary>
    public static Guid Nuevo() => Guid.CreateVersion7();
}
