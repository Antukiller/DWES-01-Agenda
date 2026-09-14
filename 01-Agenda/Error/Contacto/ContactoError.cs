using _01_Agenda.Error.Common;
using _01_Agenda.Models.Enum;

namespace _01_Agenda.Error.Contacto;


/// <summary>
/// Contenedor de errores específicos del dominio de Contactos.
/// Cada error concreto hereda de este y solo aporta su mensaje y su código HTTP.
/// </summary>
public abstract record ContactoError(string Message, HttpCodes Code) : DomainError(Message, Code) {

    /// <summary>Error: contacto no encontrado por ID. → 404</summary>
    public sealed record NotFound(string Id)
        : ContactoError($"No se encontró el contacto con ID {Id}", HttpCodes.NotFound);


    /// <summary>Error: ya existe un contacto con ese alias. → 409</summary>
    public sealed record AliasAlreadyExists(string Alias)
        : ContactoError($"Ya existe un contacto con el alias {Alias}.", HttpCodes.Conflict);

    /// <summary>Error: el teléfono ya está registrado en otro contacto. → 409</summary>
    public sealed record TelefonoAlreadyExists(string Telefono)
        : ContactoError($"Ya existe un contacto con el teléfono {Telefono}.", HttpCodes.Conflict);

    /// <summary>Error de base de datos. → 500</summary>
    public sealed record Database(string Details)
        : ContactoError($"Error de base de datos: {Details}", HttpCodes.InternalServerError);
}

/// <summary>
/// Factory para crear errores de dominio de Contacto.
/// Te ahorra acordarte del nombre de cada record: llamas a ContactoErrors.Xyz().
/// </summary>
public static class ContactoErrors {

    /// <summary>Crea un error de contacto no encontrado.</summary>
    public static DomainError NotFound(string id) {
        return new ContactoError.NotFound(id);
    }

    /// <summary>Crea un error de alias duplicado.</summary>
    public static DomainError AliasAlreadyExists(string alias) {
        return new ContactoError.AliasAlreadyExists(alias);
    }

    /// <summary>Crea un error de teléfono duplicado.</summary>
    public static DomainError TelefonoAlreadyExists(string telefono) {
        return new ContactoError.TelefonoAlreadyExists(telefono);
    }

    /// <summary>Crea un error de la base de datos.</summary>
    public static DomainError DatabaseError(string details) {
        return new ContactoError.Database(details);
    }
}