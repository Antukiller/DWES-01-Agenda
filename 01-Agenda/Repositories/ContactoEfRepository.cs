using _01_Agenda.Error.Common;
using _01_Agenda.Error.Contacto;
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

    public ContactoEfRepository(AppDbContext context) {
        _context = context;
    }

    /// <summary>
    /// Consulta diferida: en esta línea NO se ejecuta la BD; se va construyendo el SQL
    /// según agregas filtros (Where), orden (OrderBy) y paginación (Skip/Take).
    /// La BD solo se ejecuta en .ToList().
    /// .AsNoTracking(): EF no vigila las entidades → más rápido y menos memoria. Ideal en lecturas.
    /// </summary>
    public IEnumerable<Contacto> GetAll(int pagina, int tamPagina, bool isDeleteInclude) {
        IQueryable<Contacto> consulta = _context.Contacto.AsNoTracking();

        // Por defecto excluye los borrados lógicos, salvo que pida incluirlos.
        if (!isDeleteInclude)
            consulta = consulta.Where(c => !c.IsDeleted);

        // Ordena y pagina ANTES de materializar → se convierte en SQL (ORDER BY + OFFSET/LIMIT).
        return consulta
            .OrderBy(c => c.Id)
            .Skip((pagina - 1) * tamPagina)
            .Take(tamPagina)
            .ToList();
    }

    // GET por id → devuelve el contacto o null (el servicio decide el 404).
    public Contacto? GetById(int id) {
        try {
            return _context.Contacto.AsNoTracking().FirstOrDefault(c => c.Id == id);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al obtener el contacto por ID {Id}", id);
            return null;
        }
    }

    // POST → crear. Primero valida la regla de negocio: teléfono no duplicado.
    public Result<Contacto, DomainError> Create(Contacto contacto) {
        try {
            // REGLA DE NEGOCIO: no se puede guardar un teléfono ya usado por otro contacto.
            var telefonoDuplicado = _context.Contacto.Any(c => c.Telefono == contacto.Telefono && !c.IsDeleted);
            if (telefonoDuplicado)
                return Result.Failure<Contacto, DomainError>(ContactoErrors.TelefonoAlreadyExists(contacto.Telefono));

            contacto.CreatedAt = DateTime.Now;   // marca temporal de creación
            contacto.UpdatedAt = DateTime.Now;

            _context.Contacto.Add(contacto);      // lo marca como "nuevo" en el contexto
            _context.SaveChanges();               // lo inserta; aquí la BD asigna el Id

            _logger.Debug("Contacto creado en DB con ID {Id}", contacto.Id);
            return Result.Success<Contacto, DomainError>(contacto);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al crear el contacto en EF Core");
            return Result.Failure<Contacto, DomainError>(ContactoErrors.DatabaseError(ex.Message));
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