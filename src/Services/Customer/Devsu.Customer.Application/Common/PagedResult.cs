namespace Devsu.Customer.Application.Common;

/// <summary>Resultado paginado. Evita que un listado devuelva la tabla entera.</summary>
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
