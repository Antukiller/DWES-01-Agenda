using _01_Agenda.Cache;
using _01_Agenda.Error.Common;
using _01_Agenda.Models;
using _01_Agenda.Repositories;
using _01_Agenda.Services;
using CSharpFunctionalExtensions;

// ─── CONFIGURACIÓN ─────────────────────────────────────────────
// BD SQLite (se crea el fichero agenda.db en la carpeta de la app)
var db = new AppDbContext("Data Source=agenda.db");
db.EnsureCreated(); // crea BD y tablas si no existen

var cache = new LruCache<int, Contacto>(10);                    // caché LRU con capacidad 10
IAgendaServices service = new AgendaServices(
    new ContactoEfRepository(db), cache);                        // el servicio se monta sobre repo + caché

// Helper: pinta una respuesta Result como si fuera un log de servidor
void Mostrar(string verbo, int codeOk, Result<Contacto, DomainError> r) {
    if (r.IsSuccess) {
        var c = r.Value;
        Console.WriteLine($"[{verbo} {codeOk} Ok] {c.Id} | {c.Nombre} | {c.Alias} | {c.Telefono}");
    }
    else {
        Console.WriteLine($"[{verbo} {(int)r.Error.Code} {r.Error.Code}] {r.Error.Message}");
    }
}

// ─── POST /contactos  (crear) ──────────────────────────────────
Console.WriteLine("== POST /contactos ==");
Mostrar("POST", 201, service.Save(new Contacto {
    Nombre = "Ana García", Telefono = "600111222", Email = "ana@mail.com", Alias = "anni" }));
Mostrar("POST", 201, service.Save(new Contacto {
    Nombre = "Luis Pérez", Telefono = "600111333", Email = "luis@mail.com", Alias = "lucho" }));
Mostrar("POST", 201, service.Save(new Contacto {
    Nombre = "Marta Ruiz", Telefono = "600111444", Email = "marta@mail.com", Alias = "marti" }));

// Caso negativo: teléfono duplicado → 409 Conflict (regla del repositorio)
Mostrar("POST", 201, service.Save(new Contacto {
    Nombre = "Clon", Telefono = "600111222", Email = "clon@mail.com", Alias = "clon" }));

// ─── GET /contactos/{id}  (leer uno) ───────────────────────────
Console.WriteLine("\n== GET /contactos/{id} ==");
Mostrar("GET", 200, service.GetById(1));   // 1ª vez: no está en caché → consulta BD y la rellena
Mostrar("GET", 200, service.GetById(1));   // 2ª vez: sale de la CACHÉ (mismo resultado, sin tocar BD)
Mostrar("GET", 200, service.GetById(999)); // no existe → 404 NotFound

// ─── GET /contactos/alias/{alias} ──────────────────────────────
Console.WriteLine("\n== GET /contactos/alias/{alias} ==");
Mostrar("GET", 200, service.GetByAlias("lucho"));    // existe → 200
Mostrar("GET", 200, service.GetByAlias("noexiste")); // 404

// ─── GET /contactos  (listar) ──────────────────────────────────
Console.WriteLine("\n== GET /contactos (sin borrados) ==");
foreach (var c in service.GetAll(includeDeleted: false))
    Console.WriteLine($"  {c.Id} | {c.Nombre} | {c.Alias} | {c.Telefono}");
Console.WriteLine($"  (TotalContacto: {service.TotalContacto})");

// ─── PUT /contactos/{id}  (actualizar) ─────────────────────────
Console.WriteLine("\n== PUT /contactos/{id} ==");
Mostrar("PUT", 200, service.Update(2, new Contacto {
    Nombre = "Luis Pérez Vega", Telefono = "600111333", Email = "luisv@mail.com", Alias = "luigui" }));

// Caso negativo: el contacto 3 toma el teléfono del 1 → 409
Mostrar("PUT", 200, service.Update(3, new Contacto {
    Nombre = "Marta Ruiz", Telefono = "600111222", Email = "marta@mail.com", Alias = "marti" }));

// ─── DELETE /contactos/{id}  (borrar) ──────────────────────────
Console.WriteLine("\n== DELETE /contactos/{id} ==");
Mostrar("DELETE", 200, service.Delete(3));           // borrado lógico (IsDeleted = true)
Mostrar("DELETE", 200, service.Delete(999));         // no existe → 404

// Después del borrado lógico, el 3 ya no sale en la lista normal
Console.WriteLine("\n== GET /contactos tras borrar el 3 ==");
foreach (var c in service.GetAll(includeDeleted: false))
    Console.WriteLine($"  {c.Id} | {c.Nombre} | {c.Alias}");
    