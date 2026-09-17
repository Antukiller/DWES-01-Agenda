using _01_Agenda.Cache;
using _01_Agenda.Error.Common;
using _01_Agenda.Error.Contacto;
using _01_Agenda.Models;
using _01_Agenda.Repositories.Base;
using CSharpFunctionalExtensions;

namespace _01_Agenda.Services;

public class AgendaServices(ICrudRepository repository, ICache<int, Contacto> cache) : IAgendaServices {

    public int TotalContacto => repository.GetAll(1, int.MaxValue, true).Count();

    public IEnumerable<Contacto> GetAll(int page = 1, int pageSize = 10, bool includeDeleted = true)
        => repository.GetAll(page, pageSize, includeDeleted);

    public Result<Contacto, DomainError> GetById(int id) {
        if (cache.Get(id) is { } cached) return Result.Success<Contacto, DomainError>(cached);
        
        return repository.GetById(id)
            .Tap(c => cache.Add(id, c));
    }

    public Result<Contacto, DomainError> GetByAlias(string alias) {
        var contacto = repository.GetByAlias(alias);
        if (contacto is { } c) {
            // Guardamos en caché para lecturas posteriores por ID
            cache.Add(c.Id, c);
            return Result.Success<Contacto, DomainError>(c);
        }

        return Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound(alias));
    }

    public Result<Contacto, DomainError> Save(Contacto contacto) {
        return repository.Create(contacto)
            .Tap(c => cache.Add(c.Id, c)); // Guardar en caché tras crear
    }

    public Result<Contacto, DomainError> Update(int id, Contacto contacto) {
        return repository.Update(id, contacto)
            .Tap(c => cache.Remove(id)); // Eliminar de la caché solo si la actualización en repositorio fue exitosa
    }

    public Result<Contacto, DomainError> Delete(int id, bool isLogical = true) {
        return repository.Delete(id, isLogical) // Pasar isLogical correctamente al repositorio
            .Tap(c => cache.Remove(id)); // Eliminar de la caché solo si el borrado fue exitoso
    }
}