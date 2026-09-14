namespace _01_Agenda.Models.Enum;

/// <summary>
/// Códigos de estado HTTP "devueltos" por el servicio.
/// El número es el valor real del enum: ((int)HttpCodes.NotFound == 404).
/// Así en consola puedes imprimir el número y su nombre.
/// </summary>
public enum HttpCodes {
    Ok = 200,                  // Lectura / actualización correcta (GET, PUT, DELETE)
    Created = 201,             // Creación correcta (POST)
    BadRequest = 400,          // Petición mal formada (lo tienes disponible por si lo usas)
    NotFound = 404,            // El contacto no existe
    Conflict = 409,            // Teléfono o alias duplicado
    InternalServerError = 500  // Error inesperado o de base de datos
}