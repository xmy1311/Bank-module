namespace Devsu.Account.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Pagina,
    int TamanoPagina,
    int TotalRegistros)
{
    public int TotalPaginas => TamanoPagina <= 0
        ? 0
        : (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);
}

/// <summary>Correlation ID de la petición actual. Scoped.</summary>
public sealed class CorrelationContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
