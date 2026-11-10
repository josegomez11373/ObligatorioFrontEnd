using System;

namespace ObligatorioFrontEnd.Models;

public enum TareaRecursoTipo
{
    Insumo,
    Maquinaria
}

public sealed record TareaRecurso
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public TareaRecursoTipo Tipo { get; init; }
    public Guid RecursoId { get; init; }
    public decimal Cantidad { get; init; }
    public decimal? Costo { get; init; }
}
