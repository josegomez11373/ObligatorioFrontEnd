using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class EventoService
{
    private readonly IUserScope userScope;
    private readonly CampanaService campanaService;
    private readonly PotreroService potreroService;
    private readonly TipoEventoService tipoEventoService;
    private readonly FotografiaService fotografiaService;
    private readonly AuditService audit;
    private readonly List<Evento> eventos = [];

    public EventoService(
        IUserScope userScope,
        CampanaService campanaService,
        PotreroService potreroService,
        TipoEventoService tipoEventoService,
        FotografiaService fotografiaService,
        AuditService audit)
    {
        this.userScope = userScope;
        this.campanaService = campanaService;
        this.potreroService = potreroService;
        this.tipoEventoService = tipoEventoService;
        this.fotografiaService = fotografiaService;
        this.audit = audit;
    }

    public event Action? StateChanged;

    public IReadOnlyList<Evento> GetAll(bool includeAnulados = true) => eventos
        .Where(e => (includeAnulados || e.Estado == EventoEstado.Registrado) && userScope.CanAccessEmpresa(e.EmpresaId))
        .OrderByDescending(e => e.Fecha)
        .ThenByDescending(e => e.CreatedAt)
        .ToList();

    public Evento? GetById(Guid id) => eventos.SingleOrDefault(e => e.Id == id && userScope.CanAccessEmpresa(e.EmpresaId));

    public IReadOnlyList<Evento> GetByAssignment(Guid campanaPotreroId) => eventos
        .Where(e => e.CampanaPotreroId == campanaPotreroId && userScope.CanAccessEmpresa(e.EmpresaId))
        .OrderByDescending(e => e.Fecha)
        .ToList();

    public IReadOnlyList<Evento> GetByCampana(Guid campanaId)
    {
        var assignmentIds = campanaService.GetPotreros(campanaId, includeInactive: true)
            .Select(cp => cp.Id)
            .ToHashSet();

        return eventos
            .Where(e => assignmentIds.Contains(e.CampanaPotreroId) && userScope.CanAccessEmpresa(e.EmpresaId))
            .OrderByDescending(e => e.Fecha)
            .ToList();
    }

    /// <summary>
    /// Crea un evento conservando el identificador enviado por el cliente para que un
    /// reintento de sincronización no genere duplicados.
    /// </summary>
    public Evento Create(Evento draft, IReadOnlyList<FotoUpload>? fotos = null)
    {
        EnsureCanManage();

        if (draft.Id != Guid.Empty)
        {
            var existing = eventos.SingleOrDefault(e => e.Id == draft.Id);
            if (existing is not null)
            {
                if (!userScope.CanAccessEmpresa(existing.EmpresaId))
                {
                    throw new InvalidOperationException("No tiene acceso al evento seleccionado.");
                }

                return existing;
            }
        }

        Validate(draft);
        var evento = draft with
        {
            Id = draft.Id == Guid.Empty ? Guid.NewGuid() : draft.Id,
            Descripcion = draft.Descripcion.Trim(),
            Ubicacion = draft.Ubicacion?.Trim(),
            RegistradoPor = userScope.CurrentUser?.Name,
            RegistradoPorId = userScope.CurrentUser?.Id,
            Estado = EventoEstado.Registrado,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userScope.CurrentUser?.Email,
            UpdatedAt = null,
            UpdatedBy = null,
            Version = 1
        };
        eventos.Add(evento);
        audit.Record(nameof(Evento), evento.Id, "Crear", evento.EmpresaId, null, evento);
        if (fotos is { Count: > 0 })
        {
            fotografiaService.CreateMany(evento.EmpresaId, evento.Id, fotos, evento.Latitud, evento.Longitud);
        }

        StateChanged?.Invoke();
        return evento;
    }

    /// <summary>Registra el mismo fenómeno en varios potreros, un evento por potrero (RN-07).</summary>
    public IReadOnlyList<Evento> CreateForPotreros(
        IReadOnlyCollection<Guid> campanaPotreroIds,
        Guid tipoEventoId,
        DateTime fecha,
        string descripcion,
        string? ubicacion,
        double? latitud,
        double? longitud,
        IReadOnlyList<FotoUpload>? fotos = null)
    {
        EnsureCanManage();
        if (campanaPotreroIds.Count == 0)
        {
            throw new InvalidOperationException("Debe seleccionar al menos un potrero.");
        }

        var created = new List<Evento>();
        foreach (var assignmentId in campanaPotreroIds.Distinct())
        {
            var assignment = campanaService.GetCampanaPotreroById(assignmentId)
                ?? throw new InvalidOperationException("Debe seleccionar una campaña-potrero válida.");

            var draft = new Evento
            {
                EmpresaId = assignment.Campana.EmpresaId,
                CampanaPotreroId = assignmentId,
                TipoEventoId = tipoEventoId,
                Fecha = fecha,
                Descripcion = descripcion,
                Ubicacion = ubicacion,
                Latitud = latitud,
                Longitud = longitud
            };
            created.Add(Create(draft, fotos));
        }

        return created;
    }

    public bool Update(Evento updated, long expectedVersion, IReadOnlyList<FotoUpload>? nuevasFotos = null)
    {
        EnsureCanManage();
        var index = eventos.FindIndex(e => e.Id == updated.Id);
        if (index < 0 || !userScope.CanAccessEmpresa(eventos[index].EmpresaId))
        {
            return false;
        }

        var existing = eventos[index];
        EnsureVersion(existing.Version, expectedVersion);
        if (updated.EmpresaId != existing.EmpresaId)
        {
            throw new InvalidOperationException("La empresa de un evento no se puede cambiar.");
        }

        Validate(updated, existing.Id);
        var replacement = existing with
        {
            CampanaPotreroId = updated.CampanaPotreroId,
            TipoEventoId = updated.TipoEventoId,
            Fecha = updated.Fecha.Date,
            Descripcion = updated.Descripcion.Trim(),
            Ubicacion = updated.Ubicacion?.Trim(),
            Latitud = updated.Latitud,
            Longitud = updated.Longitud,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        eventos[index] = replacement;
        audit.Record(nameof(Evento), existing.Id, "Actualizar", existing.EmpresaId, existing, replacement);
        if (nuevasFotos is { Count: > 0 })
        {
            fotografiaService.CreateMany(replacement.EmpresaId, replacement.Id, nuevasFotos, replacement.Latitud, replacement.Longitud);
        }

        StateChanged?.Invoke();
        return true;
    }

    public bool Anular(Guid id, string motivo)
    {
        EnsureCanManage();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new InvalidOperationException("El motivo de anulación es obligatorio.");
        }

        var index = eventos.FindIndex(e => e.Id == id);
        if (index < 0 || !userScope.CanAccessEmpresa(eventos[index].EmpresaId))
        {
            return false;
        }

        var existing = eventos[index];
        if (existing.Estado == EventoEstado.Anulado) return true;

        var replacement = existing with
        {
            Estado = EventoEstado.Anulado,
            MotivoAnulacion = motivo.Trim(),
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        eventos[index] = replacement;
        audit.Record(nameof(Evento), existing.Id, "Anular", existing.EmpresaId, existing, replacement);
        StateChanged?.Invoke();
        return true;
    }

    private void Validate(Evento evento, Guid ignoredId = default)
    {
        var assignment = campanaService.GetCampanaPotreroById(evento.CampanaPotreroId);
        if (assignment is not { } selectedAssignment)
        {
            throw new InvalidOperationException("Debe seleccionar una campaña-potrero válida.");
        }

        if (selectedAssignment.Campana.Estado is CampanaEstado.Anulada or CampanaEstado.Cerrada ||
            selectedAssignment.Assignment.Estado is CampanaEstado.Anulada or CampanaEstado.Cerrada)
        {
            throw new InvalidOperationException("No se pueden registrar eventos en una campaña cerrada o anulada.");
        }

        var potrero = potreroService.GetById(selectedAssignment.Assignment.PotreroId);
        if (potrero is null || !potrero.Activo)
        {
            throw new InvalidOperationException("El potrero seleccionado no está activo.");
        }

        var tipo = tipoEventoService.GetById(evento.TipoEventoId);
        if (tipo is null || !tipo.Activo || tipo.EmpresaId != selectedAssignment.Campana.EmpresaId)
        {
            throw new InvalidOperationException("Debe seleccionar un tipo de evento válido.");
        }

        if (evento.EmpresaId != selectedAssignment.Campana.EmpresaId || string.IsNullOrWhiteSpace(evento.Descripcion))
        {
            throw new InvalidOperationException("Los datos del evento no son válidos.");
        }

        if (evento.Descripcion.Trim().Length > 1000)
        {
            throw new InvalidOperationException("La descripción no puede superar 1000 caracteres.");
        }

        if (evento.Fecha == default)
        {
            throw new InvalidOperationException("La fecha del evento es obligatoria.");
        }

        if (evento.Fecha.Date > SigaTime.TodayLocal)
        {
            throw new InvalidOperationException("La fecha del evento no puede ser futura.");
        }

        if (evento.Latitud is < -90 or > 90 || evento.Longitud is < -180 or > 180)
        {
            throw new InvalidOperationException("Las coordenadas del evento no son válidas.");
        }

        if (eventos.Any(e => e.Id != ignoredId &&
                             e.CampanaPotreroId == evento.CampanaPotreroId &&
                             e.TipoEventoId == evento.TipoEventoId &&
                             e.Fecha.Date == evento.Fecha.Date &&
                             e.Descripcion.Equals(evento.Descripcion.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un evento igual para ese potrero y fecha.");
        }
    }

    private void EnsureCanManage()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanManage(role, SigaModule.Eventos))
        {
            throw new InvalidOperationException("No tiene permisos para registrar eventos.");
        }
    }

    private static void EnsureVersion(long currentVersion, long expectedVersion)
    {
        if (expectedVersion != 0 && currentVersion != expectedVersion)
        {
            throw new InvalidOperationException("El evento fue modificado por otro usuario. Recargue la información e intente nuevamente.");
        }
    }
}
