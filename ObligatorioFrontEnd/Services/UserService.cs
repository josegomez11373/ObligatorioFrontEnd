using System.Net.Mail;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class UserService : IServicioUsuarios
{
    private readonly DemoAuthService auth;
    private readonly IUserScope userScope;
    private readonly EstablecimientoService establecimientoService;
    private readonly EmpresaService empresaService;
    private readonly AuditService audit;
    private readonly NotificadorDatos notificador;

    public UserService(
        DemoAuthService auth,
        IUserScope userScope,
        EstablecimientoService establecimientoService,
        EmpresaService empresaService,
        AuditService audit,
        NotificadorDatos notificador)
    {
        this.auth = auth;
        this.userScope = userScope;
        this.establecimientoService = establecimientoService;
        this.empresaService = empresaService;
        this.audit = audit;
        this.notificador = notificador;
    }

    public event Action? StateChanged;

    public IReadOnlyList<Usuario> GetAll(bool includeInactive = false)
    {
        var current = userScope.CurrentUser;
        if (current is null) return [];

        return auth.UserRecords
            .Where(user => includeInactive || user.Activo)
            .Where(user => current.Role == UserRole.AdministradorGeneral ||
                           (user.EmpresaId is Guid empresaId && empresaId == current.EmpresaId))
            .OrderBy(user => user.Nombre)
            .ToList();
    }

    public Usuario? GetById(Guid id) => GetAll(true).SingleOrDefault(user => user.Id == id);

    public Usuario Create(Usuario draft)
    {
        EnsureCanManage();
        ValidateUser(draft, isNew: true);

        if (auth.UserRecords.Any(user => user.Email.Equals(draft.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        }

        var user = draft with
        {
            Id = Guid.NewGuid(),
            Email = draft.Email.Trim(),
            Nombre = draft.Nombre.Trim(),
            EstablecimientoIds = draft.EstablecimientoIds.Distinct().ToList(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userScope.CurrentUser?.Email,
            UpdatedAt = null,
            UpdatedBy = null,
            Version = 1
        };
        auth.AddUser(user);
        audit.Record(nameof(Usuario), user.Id, "Crear", user.EmpresaId, null, user);
        Notify();
        return user;
    }

    public bool Update(Usuario updated, long expectedVersion = 0)
    {
        EnsureCanManage();
        var current = auth.UserRecords.SingleOrDefault(user => user.Id == updated.Id);
        if (current is null) return false;

        EnsureVisible(current);
        EnsureVersion(current.Version, expectedVersion);
        ValidateUser(updated, isNew: false, existing: current);

        if (auth.UserRecords.Any(user => user.Id != updated.Id &&
                                         user.Email.Equals(updated.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        }

        if (current.EmpresaId != updated.EmpresaId && userScope.CurrentUser?.Role != UserRole.AdministradorGeneral)
        {
            throw new InvalidOperationException("No puede cambiar la empresa de un usuario.");
        }

        if (current.Id == userScope.CurrentUser?.Id && current.Rol != updated.Rol)
        {
            throw new InvalidOperationException("No puede cambiar su propio rol.");
        }

        var replacement = current with
        {
            Email = updated.Email.Trim(),
            Nombre = updated.Nombre.Trim(),
            Rol = updated.Rol,
            EmpresaId = updated.EmpresaId,
            EstablecimientoIds = updated.EstablecimientoIds.Distinct().ToList(),
            Activo = updated.Activo,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        auth.ReplaceUser(replacement);
        audit.Record(nameof(Usuario), current.Id, updated.Activo ? "Actualizar" : "Desactivar", current.EmpresaId, current, replacement);
        Notify();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        EnsureCanManage();
        var current = auth.UserRecords.SingleOrDefault(user => user.Id == id);
        if (current is null) return false;

        EnsureVisible(current);
        if (current.Id == userScope.CurrentUser?.Id)
        {
            throw new InvalidOperationException("No puede desactivar el usuario de la sesión actual.");
        }

        if (!current.Activo) return true;

        if (current.Rol == UserRole.AdministradorGeneral &&
            auth.UserRecords.Count(user => user.Rol == UserRole.AdministradorGeneral && user.Activo) <= 1)
        {
            throw new InvalidOperationException("Debe quedar al menos un administrador general activo.");
        }

        var replacement = current with
        {
            Activo = false,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = current.Version + 1
        };
        auth.ReplaceUser(replacement);
        audit.Record(nameof(Usuario), current.Id, "Desactivar", current.EmpresaId, current, replacement);
        Notify();
        return true;
    }

    public Task<IReadOnlyList<UsuarioDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UsuarioDto> result = GetAll(includeInactive: incluirInactivos)
            .Select(ADto)
            .Where(dto => dto is not null)
            .Cast<UsuarioDto>()
            .ToList();

        return Task.FromResult(result);
    }

    public Task<UsuarioDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var usuario = GetById(id);
        return Task.FromResult(usuario is null ? null : ADto(usuario));
    }

    public Task<UsuarioDto> CreateAsync(
        CrearUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        var rol = dto.Rol?.AModelo()
            ?? throw new InvalidOperationException("Debe indicar el rol del usuario.");

        // El administrador general no pertenece a ninguna empresa.
        Guid? empresaId = rol == UserRole.AdministradorGeneral
            ? null
            : dto.EmpresaId ?? userScope.CurrentUser?.EmpresaId;

        var creado = Create(new Usuario
        {
            Email = dto.Email,
            Nombre = dto.Nombre,
            Rol = rol,
            EmpresaId = empresaId,
            EstablecimientoIds = dto.EstablecimientoIds
        });

        // El mock usa una contraseña fija para todos los usuarios: no se almacena.
        return Task.FromResult(ADto(creado)!);
    }

    public Task<UsuarioDto?> UpdateAsync(
        ActualizarUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        var current = auth.UserRecords.SingleOrDefault(usuario => usuario.Id == dto.Id);
        if (current is null)
        {
            return Task.FromResult<UsuarioDto?>(null);
        }

        var rol = dto.Rol?.AModelo() ?? current.Rol;
        Guid? empresaId = rol == UserRole.AdministradorGeneral
            ? null
            : dto.EmpresaId ?? current.EmpresaId;

        var actualizado = Update(
            new Usuario
            {
                Id = dto.Id,
                Email = current.Email,
                Nombre = dto.Nombre,
                Rol = rol,
                EmpresaId = empresaId,
                EstablecimientoIds = dto.EstablecimientoIds,
                Activo = dto.Activo
            },
            dto.Version);

        return Task.FromResult(
            actualizado ? ADto(auth.UserRecords.Single(usuario => usuario.Id == dto.Id)) : null);
    }

    public Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Deactivate(id));

    public Task<UsuarioDto?> CambiarContrasenaAsync(
        Guid id,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        // El mock no guarda contraseñas por usuario; la operación es válida pero no altera el login demo.
        var usuario = auth.UserRecords.SingleOrDefault(candidate => candidate.Id == id);
        return Task.FromResult(usuario is null ? null : ADto(usuario));
    }

    private UsuarioDto? ADto(Usuario usuario)
    {
        var empresaNombre = usuario.EmpresaId is Guid empresaId
            ? empresaService.GetById(empresaId)?.Nombre
            : null;

        var establecimientos = usuario.EstablecimientoIds
            .Select(establecimientoService.GetById)
            .Where(establecimiento => establecimiento is not null)
            .Select(establecimiento => establecimiento!.AContrato());

        return usuario.AContrato(empresaNombre, establecimientos);
    }

    private void Notify()
    {
        StateChanged?.Invoke();
        notificador.Notificar(ModulosDatos.Usuarios);
    }

    private void ValidateUser(Usuario user, bool isNew, Usuario? existing = null)
    {
        var current = userScope.CurrentUser
            ?? throw new InvalidOperationException("No hay una sesión activa.");

        if (string.IsNullOrWhiteSpace(user.Nombre))
        {
            throw new InvalidOperationException("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("El correo es obligatorio.");
        }

        try
        {
            _ = new MailAddress(user.Email.Trim());
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("El correo no tiene un formato válido.");
        }

        if (current.Role == UserRole.AdministradorEmpresa)
        {
            if (user.Rol is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa)
            {
                if (existing is null || existing.Id != current.Id)
                {
                    throw new InvalidOperationException("Solo un administrador general puede gestionar cuentas de administrador.");
                }
            }
        }

        if (user.Rol == UserRole.AdministradorGeneral)
        {
            if (current.Role != UserRole.AdministradorGeneral)
            {
                throw new InvalidOperationException("Solo un administrador general puede asignar ese rol.");
            }

            if (user.EmpresaId is not null || user.EstablecimientoIds.Count > 0)
            {
                throw new InvalidOperationException("El administrador general no puede tener un alcance empresarial.");
            }

            return;
        }

        if (user.EmpresaId is not Guid empresaId)
        {
            throw new InvalidOperationException("Debe seleccionar una empresa dentro de su alcance.");
        }

        if (!userScope.CanAccessEmpresa(empresaId))
        {
            throw new InvalidOperationException("No tiene acceso a la empresa seleccionada.");
        }

        var empresa = empresaService.GetById(empresaId);
        if (empresa is null || (!empresa.Activo && user.Activo))
        {
            throw new InvalidOperationException("La empresa seleccionada no está disponible.");
        }

        if (current.Role == UserRole.AdministradorEmpresa && empresaId != current.EmpresaId)
        {
            throw new InvalidOperationException("Solo puede gestionar usuarios de su empresa.");
        }

        if (user.EstablecimientoIds.Any(id => establecimientoService.GetById(id)?.EmpresaId != empresaId))
        {
            throw new InvalidOperationException("Todos los establecimientos deben pertenecer a la empresa seleccionada.");
        }
    }

    private void EnsureVisible(Usuario user)
    {
        var current = userScope.CurrentUser;
        if (current is null)
        {
            throw new InvalidOperationException("No hay una sesión activa.");
        }

        if (current.Role == UserRole.AdministradorGeneral) return;
        if (user.EmpresaId is not Guid empresaId || empresaId != current.EmpresaId)
        {
            throw new InvalidOperationException("No tiene acceso a ese usuario.");
        }

        if (user.Rol == UserRole.AdministradorEmpresa && user.Id != current.Id)
        {
            throw new InvalidOperationException("Solo un administrador general puede gestionar otras cuentas de administrador.");
        }
    }

    private void EnsureCanManage()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanManage(role, SigaModule.Usuarios))
        {
            throw new InvalidOperationException("No tiene permisos para gestionar usuarios.");
        }
    }

    private static void EnsureVersion(long current, long expected)
    {
        if (expected != 0 && current != expected)
        {
            throw new InvalidOperationException("El usuario fue modificado por otro usuario. Recargue la información e intente nuevamente.");
        }
    }
}
