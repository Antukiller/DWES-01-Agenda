using _01_Agenda.Error.Common;

namespace _01_Agenda.Error.Cita;


/// <summary>
/// Contenedor de errores específicos para el dominio de Vehículos.
/// </summary>
public abstract record ContactoError(string Message) : DomainError(Message) {

    /// <summary>Error: cita no encontrada por ID.</summary>
    public sealed record NotFound(string Id)
        : ContactoError($"No se encontró la cita con ID {Id}");
    

    /// <summary>Error: ya existe una cita programada con esa matrícula para esa fecha.</summary>
    public sealed record AliasAlreadyExists(string Alias)
        : ContactoError($"La matrícula {Alias} ya tiene programada una cita para esa fecha.");

    /// <summary>Error: el DNI del propietario ya está registrado.</summary>
    public sealed record TelefonoAlreadyExists(string Telefono)
        : ContactoError(
            $"Conflicto de integridad: El DNI del propietario {Telefono} ya está registrado en el sistema.");

    /// <summary>Error de base de datos.</summary>
    public sealed record Database(string Details)
        : ContactoError($"Error de base de datos: {Details}");
    
}

/// <summary>
/// Factory para crear errores de dominio de Vehículo.
/// </summary>
public static class CitaErrors {

    /// <summary>Crea un error de cita no encontrada.</summary>
    public static DomainError NotFound(string id) {
        return new ContactoError.NotFound(id);
    }
    

    /// <summary>Crea un error de matrícula duplicada.</summary>
    public static DomainError AliasAlreadyExists(string alias) {
        return new ContactoError.AliasAlreadyExists(alias);
    }

    /// <summary>Crea un error de DNI de propietario duplicado.</summary>
    public static DomainError TelefonoAlreadyExists(string Telefono) {
        return new ContactoError.TelefonoAlreadyExists(Telefono);
    }
    
    

    /// <summary>Crea un error de base de datos.</summary>
    public static DomainError DatabaseError(string details) {
        return new ContactoError.Database(details);
    }
}
