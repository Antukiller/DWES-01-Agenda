using _01_Agenda.Error.Common;
using _01_Agenda.Models;
using CSharpFunctionalExtensions;

namespace _01_Agenda.Repositories.Base;

/// <summary>
/// Contrato del repositorio: QUÉ operaciones de datos existen, sin importar CÓMO se hacen.
/// La implementación con EF Core es quien se encarga de ejecutarlas contra SQLite.
/// Las reglas de negocio (teléfono/alias duplicado) viven en la implementación.
/// </summary>
public interface ICrudRepository {
    // GET /contactos → lista paginada.
    // isDeleteInclude: true = incluir también los borrados lógicos.
    IEnumerable<Contacto> GetAll(int pagina, int tamPagina, bool isDeleteInclude);

    // GET /contactos/{id} → un contacto, o null si no existe.
    Result<Contacto, DomainError> GetById(int id);

    // POST /contactos → crea. Éxito: Result con el contacto (Id ya asignado por la BD).
    // Teléfono duplicado: error 409.
    Result<Contacto, DomainError> Create(Contacto contacto);

    // PUT /contactos/{id} → actualiza. No existe: 404. Teléfono duplicado: 409.
    Result<Contacto, DomainError> Update(int id, Contacto contacto);

    // DELETE /contactos/{id} → borra lógico (default: marca IsDeleted) o físico (borra la fila).
    // No existe: 404.
    Result<Contacto, DomainError> Delete(int id, bool isLogical = true);

    // GET /contactos/alias/{alias} → un contacto por alias, o null si no existe.
    Contacto? GetByAlias(string alias);

    // ¿Existe ya un contacto con ese alias? (para la regla de negocio "alias único").
    bool ExistsAlias(string alias);
}