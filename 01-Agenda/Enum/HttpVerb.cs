namespace _01_Agenda.Models.Enum;

/// <summary>
/// Los verbos HTTP que tu servicio "simula".
/// Con la "Opción A" (Result + DomainError) no son imprescindibles: el verbo lo dice
/// el nombre del método (GetById = GET, Save = POST...). Los mantienes por si quieres
/// etiquetar una petición o imprimir el verbo junto al código en la salida.
/// </summary>
public enum HttpVerb {
    Get,    // leer datos
    Post,   // crear un recurso
    Put,    // actualizar un recurso existente
    Delete  // borrar un recurso
}