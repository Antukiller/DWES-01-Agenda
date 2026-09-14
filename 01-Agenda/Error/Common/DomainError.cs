using _01_Agenda.Models.Enum;

namespace _01_Agenda.Error.Common;

/// <summary>
/// Base de TODOS los errores del dominio de la agenda.
/// Lleva mensaje + código HTTP. Gracias a esto, el Result ya sabe qué "status"
/// devolver sin necesidad de una clase ApiResponse aparte.
/// Cualquier error nuevo debe pasar estos dos datos al constructor.
/// </summary>
public abstract record DomainError(string Message, HttpCodes Code);