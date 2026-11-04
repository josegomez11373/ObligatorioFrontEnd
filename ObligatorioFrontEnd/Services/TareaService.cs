using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class TareaService
{
    private readonly IUserScope userScope;
    private readonly CampanaService campanaService;
    private readonly PotreroService potreroService;
    private readonly TipoTareaService tipoTareaService;
    private readonly EventoService eventoService;
    private readonly UserService userService;
    private readonly ContratistaService contratistaService;
    private readonly InsumoService insumoService;
    private readonly MaquinariaService maquinariaService;
    private readonly AuditService audit;
    private readonly List<Tarea> tareas = [];

    public TareaService(
        IUserScope userScope,
        CampanaService campanaService,
        PotreroService potreroService,
        TipoTareaService tipoTareaService,
        EventoService eventoService,
        UserService userService,
        ContratistaService contratistaService,
        InsumoService insumoService,
        MaquinariaService maquinariaService,
        AuditService audit)
    {
        this.userScope = userScope;
        this.campanaService = campanaService;
        this.potreroService = potreroService;
        this.tipoTareaService = tipoTareaService;
        this.eventoService = eventoService;
        this.userService = userService;
        this.contratistaService = contratistaService;
        this.insumoService = insumoService;
        this.maquinariaService = maquinariaService;
        this.audit = audit;
    }

    public event Action? StateChanged;

    public IReadOnlyList<Tarea> GetAll(bool includeAnuladas = true) => tareas
        .Where(t => (includeAnuladas || t.Estado != TareaEstado.Anulada) && CanAccessTask(t))
        .OrderByDescending(t => t.FechaPlanificada)
        .ThenByDescending(t => t.CreatedAt)
        .ToList();

    public Tarea? GetById(Guid id)
    {
        var tarea = tareas.SingleOrDefault(t => t.Id == id);
        return tarea is not null && CanAccessTask(tarea) ? tarea : null;
    }

    public IReadOnlyList<Tarea> GetByAssignment(Guid campanaPotreroId) => tareas
        .Where(t => t.CampanaPotreroId == campanaPotreroId && CanAccessTask(t))
        .OrderByDescending(t => t.FechaPlanificada)
        .ToList();

    public Tarea Create(Tarea draft)
    {
        EnsureCanCreate();
        if (draft.Id != Guid.Empty)
        {
            var existing = tareas.SingleOrDefault(t => t.Id == draft.Id);
            if (existing is not null)
            {
                if (!CanAccessTask(existing))
                {
                    throw new InvalidOperationException("No tiene acceso a la tarea seleccionada.");
                }

                return existing;
            }
        }

        ValidateDraft(draft);
        var tarea = draft with
        {
            Id = draft.Id == Guid.Empty ? Guid.NewGuid() : draft.Id,
            Descripcion = draft.Descripcion.Trim(),
            Estado = TareaEstado.Planificada,
            EjecutorId = null,
            EjecutorTipo = null,
            Costo = CalculateCost(draft.Recursos, draft.CostoContratista),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userScope.CurrentUser?.Email,
            UpdatedAt = null,
            UpdatedBy = null,
            Version = 1
        };
        tareas.Add(tarea);
        audit.Record(nameof(Tarea), tarea.Id, "Crear", tarea.EmpresaId, null, tarea);
        StateChanged?.Invoke();
        return tarea;
    }

    public bool Update(Tarea updated, long expectedVersion)
    {
        EnsureCanCreate();
        var index = FindAccessible(updated.Id);
        var existing = tareas[index];
        EnsureVersion(existing.Version, expectedVersion);
        if (existing.Estado is TareaEstado.Realizada or TareaEstado.Verificada or TareaEstado.Anulada)
        {
            throw new InvalidOperationException("Solo se pueden editar tareas planificadas o asignadas.");
        }

        if (updated.EmpresaId != existing.EmpresaId)
        {
            throw new InvalidOperationException("La empresa de una tarea no se puede cambiar.");
        }

        ValidateDraft(updated, existing.Id);
        var replacement = existing with
        {
            CampanaPotreroId = updated.CampanaPotreroId,
            EventoId = updated.EventoId,
            TipoTareaId = updated.TipoTareaId,
            Descripcion = updated.Descripcion.Trim(),
            FechaPlanificada = updated.FechaPlanificada.Date,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        tareas[index] = replacement;
        audit.Record(nameof(Tarea), existing.Id, "Actualizar", existing.EmpresaId, existing, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool Assign(Guid id, Guid executorId, TareaEjecutorTipo executorType)
    {
        EnsureCanAssign();
        var index = FindAccessible(id);
        var current = tareas[index];
        if (current.Estado is TareaEstado.Realizada or TareaEstado.Verificada or TareaEstado.Anulada)
        {
            throw new InvalidOperationException("No se puede reasignar una tarea en ese estado.");
        }

        var assignment = campanaService.GetCampanaPotreroById(current.CampanaPotreroId)
            ?? throw new InvalidOperationException("La campaña-potrero de la tarea ya no está disponible.");
        var potrero = potreroService.GetById(assignment.Assignment.PotreroId)
            ?? throw new InvalidOperationException("El potrero de la tarea ya no está disponible.");

        if (executorType == TareaEjecutorTipo.Usuario)
        {
            var user = userService.GetById(executorId);
            if (user is not { Activo: true } || user.EmpresaId != current.EmpresaId)
            {
                throw new InvalidOperationException("El usuario no pertenece a la empresa de la tarea.");
            }

            if (user.Rol != UserRole.OperarioCampo)
            {
                throw new InvalidOperationException("Solo un operario de campo puede ejecutar tareas asignadas a usuarios.");
            }

            if (user.EstablecimientoIds.Count > 0 && !user.EstablecimientoIds.Contains(potrero.EstablecimientoId))
            {
                throw new InvalidOperationException("El operario no tiene alcance sobre el establecimiento de la tarea.");
            }
        }
        else
        {
            var contractor = contratistaService.GetById(executorId);
            if (contractor is not { Activo: true } || contractor.EmpresaId != current.EmpresaId)
            {
                throw new InvalidOperationException("El contratista no pertenece a la empresa de la tarea.");
            }
        }

        var replacement = current with
        {
            EjecutorId = executorId,
            EjecutorTipo = executorType,
            Estado = TareaEstado.Asignada,
            AsignadaPor = userScope.CurrentUser?.Name,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        tareas[index] = replacement;
        audit.Record(nameof(Tarea), current.Id, "Asignar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool AddResource(Guid id, TareaRecurso resource)
    {
        EnsureCanAssign();
        var index = FindAccessible(id);
        var current = tareas[index];
        if (current.Estado is TareaEstado.Verificada or TareaEstado.Anulada)
        {
            throw new InvalidOperationException("No se pueden modificar recursos de una tarea verificada o anulada.");
        }

        if (resource.Cantidad <= 0)
        {
            throw new InvalidOperationException("La cantidad del recurso debe ser mayor que cero.");
        }

        decimal unitCost;
        if (resource.Tipo == TareaRecursoTipo.Insumo)
        {
            var insumo = insumoService.GetById(resource.RecursoId);
            if (insumo is not { Activo: true } || insumo.EmpresaId != current.EmpresaId)
            {
                throw new InvalidOperationException("El insumo no está disponible en la empresa.");
            }

            unitCost = insumo.PrecioUnitario ?? 0;
        }
        else
        {
            var maquinaria = maquinariaService.GetById(resource.RecursoId);
            if (maquinaria is not { Activo: true } || maquinaria.EmpresaId != current.EmpresaId)
            {
                throw new InvalidOperationException("La maquinaria no está disponible en la empresa.");
            }

            unitCost = maquinaria.CostoHora ?? 0;
        }

        var priced = resource with { Id = resource.Id == Guid.Empty ? Guid.NewGuid() : resource.Id, Costo = resource.Cantidad * unitCost };
        var resources = current.Recursos.Append(priced).ToList();
        var replacement = current with
        {
            Recursos = resources,
            Costo = CalculateCost(resources, current.CostoContratista),
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        tareas[index] = replacement;
        audit.Record(nameof(Tarea), current.Id, "Agregar recurso", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool RemoveResource(Guid id, Guid resourceId)
    {
        EnsureCanAssign();
        var index = FindAccessible(id);
        var current = tareas[index];
        if (current.Estado is TareaEstado.Verificada or TareaEstado.Anulada)
        {
            throw new InvalidOperationException("No se pueden modificar recursos de una tarea verificada o anulada.");
        }

        var resources = current.Recursos.Where(r => r.Id != resourceId).ToList();
        if (resources.Count == current.Recursos.Count) return false;

        var replacement = current with
        {
            Recursos = resources,
            Costo = CalculateCost(resources, current.CostoContratista),
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        tareas[index] = replacement;
        audit.Record(nameof(Tarea), current.Id, "Quitar recurso", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool SetCostoContratista(Guid id, decimal costo)
    {
        EnsureCanAssign();
        if (costo < 0)
        {
            throw new InvalidOperationException("El costo del contratista no puede ser negativo.");
        }

        var index = FindAccessible(id);
        var current = tareas[index];
        if (current.Estado is TareaEstado.Verificada or TareaEstado.Anulada)
        {
            throw new InvalidOperationException("No se puede modificar el costo de una tarea verificada o anulada.");
        }

        var replacement = current with
        {
            CostoContratista = costo,
            Costo = CalculateCost(current.Recursos, costo),
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        tareas[index] = replacement;
        audit.Record(nameof(Tarea), current.Id, "Costo contratista", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>El ejecutor marca la tarea como realizada; el resultado lo registra un responsable.</summary>
    public bool MarcarRealizada(Guid id)
    {
        EnsureCanExecute();
        var index = FindAccessible(id);
        var current = tareas[index];
        if (!IsCurrentExecutor(current))
        {
            throw new InvalidOperationException("Solo el ejecutor asignado puede marcar la tarea como realizada.");
        }

        if (current.Estado is TareaEstado.Realizada or TareaEstado.Verificada or TareaEstado.Anulada)
        {
            throw new InvalidOperationException("La tarea ya está realizada, verificada o anulada.");
        }

        var replacement = current with
        {
            Estado = TareaEstado.Realizada,
            FechaRealizacion = SigaTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        tareas[index] = replacement;
        audit.Record(nameof(Tarea), current.Id, "Marcar realizada", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool RegisterResult(Guid id, string resultado)
    {
        EnsureCanRegisterResult();
        if (string.IsNullOrWhiteSpace(resultado))
        {
            throw new InvalidOperationException("El resultado es obligatorio.");
        }

        var index = FindAccessible(id);
        var current = tareas[index];
        if (current.Estado is TareaEstado.Verificada or TareaEstado.Anulada)
        {
            throw new InvalidOperationException("No se puede registrar el resultado de una tarea verificada o anulada.");
        }

        var replacement = current with
        {
            Resultado = resultado.Trim(),
            FechaRealizacion = current.FechaRealizacion ?? SigaTime.UtcNow,
            Estado = TareaEstado.Realizada,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        tareas[index] = replacement;
        audit.Record(nameof(Tarea), current.Id, "Registrar resultado", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool Verify(Guid id, bool solucionoProblema, string? comentario)
    {
        EnsureCanVerify();
        var index = FindAccessible(id);
        var current = tareas[index];
        if (current.Estado != TareaEstado.Realizada)
        {
            throw new InvalidOperationException("Solo se pueden verificar tareas realizadas.");
        }

        var resultado = string.IsNullOrWhiteSpace(comentario)
            ? current.Resultado
            : $"{current.Resultado} | Verificación: {comentario.Trim()}".Trim(' ', '|');

        var replacement = current with
        {
            Resultado = resultado,
            SolucionoProblema = solucionoProblema,
            FechaComprobacion = SigaTime.UtcNow,
            Estado = TareaEstado.Verificada,
            VerificadaPor = userScope.CurrentUser?.Name,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        tareas[index] = replacement;
        audit.Record(nameof(Tarea), current.Id, "Verificar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool Anular(Guid id, string motivo)
    {
        EnsureCanAnular();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new InvalidOperationException("El motivo de anulación es obligatorio.");
        }

        var index = FindAccessible(id);
        var current = tareas[index];
        if (current.Estado == TareaEstado.Anulada) return true;

        var replacement = current with
        {
            Estado = TareaEstado.Anulada,
            MotivoAnulacion = motivo.Trim(),
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        tareas[index] = replacement;
        audit.Record(nameof(Tarea), current.Id, "Anular", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    private void ValidateDraft(Tarea tarea, Guid ignoredId = default)
    {
        var assignment = campanaService.GetCampanaPotreroById(tarea.CampanaPotreroId)
            ?? throw new InvalidOperationException("Debe seleccionar una campaña y potrero.");

        if (assignment.Campana.Estado is CampanaEstado.Anulada or CampanaEstado.Cerrada ||
            assignment.Assignment.Estado is CampanaEstado.Anulada or CampanaEstado.Cerrada)
        {
            throw new InvalidOperationException("No se pueden crear tareas en una campaña cerrada o anulada.");
        }

        var potrero = potreroService.GetById(assignment.Assignment.PotreroId);
        if (potrero is null || !potrero.Activo)
        {
            throw new InvalidOperationException("El potrero seleccionado no está activo.");
        }

        var tipo = tipoTareaService.GetById(tarea.TipoTareaId);
        if (tipo is null || !tipo.Activo || tipo.EmpresaId != assignment.Campana.EmpresaId)
        {
            throw new InvalidOperationException("Debe seleccionar un tipo de tarea válido.");
        }

        if (tarea.EmpresaId != assignment.Campana.EmpresaId)
        {
            throw new InvalidOperationException("La tarea no corresponde a la empresa de la campaña.");
        }

        if (string.IsNullOrWhiteSpace(tarea.Descripcion) || tarea.Descripcion.Trim().Length > 1000)
        {
            throw new InvalidOperationException("La descripción de la tarea es obligatoria y no puede superar 1000 caracteres.");
        }

        if (tarea.FechaPlanificada == default)
        {
            throw new InvalidOperationException("La fecha planificada es obligatoria.");
        }

        if (tarea.EventoId is Guid eventId)
        {
            var evento = eventoService.GetById(eventId);
            if (evento is null || evento.EmpresaId != tarea.EmpresaId || evento.CampanaPotreroId != tarea.CampanaPotreroId)
            {
                throw new InvalidOperationException("El evento seleccionado no corresponde a la campaña-potrero de la tarea.");
            }

            if (evento.Estado == EventoEstado.Anulado)
            {
                throw new InvalidOperationException("No se puede vincular una tarea a un evento anulado.");
            }
        }

        if (tareas.Any(t => t.Id != ignoredId &&
                            t.CampanaPotreroId == tarea.CampanaPotreroId &&
                            t.TipoTareaId == tarea.TipoTareaId &&
                            t.FechaPlanificada.Date == tarea.FechaPlanificada.Date &&
                            t.Descripcion.Equals(tarea.Descripcion.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            t.Estado != TareaEstado.Anulada))
        {
            throw new InvalidOperationException("Ya existe una tarea igual para ese potrero y fecha.");
        }
    }

    private bool CanAccessTask(Tarea tarea)
    {
        if (!userScope.CanAccessEmpresa(tarea.EmpresaId)) return false;
        if (userScope.CurrentUser is not { } current) return false;

        return current.Role switch
        {
            UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo => true,
            UserRole.Contratista => IsAssignedToCurrentContractor(tarea),
            UserRole.OperarioCampo => IsWithinOperarioScope(tarea),
            _ => false
        };
    }

    private bool IsWithinOperarioScope(Tarea tarea)
    {
        var assignment = campanaService.GetCampanaPotreroById(tarea.CampanaPotreroId);
        if (assignment is null) return false;
        var potrero = potreroService.GetById(assignment.Value.Assignment.PotreroId);
        return potrero is not null && userScope.CanAccessEstablecimiento(potrero.EstablecimientoId, tarea.EmpresaId);
    }

    private bool IsAssignedToCurrentContractor(Tarea tarea)
    {
        var current = userScope.CurrentUser;
        if (current is null || tarea.EjecutorTipo != TareaEjecutorTipo.Contratista || tarea.EjecutorId is null) return false;
        var contractor = contratistaService.GetById(tarea.EjecutorId.Value);
        return contractor?.UsuarioId == current.Id;
    }

    private bool IsCurrentExecutor(Tarea tarea)
    {
        var current = userScope.CurrentUser;
        if (current is null) return false;
        return tarea.EjecutorTipo switch
        {
            TareaEjecutorTipo.Usuario => tarea.EjecutorId == current.Id,
            TareaEjecutorTipo.Contratista => IsAssignedToCurrentContractor(tarea),
            _ => false
        };
    }

    private int FindAccessible(Guid id)
    {
        var index = tareas.FindIndex(t => t.Id == id && CanAccessTask(t));
        if (index < 0)
        {
            throw new InvalidOperationException("La tarea no existe o no está dentro de su alcance.");
        }

        return index;
    }

    private static decimal CalculateCost(IReadOnlyList<TareaRecurso> resources, decimal? costoContratista) =>
        resources.Sum(resource => resource.Costo ?? 0) + (costoContratista ?? 0);

    private void EnsureCanCreate()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanCreateTarea(role))
        {
            throw new InvalidOperationException("No tiene permisos para crear tareas.");
        }
    }

    private void EnsureCanAssign()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanAssignTarea(role))
        {
            throw new InvalidOperationException("No tiene permisos para planificar o asignar tareas.");
        }
    }

    private void EnsureCanExecute()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanExecuteTarea(role))
        {
            throw new InvalidOperationException("No tiene permisos para ejecutar tareas.");
        }
    }

    private void EnsureCanRegisterResult()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanRegisterResult(role))
        {
            throw new InvalidOperationException("No tiene permisos para registrar resultados de tareas.");
        }
    }

    private void EnsureCanVerify()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanVerifyTarea(role))
        {
            throw new InvalidOperationException("No tiene permisos para verificar tareas.");
        }
    }

    private void EnsureCanAnular()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanAnularTarea(role))
        {
            throw new InvalidOperationException("No tiene permisos para anular tareas.");
        }
    }

    private static void EnsureVersion(long currentVersion, long expectedVersion)
    {
        if (expectedVersion != 0 && currentVersion != expectedVersion)
        {
            throw new InvalidOperationException("La tarea fue modificada por otro usuario. Recargue la información e intente nuevamente.");
        }
    }
}
