using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class CampanaService : IServicioCampanas
{
    private readonly IUserScope userScope;
    private readonly CultivoService cultivoService;
    private readonly PotreroService potreroService;
    private readonly EstablecimientoService establecimientoService;
    private readonly EmpresaService empresaService;
    private readonly AuditService audit;
    private readonly NotificadorDatos notificador;

    private readonly List<Campana> campanas =
    [
        new()
        {
            Id = SeedIds.CampanaSoja2026,
            EmpresaId = SeedIds.AgroDelSur,
            Nombre = "Soja 2026",
            CultivoId = SeedIds.SojaAgroDelSur,
            FechaInicio = new DateTime(2026, 9, 1),
            Estado = CampanaEstado.Activa,
            CreatedBy = "seed"
        },
        new()
        {
            Id = SeedIds.CampanaTrigo2026,
            EmpresaId = SeedIds.CamposVerdes,
            Nombre = "Trigo 2026",
            CultivoId = SeedIds.TrigoCamposVerdes,
            FechaInicio = new DateTime(2026, 6, 15),
            FechaFin = new DateTime(2026, 11, 30),
            Estado = CampanaEstado.Cerrada,
            CreatedBy = "seed"
        }
    ];

    private readonly List<CampanaPotrero> campanaPotreros =
    [
        new() { Id = SeedIds.CampanaPotreroSoja, CampanaId = SeedIds.CampanaSoja2026, PotreroId = SeedIds.PotreroLaEsperanza, FechaInicio = new DateTime(2026, 9, 1), Estado = CampanaEstado.Activa, CreatedBy = "seed" },
        new() { Id = SeedIds.CampanaPotreroTrigo, CampanaId = SeedIds.CampanaTrigo2026, PotreroId = SeedIds.PotreroElAlto, FechaInicio = new DateTime(2026, 6, 15), FechaFin = new DateTime(2026, 11, 30), Estado = CampanaEstado.Cerrada, CreatedBy = "seed" }
    ];

    public CampanaService(
        IUserScope userScope,
        CultivoService cultivoService,
        PotreroService potreroService,
        EstablecimientoService establecimientoService,
        EmpresaService empresaService,
        AuditService audit,
        NotificadorDatos notificador)
    {
        this.userScope = userScope;
        this.cultivoService = cultivoService;
        this.potreroService = potreroService;
        this.establecimientoService = establecimientoService;
        this.empresaService = empresaService;
        this.audit = audit;
        this.notificador = notificador;
    }

    public event Action? StateChanged;

    public IReadOnlyList<Campana> GetAll(bool includeInactive = false) => campanas
        .Where(c => (includeInactive || c.Estado != CampanaEstado.Anulada) && CanAccess(c))
        .OrderByDescending(c => c.FechaInicio)
        .ThenBy(c => c.Nombre)
        .ToList();

    public Campana? GetById(Guid id)
    {
        var campana = campanas.SingleOrDefault(c => c.Id == id);
        return campana is not null && CanAccess(campana) ? campana : null;
    }

    public IReadOnlyList<CampanaPotrero> GetPotreros(Guid campanaId, bool includeInactive = false)
    {
        if (GetById(campanaId) is null)
        {
            return [];
        }

        return campanaPotreros
            .Where(cp => cp.CampanaId == campanaId &&
                         (includeInactive || cp.Estado != CampanaEstado.Anulada) &&
                         potreroService.GetById(cp.PotreroId) is not null)
            .OrderBy(cp => potreroService.GetById(cp.PotreroId)!.Nombre)
            .ToList();
    }

    public IReadOnlyList<(CampanaPotrero Assignment, Campana Campana)> GetCampanaPotreros() => campanaPotreros
        .Select(assignment => (Assignment: assignment, Campana: campanas.SingleOrDefault(c => c.Id == assignment.CampanaId)))
        .Where(item => item.Campana is not null &&
                       item.Campana.Estado != CampanaEstado.Anulada &&
                       CanAccess(item.Campana) &&
                       potreroService.GetById(item.Assignment.PotreroId) is not null)
        .Select(item => (Assignment: item.Assignment, Campana: item.Campana!))
        .OrderByDescending(item => item.Campana.FechaInicio)
        .ToList();

    public (CampanaPotrero Assignment, Campana Campana)? GetCampanaPotreroById(Guid id)
    {
        var assignment = campanaPotreros.SingleOrDefault(cp => cp.Id == id);
        if (assignment is null)
        {
            return null;
        }

        var campana = campanas.SingleOrDefault(c => c.Id == assignment.CampanaId);
        return campana is not null && CanAccess(campana) && potreroService.GetById(assignment.PotreroId) is not null
            ? (assignment, campana)
            : null;
    }

    public Campana Create(Campana draft, IReadOnlyCollection<Guid> potreroIds)
    {
        EnsureCanManage();
        ValidateDraft(draft, potreroIds, Guid.Empty);

        var campana = draft with
        {
            Id = draft.Id == Guid.Empty ? Guid.NewGuid() : draft.Id,
            Nombre = draft.Nombre.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userScope.CurrentUser?.Email,
            UpdatedAt = null,
            UpdatedBy = null,
            Version = 1
        };

        campanas.Add(campana);
        AddPotreros(campana, potreroIds);
        audit.Record(nameof(Campana), campana.Id, "Crear", campana.EmpresaId, null, campana);
        Notify();
        return campana;
    }

    public bool Update(Campana updated, IReadOnlyCollection<Guid> potreroIds, long expectedVersion)
    {
        EnsureCanManage();
        var index = campanas.FindIndex(c => c.Id == updated.Id);
        if (index < 0)
        {
            return false;
        }

        var existing = campanas[index];
        EnsureAccess(existing);
        if (existing.EmpresaId != updated.EmpresaId)
        {
            throw new InvalidOperationException("La empresa de una campaña no se puede cambiar.");
        }

        EnsureVersion(existing.Version, expectedVersion);
        ValidateDraft(updated, potreroIds, existing.Id);

        var saved = existing with
        {
            Nombre = updated.Nombre.Trim(),
            CultivoId = updated.CultivoId,
            FechaInicio = updated.FechaInicio.Date,
            FechaFin = updated.FechaFin?.Date,
            Estado = updated.Estado,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };

        campanas[index] = saved;
        SyncPotreros(saved, potreroIds);
        audit.Record(nameof(Campana), existing.Id, "Actualizar", existing.EmpresaId, existing, saved);
        Notify();
        return true;
    }

    /// <summary>Cierra el trabajo de un potrero sin borrar la asignación ni su historial.</summary>
    public bool CerrarPotrero(Guid campanaPotreroId, DateTime? fechaFin = null)
    {
        EnsureCanManage();
        var index = campanaPotreros.FindIndex(cp => cp.Id == campanaPotreroId);
        if (index < 0) return false;

        var existing = campanaPotreros[index];
        var assignment = GetCampanaPotreroById(campanaPotreroId);
        if (assignment is null)
        {
            throw new InvalidOperationException("No tiene acceso a la campaña-potrero seleccionada.");
        }

        if (existing.Estado is CampanaEstado.Cerrada or CampanaEstado.Anulada) return true;

        var replacement = existing with
        {
            Estado = CampanaEstado.Cerrada,
            FechaFin = (fechaFin ?? SigaTime.TodayLocal).Date,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        campanaPotreros[index] = replacement;
        audit.Record(nameof(CampanaPotrero), existing.Id, "Cerrar potrero", assignment.Value.Campana.EmpresaId, existing, replacement);

        var campaignAssignments = campanaPotreros.Where(cp => cp.CampanaId == assignment.Value.Campana.Id).ToList();
        if (campaignAssignments.All(cp => cp.Estado is CampanaEstado.Cerrada or CampanaEstado.Anulada))
        {
            CloseCampaign(assignment.Value.Campana, fechaFin);
        }

        Notify();
        return true;
    }

    public bool Cerrar(Guid campanaId, DateTime? fechaFin = null)
    {
        EnsureCanManage();
        var index = campanas.FindIndex(c => c.Id == campanaId);
        if (index < 0) return false;

        var existing = campanas[index];
        EnsureAccess(existing);
        if (existing.Estado == CampanaEstado.Cerrada) return true;

        var end = (fechaFin ?? SigaTime.TodayLocal).Date;
        var saved = existing with
        {
            Estado = CampanaEstado.Cerrada,
            FechaFin = end,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        campanas[index] = saved;

        for (var i = 0; i < campanaPotreros.Count; i++)
        {
            var assignment = campanaPotreros[i];
            if (assignment.CampanaId != campanaId || assignment.Estado is CampanaEstado.Cerrada or CampanaEstado.Anulada) continue;
            campanaPotreros[i] = assignment with
            {
                Estado = CampanaEstado.Cerrada,
                FechaFin = end,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userScope.CurrentUser?.Email,
                Version = assignment.Version + 1
            };
        }

        audit.Record(nameof(Campana), existing.Id, "Cerrar", existing.EmpresaId, existing, saved);
        Notify();
        return true;
    }

    public Task<IReadOnlyList<CampanaDto>> GetAllAsync(
        bool incluirAnuladas = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CampanaDto> result = GetAll(includeInactive: incluirAnuladas)
            .Select(ADto)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<CampanaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var campana = GetById(id);
        return Task.FromResult(campana is null ? null : ADto(campana));
    }

    public Task<CampanaDto> CreateAsync(
        CrearCampanaDto dto,
        CancellationToken cancellationToken = default)
    {
        var empresaId = dto.EmpresaId
            ?? userScope.CurrentUser?.EmpresaId
            ?? throw new InvalidOperationException("Debe indicar la empresa de la campaña.");

        var creada = Create(
            new Campana
            {
                EmpresaId = empresaId,
                Nombre = dto.Nombre,
                CultivoId = dto.CultivoId,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                // La API siempre crea las campañas en estado Planificada.
                Estado = CampanaEstado.Planificada
            },
            dto.PotreroIds);

        return Task.FromResult(ADto(creada));
    }

    public Task<CampanaDto?> UpdateAsync(
        ActualizarCampanaDto dto,
        CancellationToken cancellationToken = default)
    {
        var existing = campanas.SingleOrDefault(campana => campana.Id == dto.Id);
        if (existing is null)
        {
            return Task.FromResult<CampanaDto?>(null);
        }

        // El contrato de la API no envía potreros: se conservan los vigentes.
        var potreroIds = campanaPotreros
            .Where(asignacion => asignacion.CampanaId == dto.Id && asignacion.Estado != CampanaEstado.Anulada)
            .Select(asignacion => asignacion.PotreroId)
            .ToList();

        var actualizada = Update(
            new Campana
            {
                Id = dto.Id,
                EmpresaId = existing.EmpresaId,
                Nombre = dto.Nombre,
                CultivoId = dto.CultivoId,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                Estado = dto.Estado.AModelo()
            },
            potreroIds,
            dto.Version);

        return Task.FromResult(
            actualizada ? ADto(campanas.Single(campana => campana.Id == dto.Id)) : null);
    }

    public Task<bool> AnularAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsureCanManage();

        var index = campanas.FindIndex(campana => campana.Id == id);
        if (index < 0)
        {
            return Task.FromResult(false);
        }

        var existing = campanas[index];
        EnsureAccess(existing);

        if (existing.Estado == CampanaEstado.Anulada)
        {
            return Task.FromResult(true);
        }

        var end = (existing.FechaFin ?? SigaTime.TodayLocal).Date;
        var saved = existing with
        {
            Estado = CampanaEstado.Anulada,
            FechaFin = end,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        campanas[index] = saved;

        for (var i = 0; i < campanaPotreros.Count; i++)
        {
            var assignment = campanaPotreros[i];
            if (assignment.CampanaId != id || assignment.Estado == CampanaEstado.Anulada) continue;
            campanaPotreros[i] = assignment with
            {
                Estado = CampanaEstado.Anulada,
                FechaFin = end,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userScope.CurrentUser?.Email,
                Version = assignment.Version + 1
            };
        }

        audit.Record(nameof(Campana), existing.Id, "Anular", existing.EmpresaId, existing, saved);
        Notify();
        return Task.FromResult(true);
    }

    private CampanaDto ADto(Campana campana)
    {
        var cultivoNombre = cultivoService.GetById(campana.CultivoId)?.Nombre ?? "—";
        var potreros = GetPotreros(campana.Id, includeInactive: true)
            .Select(asignacion => asignacion.AContrato(
                potreroService.GetById(asignacion.PotreroId)?.Nombre ?? "—"));

        return campana.AContrato(cultivoNombre, potreros);
    }

    private void Notify()
    {
        StateChanged?.Invoke();
        notificador.Notificar(ModulosDatos.Campanas);
    }

    private void CloseCampaign(Campana campana, DateTime? fechaFin)
    {
        var index = campanas.FindIndex(c => c.Id == campana.Id);
        if (index < 0 || campanas[index].Estado == CampanaEstado.Cerrada) return;

        var existing = campanas[index];
        var saved = existing with
        {
            Estado = CampanaEstado.Cerrada,
            FechaFin = (fechaFin ?? SigaTime.TodayLocal).Date,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        campanas[index] = saved;
        audit.Record(nameof(Campana), existing.Id, "Cerrar", existing.EmpresaId, existing, saved);
    }

    private void AddPotreros(Campana campana, IEnumerable<Guid> potreroIds)
    {
        foreach (var potreroId in potreroIds.Distinct())
        {
            campanaPotreros.Add(new CampanaPotrero
            {
                CampanaId = campana.Id,
                PotreroId = potreroId,
                FechaInicio = campana.FechaInicio,
                FechaFin = campana.FechaFin,
                Estado = campana.Estado,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userScope.CurrentUser?.Email,
                Version = 1
            });
        }
    }

    /// <summary>
    /// Actualiza las asignaciones existentes conservando su identificador. Los potreros
    /// quitados se marcan como anulados en lugar de eliminarse para no dejar eventos ni
    /// tareas huérfanas.
    /// </summary>
    private void SyncPotreros(Campana campana, IReadOnlyCollection<Guid> potreroIds)
    {
        var selected = potreroIds.Distinct().ToHashSet();
        var existing = campanaPotreros.Where(cp => cp.CampanaId == campana.Id).ToList();

        foreach (var assignment in existing.Where(cp => !selected.Contains(cp.PotreroId)))
        {
            if (assignment.Estado == CampanaEstado.Anulada) continue;
            var replacement = assignment with
            {
                Estado = CampanaEstado.Anulada,
                FechaFin = assignment.FechaFin ?? campana.FechaFin ?? SigaTime.TodayLocal.Date,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userScope.CurrentUser?.Email,
                Version = assignment.Version + 1
            };
            ReplaceAssignment(assignment, replacement, campana.EmpresaId);
        }

        foreach (var potreroId in selected)
        {
            var assignment = existing.FirstOrDefault(cp => cp.PotreroId == potreroId && cp.Estado != CampanaEstado.Anulada);
            if (assignment is null)
            {
                var created = new CampanaPotrero
                {
                    CampanaId = campana.Id,
                    PotreroId = potreroId,
                    FechaInicio = campana.FechaInicio,
                    FechaFin = campana.FechaFin,
                    Estado = campana.Estado,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userScope.CurrentUser?.Email,
                    Version = 1
                };
                campanaPotreros.Add(created);
                audit.Record(nameof(CampanaPotrero), created.Id, "Agregar potrero", campana.EmpresaId, null, created);
                continue;
            }

            var updatedAssignment = assignment with
            {
                FechaInicio = campana.FechaInicio,
                FechaFin = campana.FechaFin,
                Estado = campana.Estado == CampanaEstado.Anulada
                    ? CampanaEstado.Anulada
                    : campana.Estado == CampanaEstado.Cerrada
                        ? CampanaEstado.Cerrada
                        : assignment.Estado,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userScope.CurrentUser?.Email,
                Version = assignment.Version + 1
            };

            if (updatedAssignment != assignment)
            {
                ReplaceAssignment(assignment, updatedAssignment, campana.EmpresaId);
            }
        }
    }

    private void ReplaceAssignment(CampanaPotrero current, CampanaPotrero replacement, Guid empresaId)
    {
        var index = campanaPotreros.FindIndex(cp => cp.Id == current.Id);
        if (index < 0) return;
        campanaPotreros[index] = replacement;
        audit.Record(nameof(CampanaPotrero), current.Id, "Actualizar", empresaId, current, replacement);
    }

    private void ValidateDraft(Campana draft, IReadOnlyCollection<Guid> potreroIds, Guid ignoredCampanaId)
    {
        if (draft.EmpresaId == Guid.Empty || !userScope.CanAccessEmpresa(draft.EmpresaId))
        {
            throw new InvalidOperationException("No tiene acceso a la empresa seleccionada.");
        }

        var empresa = empresaService.GetById(draft.EmpresaId);
        if (empresa is null || (!empresa.Activo && draft.Estado != CampanaEstado.Cerrada && draft.Estado != CampanaEstado.Anulada))
        {
            throw new InvalidOperationException("La empresa seleccionada no está disponible.");
        }

        if (string.IsNullOrWhiteSpace(draft.Nombre))
        {
            throw new InvalidOperationException("El nombre de la campaña es obligatorio.");
        }

        var trimmed = draft.Nombre.Trim();
        if (campanas.Any(c => c.Id != ignoredCampanaId &&
                              c.EmpresaId == draft.EmpresaId &&
                              c.Nombre.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe una campaña con ese nombre en la empresa.");
        }

        if (draft.FechaInicio == default)
        {
            throw new InvalidOperationException("La fecha de inicio es obligatoria.");
        }

        if (draft.FechaFin is not null && draft.FechaFin.Value.Date < draft.FechaInicio.Date)
        {
            throw new InvalidOperationException("La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        if (draft.Estado == CampanaEstado.Cerrada && draft.FechaFin is null)
        {
            throw new InvalidOperationException("Una campaña cerrada debe tener fecha de fin.");
        }

        var cultivo = cultivoService.GetById(draft.CultivoId);
        if (cultivo is null || !cultivo.Activo || cultivo.EmpresaId != draft.EmpresaId)
        {
            throw new InvalidOperationException("Debe seleccionar un cultivo activo de la empresa.");
        }

        var selectedPotreros = potreroIds.Distinct().ToList();
        if (selectedPotreros.Count == 0)
        {
            throw new InvalidOperationException("Debe seleccionar al menos un potrero.");
        }

        foreach (var potreroId in selectedPotreros)
        {
            var potrero = potreroService.GetById(potreroId);
            var establecimiento = potrero is null ? null : establecimientoService.GetById(potrero.EstablecimientoId);
            if (potrero is null || !potrero.Activo || establecimiento is null || !establecimiento.Activo || establecimiento.EmpresaId != draft.EmpresaId)
            {
                throw new InvalidOperationException("Solo puede seleccionar potreros activos de la empresa.");
            }

            if (draft.Estado != CampanaEstado.Anulada)
            {
                ValidateNoOverlap(draft, potreroId, ignoredCampanaId);
            }
        }
    }

    private void ValidateNoOverlap(Campana draft, Guid potreroId, Guid ignoredCampanaId)
    {
        var overlaps = campanaPotreros
            .Where(cp => cp.PotreroId == potreroId &&
                         cp.CampanaId != ignoredCampanaId &&
                         cp.Estado != CampanaEstado.Anulada)
            .Select(cp => new { Assignment = cp, Campana = campanas.SingleOrDefault(c => c.Id == cp.CampanaId) })
            .Any(item => item.Campana is not null &&
                         item.Campana.Estado != CampanaEstado.Anulada &&
                         PeriodsOverlap(draft.FechaInicio, draft.FechaFin, item.Assignment.FechaInicio, item.Assignment.FechaFin));

        if (overlaps)
        {
            throw new InvalidOperationException("El potrero seleccionado ya tiene una campaña en ese período.");
        }
    }

    private static bool PeriodsOverlap(DateTime startA, DateTime? endA, DateTime startB, DateTime? endB)
    {
        var effectiveEndA = (endA ?? DateTime.MaxValue).Date;
        var effectiveEndB = (endB ?? DateTime.MaxValue).Date;
        return startA.Date <= effectiveEndB && startB.Date <= effectiveEndA;
    }

    private bool CanAccess(Campana campana) => userScope.CanAccessEmpresa(campana.EmpresaId);

    private void EnsureAccess(Campana campana)
    {
        if (!CanAccess(campana))
        {
            throw new InvalidOperationException("No tiene acceso a la campaña seleccionada.");
        }
    }

    private void EnsureCanManage()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanManage(role, SigaModule.Campanas))
        {
            throw new InvalidOperationException("No tiene permisos para gestionar campañas.");
        }
    }

    private static void EnsureVersion(long currentVersion, long expectedVersion)
    {
        if (expectedVersion != 0 && currentVersion != expectedVersion)
        {
            throw new InvalidOperationException("La campaña fue modificada por otro usuario. Recargue la información e intente nuevamente.");
        }
    }
}
