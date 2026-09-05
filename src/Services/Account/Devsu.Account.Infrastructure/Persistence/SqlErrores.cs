using Microsoft.Data.SqlClient;

namespace Devsu.Account.Infrastructure.Persistence;

internal static class SqlErrores
{
    private const int IndiceUnicoDuplicado = 2601;

    private const int ClaveDuplicada = 2627;

    public static bool EsViolacionDeUnicidad(Exception ex)
        => ex.InnerException is SqlException sql
           && sql.Number is IndiceUnicoDuplicado or ClaveDuplicada;
}
