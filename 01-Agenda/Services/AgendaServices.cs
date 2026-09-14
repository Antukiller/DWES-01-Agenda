using _01_Agenda.Cache;
using _01_Agenda.Error.Common;
using _01_Agenda.Error.Contacto;
using _01_Agenda.Models;
using _01_Agenda.Repositories.Base;
using CSharpFunctionalExtensions;

namespace _01_Agenda.Services;

/// <summary>
/// El servicio es la capa intermedia entre "el que llama" (consola) y los datos.
/// Su trabajo: decidir si pregunta a la caché o a la BD, y devolver Result
/// (éxito con Contacto, o error con su código HTTP).
/// Reglas de negocio NO van aquí: están en el repositorio.
/// </summary>
public class AgendaServices(ICrudRepository repository, ICache<int, Contacto> cache) : IAgendaServices {

    // Recorre TODOS los contactos (página enorme) y cuenta. Para tus pruebas de paginación.
    public int TotalContacto => repository.GetAll(1, int.MaxValue, true).Count();

    // GET /contactos → pasa la página al repositorio y devuelve la lista directamente.
    public IEnumerable<Contacto> GetAll(int page = 1, int pageSize = 10, bool includeDeleted = true)
        => repository.GetAll(page, pageSize, includeDeleted);

    // GET /contactos/{id}
    // 1ª pregunta a la caché: si está, devuelves el dato SIN tocar la BD (rápido).
    // 2ª si no está en caché, preguntas a la BD y aprovechas para GUARDARLO en caché
    //    (así la próxima vez el mismo id saldrá sin consultar).
    // 3ª si tampoco está en la BD → error NotFound (su Code es 404).
    public Result<Contacto, DomainError> GetById(int id) {
        if (cache.Get(id) is { } cacheado)
            return Result.Success<Contacto, DomainError>(cacheado);

        if (repository.GetById(id) is { } contacto) {
            cache.Add(id, contacto);
            return Result.Success<Contacto, DomainError>(contacto);
        }

        return Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound(id.ToString()));
    }

    // GET /contactos/alias/{alias} → igual que GetById pero buscando por alias.
    // Lo guardo en caché con su Id para que un GetById posterior también lo encuentre.
    public Result<Contacto, DomainError> GetByAlias(string alias) {
        if (repository.GetByAlias(alias) is { } contacto) {
            cache.Add(contacto.Id, contacto);
            return Result.Success<Contacto, DomainError>(contacto);
        }

        return Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound(alias));
    }

    // POST /contactos → crear.
    // La validación de teléfono duplicado la hace repository.Create (regla de negocio en el repo).
    // Si terminó bien, meto el contacto nuevo en caché (para futuras lecturas).
    public Result<Contacto, DomainError> Save(Contacto contacto) {
        var resultado = repository.Create(contacto);

        if (resultado.IsSuccess)
            cache.Add(resultado.Value.Id, resultado.Value);

        return resultado;
    }

    // PUT /contactos/{id} → actualizar.
    // OJO: al modificar, la copia en caché queda DESACTUALIZADA, así que la borro.
    // El siguiente GetById volverá a leer la BD y guardará la versión nueva.
    public Result<Contacto, DomainError> Update(int id, Contacto contacto) {
        var resultado = repository.Update(id, contacto);

        if (resultado.IsSuccess)
            cache.Remove(id);

        return resultado;
    }

    // DELETE /contactos/{id} → borrar (lógico por defecto: marca IsDeleted=true).
    // Igual que Update: si el contacto desaparece, su copia en caché también debe desaparecer.
    public Result<Contacto, DomainError> Delete(int id, bool isLogical = true) {
        var resultado = repository.Delete(id, isLogical);

        if (resultado.IsSuccess)
            cache.Remove(id);

        return resultado;
    }
}