using _01_Agenda.Error.Common;
using _01_Agenda.Error.Contacto;
using _01_Agenda.Mapper;
using _01_Agenda.Models;
using _01_Agenda.Repositories.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace _01_Agenda.Repositories;

public class ContactoEfRepository : ICrudRepository {
    private readonly AppDbContext _context;
    private readonly ILogger _logger = Log.ForContext<ContactoEfRepository>();

    public ContactoEfRepository(AppDbContext context, bool dropData = false) {
        _context = context;
        if (dropData) _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    public IEnumerable<Contacto> GetAll(int pagina, int tamPagina, bool isDeleteInclude) {
        _logger.Debug("Obteniendo todos los contactos");
        try {
            var query = _context.Contacto.AsNoTracking();
            
            // Si NO se deben incluir los borrados lógicos, filtramos
            if (!isDeleteInclude) {
                query = query.Where(c => !c.IsDeleted);
            }

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

    public Result<Contacto, DomainError> GetById(int id) {
        try {
            var entity = _context.Contacto.AsNoTracking().FirstOrDefault(c => c.Id == id);
            if (entity == null) {
                return Result.Failure<Contacto, DomainError>(new ContactoError.NotFound(id.ToString()));
            }
            return Result.Success<Contacto, DomainError>(entity.ToModel());
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al obtener contacto por ID {Id}", id);
            return Result.Failure<Contacto, DomainError>(new ContactoError.Database(ex.Message));
        }
    }

    public Result<Contacto, DomainError> Create(Contacto contacto) {
        _logger.Debug("Creando contacto...");
        if (ExistsTelefono(contacto.Telefono))
            return Result.Failure<Contacto, DomainError>(ContactoErrors.TelefonoAlreadyExists(contacto.Telefono));

        contacto.Id = 0;
        contacto.CreatedAt = DateTime.UtcNow;
        contacto.UpdatedAt = DateTime.UtcNow;
        contacto.IsDeleted = false;
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

    public Result<Contacto, DomainError> Update(int id, Contacto contacto) {
        _logger.Debug("Actualizando contacto con id: {Id}", id);
        try {
            var entity = _context.Contacto.FirstOrDefault(i => i.Id == id);
            if (entity == null)
                return Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound(id.ToString()));

            // Validación: ¿El teléfono ya le pertenece a OTRO contacto?
            if (_context.Contacto.Any(c => c.Telefono == contacto.Telefono && c.Id != id && !c.IsDeleted)) {
                return Result.Failure<Contacto, DomainError>(ContactoErrors.TelefonoAlreadyExists(contacto.Telefono));
            }

            entity.Nombre = contacto.Nombre;
            entity.Alias = contacto.Alias;
            entity.Telefono = contacto.Telefono;
            entity.Email = contacto.Email;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();
            _logger.Debug("Contacto actualizado correctamente");
            return Result.Success<Contacto, DomainError>(entity.ToModel());
        }
        catch (Exception e) {
            _logger.Error(e, "Error, no se pudo actualizar el contacto con ID: {Id}", id);
            return Result.Failure<Contacto, DomainError>(ContactoErrors.DatabaseError(e.Message));
        }
    }

    public Result<Contacto, DomainError> Delete(int id, bool isLogical = true) {
        try {
            _logger.Debug("Eliminando contacto con id: {Id}", id);
            var entity = _context.Contacto.FirstOrDefault(i => i.Id == id);
            if (entity == null) 
                return Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound(id.ToString()));

            if (isLogical) {
                // Borrado LÓGICO
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
            } else {
                // Borrado FÍSICO
                _context.Contacto.Remove(entity);
            }

            _context.SaveChanges();
            _logger.Debug("Contacto eliminado correctamente");
            return Result.Success<Contacto, DomainError>(entity.ToModel());
        }
        catch (Exception e) {
            _logger.Error(e, "Error, no se pudo eliminar el contacto con ID: {Id}", id);
            return Result.Failure<Contacto, DomainError>(ContactoErrors.DatabaseError(e.Message));
        }
    }

    public Contacto? GetByAlias(string alias) {
        try {
            return _context.Contacto.AsNoTracking().FirstOrDefault(c => c.Alias == alias)?.ToModel();
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al obtener el contacto por Alias {Alias}", alias);
            return null;
        }
    }

    public bool ExistsAlias(string alias) {
        try {
            return _context.Contacto.Any(c => c.Alias == alias);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al verificar el Alias del contacto {Alias}", alias);
            return false;
        }
    }

    // En ContactoEfRepository.cs

    public bool ExistsTelefono(string telefono) {
        try {
            // Solo cuenta si el contacto NO ha sido borrado lógicamente
            return _context.Contacto.Any(c => c.Telefono == telefono && !c.IsDeleted);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al verificar el Telefono del contacto {Telefono}", telefono);
            return false;
        }
    }
}