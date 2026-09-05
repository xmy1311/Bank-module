namespace Devsu.Customer.Domain.Common;

public static class SecuencialGuid
{
    public static Guid Nuevo() => Guid.CreateVersion7();
}
