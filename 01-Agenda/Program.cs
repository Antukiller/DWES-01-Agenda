using _01_Agenda.Cache;
using _01_Agenda.Error.Common;
using _01_Agenda.Models;
using _01_Agenda.Models.Enum;
using _01_Agenda.Repositories;
using _01_Agenda.Services;
using CSharpFunctionalExtensions;

// ─── CONFIGURACIÓN ─────────────────────────────────────────────
// BD SQLite (se crea el fichero agenda.db en la carpeta de la app)
var db = new AppDbContext("Data Source=agenda.db");
db.EnsureCreated(); // crea BD y tablas si no existen

var cache = new LruCache<int, Contacto>(10);   // caché LRU con capacidad 10
IAgendaServices service = new AgendaServices(
    new ContactoEfRepository(db), cache);      // el servicio se monta sobre repo + caché

// Helper: convierte un HttpVerb en el texto del verbo HTTP (capa de "rutas").
// Es un switch: añadir un verbo nuevo = añadir un case. Así "se cambia" fácil.
string Verbo(HttpVerb v) => v switch {
    HttpVerb.Post   => "POST",
    HttpVerb.Get    => "GET",
    HttpVerb.Put    => "PUT",
    HttpVerb.Delete => "DELETE",
    _               => "?"   // por si algún día metes un verbo nuevo sin ponerle case
};

// Helper: pinta una respuesta Result como si fuera un log de servidor.
// codeOk es el código HTTP "esperado" en caso de ÉXITO (Created para POST, Ok para el resto).
void Mostrar(HttpVerb verbo, HttpCodes codeOk, Result<Contacto, DomainError> r) {
    if (r.IsSuccess) {
        var c = r.Value;
        Console.WriteLine($"[{Verbo(verbo)} {(int)codeOk} {codeOk}] {c.Id} | {c.Nombre} | {c.Alias} | {c.Telefono}");
    }
    else {
        Console.WriteLine($"[{Verbo(verbo)} {(int)r.Error.Code} {r.Error.Code}] {r.Error.Message}");
    }
}

// ─── POST /contactos  (crear) ──────────────────────────────────
Console.WriteLine("\n== POST /contactos ==");
Mostrar(HttpVerb.Post, HttpCodes.Created, service.Save(new Contacto {
    Nombre = "Ana García", Telefono = "600111222", Email = "ana@mail.com", Alias = "anni" }));
Mostrar(HttpVerb.Post, HttpCodes.Created, service.Save(new Contacto {
    Nombre = "Luis Pérez", Telefono = "600111333", Email = "luis@mail.com", Alias = "lucho" }));
Mostrar(HttpVerb.Post, HttpCodes.Created, service.Save(new Contacto {
    Nombre = "Marta Ruiz", Telefono = "600111444", Email = "marta@mail.com", Alias = "marti" }));

// Caso negativo: teléfono duplicado → 409 Conflict (regla del repositorio)
Mostrar(HttpVerb.Post, HttpCodes.Created, service.Save(new Contacto {
    Nombre = "Clon", Telefono = "600111222", Email = "clon@mail.com", Alias = "clon" }));

// ─── GET /contactos/{id}  (leer uno) ───────────────────────────
Console.WriteLine("\n== GET /contactos/{id} ==");
Mostrar(HttpVerb.Get, HttpCodes.Ok, service.GetById(1));   // 1ª vez: no está en caché → consulta BD y la rellena
Mostrar(HttpVerb.Get, HttpCodes.Ok, service.GetById(1));   // 2ª vez: sale de la CACHÉ (mismo resultado, sin tocar BD)
Mostrar(HttpVerb.Get, HttpCodes.Ok, service.GetById(999)); // no existe → 404 NotFound

// ─── GET /contactos/alias/{alias} ─────────────────────────────
Console.WriteLine("\n== GET /contactos/alias/{alias} ==");
Mostrar(HttpVerb.Get, HttpCodes.Ok, service.GetByAlias("lucho"));    // existe → 200
Mostrar(HttpVerb.Get, HttpCodes.Ok, service.GetByAlias("noexiste")); // 404

// ─── GET /contactos  (listar) ──────────────────────────────────
Console.WriteLine("\n== GET /contactos (sin borrados) ==");
foreach (var c in service.GetAll(includeDeleted: false))
    Console.WriteLine($"  {c.Id} | {c.Nombre} | {c.Alias} | {c.Telefono}");
Console.WriteLine($"  (TotalContacto: {service.TotalContacto})");

// ─── PUT /contactos/{id}  (actualizar) ─────────────────────────
Console.WriteLine("\n== PUT /contactos/{id} ==");
Mostrar(HttpVerb.Put, HttpCodes.Ok, service.Update(2, new Contacto {
    Nombre = "Luis Pérez Vega", Telefono = "600111333", Email = "luisv@mail.com", Alias = "luigui" }));

// Caso negativo: el contacto 3 toma el teléfono del 1 → 409
Mostrar(HttpVerb.Put, HttpCodes.Ok, service.Update(3, new Contacto {
    Nombre = "Marta Ruiz", Telefono = "600111222", Email = "marta@mail.com", Alias = "marti" }));

// ─── DELETE /contactos/{id}  (borrar) ──────────────────────────
Console.WriteLine("\n== DELETE /contactos/{id} ==");
Mostrar(HttpVerb.Delete, HttpCodes.Ok, service.Delete(3));   // borrado lógico (IsDeleted = true)
Mostrar(HttpVerb.Delete, HttpCodes.Ok, service.Delete(999)); // no existe → 404

// ─── Capa de RUTAS: petición MAL FORMADA → 400 ─────────────────
// Aquí sí se usa HttpCodes.BadRequest: la ruta llega SIN el {id} obligatorio
// ("PUT /contactos/" en vez de "PUT /contactos/5"). Es un problema de la
// RUTA, no del dominio: el servicio ni se entera. Por eso se rechaza en
// la capa de rutas con 400, antes de intentar siquiera llamar al servicio.
Console.WriteLine("\n== PUT /contactos/ (sin {id}) ==");
string idSolicitado = "";                                     // el segmento {id} viene vacío
if (string.IsNullOrWhiteSpace(idSolicitado))
    Console.WriteLine($"[{Verbo(HttpVerb.Put)} {(int)HttpCodes.BadRequest} {HttpCodes.BadRequest}] falta el {{id}} en la ruta");
else
    Mostrar(HttpVerb.Put, HttpCodes.Ok, service.Update(int.Parse(idSolicitado), new Contacto()));
