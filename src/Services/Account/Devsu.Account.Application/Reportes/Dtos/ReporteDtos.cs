namespace Devsu.Account.Application.Reportes.Dtos;


public sealed record ReporteEstadoCuentaResponse
{
    public required ReporteClienteDto Cliente { get; init; }
    public required DateTime FechaInicio { get; init; }
    public required DateTime FechaFin { get; init; }
    public required IReadOnlyCollection<ReporteCuentaDto> Cuentas { get; init; }

    public decimal TotalDebitos => Cuentas.Sum(c => c.Movimientos.Where(m => m.Valor < 0).Sum(m => m.Valor));

    public decimal TotalCreditos => Cuentas.Sum(c => c.Movimientos.Where(m => m.Valor > 0).Sum(m => m.Valor));
}

public sealed record ReporteClienteDto
{
    public required string ClienteId { get; init; }
    public required string Nombre { get; init; }
    public required string Identificacion { get; init; }
    public required bool Estado { get; init; }
}

public sealed record ReporteCuentaDto
{
    public required string NumeroCuenta { get; init; }
    public required string TipoCuenta { get; init; }
    public required decimal SaldoInicial { get; init; }
    public required decimal SaldoDisponible { get; init; }
    public required bool Estado { get; init; }
    public required IReadOnlyCollection<ReporteMovimientoDto> Movimientos { get; init; }
}

public sealed record ReporteMovimientoDto
{
    public required DateTime Fecha { get; init; }
    public required string TipoMovimiento { get; init; }
    public required decimal Valor { get; init; }
    public required decimal Saldo { get; init; }
}
