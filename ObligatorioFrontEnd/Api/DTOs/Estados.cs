namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Espejo de ApiSiga.Contracts.EstadoEvento.</summary>
public enum EstadoEvento
{
    Registrado,
    Anulado
}

/// <summary>Espejo de ApiSiga.Contracts.EstadoTarea.</summary>
public enum EstadoTarea
{
    Planificada,
    Asignada,
    Realizada,
    Verificada,
    Anulada
}

/// <summary>Define si el ejecutor de una tarea es un usuario o un contratista.</summary>
public enum TipoEjecutorTarea
{
    Usuario,
    Contratista
}

/// <summary>Recursos que se pueden asociar a una tarea.</summary>
public enum TipoRecursoTarea
{
    Insumo,
    Maquinaria
}

/// <summary>Espejo de ApiSiga.Contracts.AccionAuditoria.</summary>
public enum AccionAuditoria
{
    Alta,
    Edicion,
    Desactivacion,
    Anulacion
}
