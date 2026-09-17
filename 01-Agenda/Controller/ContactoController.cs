using System.Text.Json;
using _01_Agenda.Dto;
using _01_Agenda.Models;
using _01_Agenda.Models.Enum;
using _01_Agenda.Services;

namespace _01_Agenda.Controller;

public class ContactoController {
    private readonly IAgendaServices _services;
    
    public ContactoController(IAgendaServices services) => _services = services;

    public ResponseDto Dispatch(ResquestDto req) {
        return req.Verbo switch {
            HttpVerb.Get => Get(req.Contenido),
            HttpVerb.Post => Post(req.Contenido),
            HttpVerb.Put => Put(req.Contenido),
            HttpVerb.Delete => Delete(req.Contenido),
            _ => new ResponseDto(HttpCodes.BadRequest, "{}")
        };
    }

    private ResponseDto Get(string json) {
        // Caso 1: Viene vacío o "{}" -> El usuario pide TODOS los contactos (GetAll)
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}" || json.Trim() == "null") {
            var contactos = _services.GetAll();
            return new ResponseDto(HttpCodes.Ok, JsonSerializer.Serialize(contactos));
        }

        // Caso 2: Viene un número (ej. "5") -> El usuario pide por ID (GetById)
        if (int.TryParse(json.Trim(), out int id)) {
            var r = _services.GetById(id);
            return r.IsSuccess
                ? new ResponseDto(HttpCodes.Ok, JsonSerializer.Serialize(r.Value))
                : new ResponseDto(r.Error.Code, r.Error.Message);
        }

        // Caso 3: Viene un texto plano -> Asumimos que busca por Alias (GetByAlias)
        // (O si fuera un JSON más complejo, podrías deserializar parámetros de paginación)
        var alias = json.Replace("\"", "").Trim();
        var resultAlias = _services.GetByAlias(alias);

        return resultAlias.IsSuccess
            ? new ResponseDto(HttpCodes.Ok, JsonSerializer.Serialize(resultAlias.Value))
            : new ResponseDto(resultAlias.Error.Code, resultAlias.Error.Message);
    }
    
    // POST /contactos
    private ResponseDto Post(string json) {
        var c = JsonSerializer.Deserialize<Contacto>(json);
        var r = _services.Save(c!);
        return r.IsSuccess
            ? new ResponseDto(HttpCodes.Created, JsonSerializer.Serialize(r.Value))
            : new ResponseDto(r.Error.Code, r.Error.Message);  
    }


    private ResponseDto Put(string json) {
        var c = JsonSerializer.Deserialize<Contacto>(json);
        var r = _services.Update(c!.Id, c);
        return r.IsSuccess
            ? new ResponseDto(HttpCodes.Ok, JsonSerializer.Serialize(r.Value))
            : new ResponseDto(r.Error.Code, r.Error.Message);
    }

    private ResponseDto Delete(string json) {
        var id = JsonSerializer.Deserialize<int>(json);
        var r = _services.Delete(id);
        
        return r.IsSuccess
            ? new ResponseDto(HttpCodes.Ok, "{}")
            : new ResponseDto(r.Error.Code, r.Error.Message);
    }
}