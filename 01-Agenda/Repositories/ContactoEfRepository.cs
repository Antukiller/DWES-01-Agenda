using _01_Agenda.Error.Common;
using _01_Agenda.Error.Contacto;
using _01_Agenda.Mapper;
using _01_Agenda.Models;
using _01_Agenda.Repositories.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace _01_Agenda.Repositories;

/// <summary>
/// Repositorio de Contactos con Entity Framework Core (SQLite). Implementa ICrudRepository.
/// CAPA DE DATOS + reglas de negocio: persistencia y validaciones de duplicados.
/// </summary>
public class ContactoEfRepository : ICrudRepository {
    private readonly AppDbContext _context;  // el contexto = la "sesión" de conexión a la BD
    private readonly ILogger _logger = Log.ForContext<ContactoEfRepository>(); // logs de Serilog

    public ContactoEfRepository(AppDbContext context, bool dropData = false) {
        _context = context;
        if (dropData) _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    /// <summary>
    /// Consulta diferida: en esta línea NO se ejecuta la BD; se va construyendo el SQL
    /// según agregas filtros (Where), orden (OrderBy) y paginación (Skip/Take).
    /// La BD solo se ejecuta en .ToList().
    /// .AsNoTracking(): EF no vigila las entidades → más rápido y menos memoria. Ideal en lecturas.
    /// </summary>
    public IEnumerable<Contacto> GetAll(int pagina, int tamPagina, bool isDeleteInclude) {
        _logger.Debug("Obteniendo todos los contactos");
        try {
            var query = _context.Contacto.AsNoTracking();
            var entities = query
                .OrderBy(i => i.Id)
                .Skip((pagina - 1) * tamPagina)
                .Take(tamPagina)
                .ToList();
            _logger.Debug("Se han obtenido {Count} contactos exitosamente", entities.Count);
            return entities.Select(i => i.ToModel());
        }
        catch (Exception e) {
            _logger.Error(e, "Error, no se pudieron obtener los contactos");
            return Enumerable.Empty<Contacto>();
        }
    }

    // GET por id → devuelve el contacto o null (el servicio decide el 404).
    public Contacto? GetById(int id) {
        try {
            _logger.Debug("Obteniendo contacto con id: {Id}", id);
            var contacto = _context.Contacto.FirstOrDefault(i => i.Id == id)?.ToModel();
            if (contacto is null)
                return Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound(id.ToString()));
            return Result.Success<Contacto, DomainError>(contacto);
        }
        catch (Exception e) {
            _logger.Error(e, "Error, no se encontro al contacto con ID: {Id}", id);
            return Result.Failure<Contacto, DomainError>(ContactoErrors.DatabaseError(e.Message));
        }
    }

    // POST → crear. Primero valida la regla de negocio: teléfono no duplicado.
    public Result<Contacto, DomainError> Create(Contacto contacto) {
        _logger.Debug("Creando contacto...");
        var exist = ExistsTelefono(contacto.Telefono);
        if (exist)
            return Result.Failure<Contacto, DomainError>(ContactoErrors.TelefonoAlreadyExists(contacto.Telefono));
        contacto = contacto with {
            Id = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        try {
            var entity = contacto.ToEntity();
            _context.Contacto.Add(entity);
            _context.SaveChanges();
            _logger.Debug("Contacto creado correctamente");
            return Result.Success<Contacto, DomainError>(entity.ToModel());
        }
        catch (Exception e) {
            _logger.Error(e, "Error, no se pudo crear al contacto que introducistes");
            return Result.Failure<Contacto, DomainError>(ContactoErrors.DatabaseError(e.Message));
        }
    }

    // PUT → actualizar. NO usa AsNoTracking porque necesitas que EF VIGILE la entidad
    // para detectar los cambios que haces y persistirlos en SaveChanges().
    public Result<Contacto, DomainError> Update(int id, Contacto contacto) {
        try {
            var existente = _context.Contacto.FirstOrDefault(c => c.Id == id);
            if (existente == null)
                return Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound(id.ToString()));

            // Solo comprueba duplicado si el teléfono cambió (si no, se acusaría a sí mismo).
            var telefonoCambiado = existente.Telefono != contacto.Telefono;
            if (telefonoCambiado) {
                var telefonoDuplicado = _context.Contacto
                    .Any(c => c.Telefono == contacto.Telefono && c.Id != id && !c.IsDeleted);
                if (telefonoDuplicado)
                    return Result.Failure<Contacto, DomainError>(ContactoErrors.TelefonoAlreadyExists(contacto.Telefono));
            }

            // Copia los campos permitidos y actualiza la marca temporal.
            existente.Nombre = contacto.Nombre;
            existente.Telefono = contacto.Telefono;
            existente.Email = contacto.Email;
            existente.Alias = contacto.Alias;
            existente.UpdatedAt = DateTime.Now;

            _context.SaveChanges(); // detecta las diferencias y emite un UPDATE en SQL

            _logger.Debug("Contacto con ID {Id} actualizado en DB", id);
            return Result.Success<Contacto, DomainError>(existente);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al actualizar el contacto en EF Core");
            return Result.Failure<Contacto, DomainError>(ContactoErrors.DatabaseError(ex.Message));
        }
    }

    // DELETE → borrar.
    // Lógico: solo marca IsDeleted = true (la fila sigue en la BD).
    // Físico: borra la fila de verdad (Remove).
    public Result<Contacto, DomainError> Delete(int id, bool isLogical = true) {
        try {
            var existente = _context.Contacto.FirstOrDefault(c => c.Id == id);
            if (existente == null)
                return Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound(id.ToString()));

            if (isLogical) {
                existente.IsDeleted = true;
                existente.UpdatedAt = DateTime.Now;
            }
            else {
                _context.Contacto.Remove(existente);
            }

            _context.SaveChanges();

            _logger.Debug("Contacto con ID {Id} eliminado en DB ({Tipo})", id, isLogical ? "lógico" : "físico");
            return Result.Success<Contacto, DomainError>(existente);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al eliminar el contacto en EF Core");
            return Result.Failure<Contacto, DomainError>(ContactoErrors.DatabaseError(ex.Message));
        }
    }

    // GET por alias → devuelve el contacto o null (el servicio decide el 404).
    public Contacto? GetByAlias(string alias) {
        try {
            return _context.Contacto.AsNoTracking().FirstOrDefault(c => c.Alias == alias);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al obtener el contacto por Alias {Alias}", alias);
            return null;
        }
    }

    // ¿Existe un contacto con ese alias?
    public bool ExistsAlias(string alias) {
        try {
            return _context.Contacto.Any(c => c.Alias == alias);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al verificar el Alias del contacto {Alias}", alias);
            return false;
        }
    }

    // ¿Existe un contacto con ese teléfono?
    public bool ExistsTelefono(string telefono) {
        try {
            return _context.Contacto.Any(c => c.Telefono == telefono);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al verificar el Telefono del contacto {Telefono}", telefono);
            return false;
        }
    }
}