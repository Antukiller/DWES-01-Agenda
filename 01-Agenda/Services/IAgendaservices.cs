using _01_Agenda.Error.Common;
using _01_Agenda.Models;
using CSharpFunctionalExtensions;

namespace _01_Agenda.Services;

/// <summary>
/// Contrato del servicio: esto es lo que consume Program (o quien sea).
/// Result&lt;Contacto, DomainError&gt; = éxito con el Contacto, o error con su código HTTP (Error.Code).
/// </summary>
public interface IAgendaServices {

    // Total de contactos existentes (te sirve para paginación).
    int TotalContacto { get; }

    // GET /contactos → lista paginada.
    IEnumerable<Contacto> GetAll(int page = 1,
        int pageSize = 10,
        bool includeDeleted = true);

    // GET /contactos/{id} → un contacto por su id. Éxito: 200. No existe: 404.
    Result<Contacto, DomainError> GetById(int id);

    // GET /contactos/alias/{email} → igual, pero buscando por alias. Éxito: 200. No existe: 404.
    Result<Contacto, DomainError> GetByAlias(string email);

    // POST /contactos → crea nuevo. Éxito: 201. Teléfono/alias duplicado: 409.
    Result<Contacto, DomainError> Save(Contacto contacto);

    // PUT /contactos/{id} → actualiza. Éxito: 200. No existe: 404. Duplicado: 409.
    Result<Contacto, DomainError> Update(int id, Contacto contacto);

    // DELETE /contactos/{id} → borra lógico (marca IsDeleted) por defecto. Éxito: 200. No existe: 404.
    Result<Contacto, DomainError> Delete(int id, bool isLogical = true);
}