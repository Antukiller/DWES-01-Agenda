using System;
using System.Text.Json;
using _01_Agenda.Controller;
using _01_Agenda.Dto;
using _01_Agenda.Infrastructure;
using _01_Agenda.Models;
using _01_Agenda.Models.Enum;
using Microsoft.Extensions.DependencyInjection;

// ─── CONFIGURACIÓN VÍA DEPENDENCIESPROVIDER ────────────────────
var serviceProvider = DependenciesProvider.BuildServiceProvider();

// Resolvemos el DbContext para asegurar la creación de la BD
using (var scope = serviceProvider.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.EnsureCreated();
}

// Resolvemos el controlador (DI inyecta automáticamente el servicio, repo y caché)
var contactos = serviceProvider.GetRequiredService<ContactoController>();

// ─── HELPERS ───────────────────────────────────────────────────
void Mostrar(HttpVerb verbo, ResponseDto resp) {
    Console.WriteLine($"[{Verbo(verbo)} {(int)resp.Codigo} {resp.Codigo}] {resp.Contenido}");
}

void Dispatch(HttpVerb verbo, string cuerpo) {
    Mostrar(verbo, contactos.Dispatch(new ResquestDto(verbo, cuerpo)));
}

string Verbo(HttpVerb v) => v switch {
    HttpVerb.Post   => "POST",
    HttpVerb.Get    => "GET",
    HttpVerb.Put    => "PUT",
    HttpVerb.Delete => "DELETE",
    _               => "?"
};

// ─── POST /contactos  (crear) ──────────────────────────────────
Console.WriteLine("\n== POST /contactos ==");
Dispatch(HttpVerb.Post, JsonSerializer.Serialize(new Contacto {
    Nombre = "Ana García", Telefono = "600111222", Email = "ana@mail.com", Alias = "anni" }));
Dispatch(HttpVerb.Post, JsonSerializer.Serialize(new Contacto {
    Nombre = "Luis Pérez", Telefono = "600111333", Email = "luis@mail.com", Alias = "lucho" }));
Dispatch(HttpVerb.Post, JsonSerializer.Serialize(new Contacto {
    Nombre = "Marta Ruiz", Telefono = "600111444", Email = "marta@mail.com", Alias = "marti" }));

Console.WriteLine("\n== POST /contactos (duplicado) ==");
Dispatch(HttpVerb.Post, JsonSerializer.Serialize(new Contacto {
    Nombre = "Clon", Telefono = "600111222", Email = "clon@mail.com", Alias = "clon" })); // 409

// ─── GET /contactos/{id}  (leer uno) ───────────────────────────
Console.WriteLine("\n== GET /contactos/{id} ==");
Dispatch(HttpVerb.Get, JsonSerializer.Serialize(1));          // existe → 200
Dispatch(HttpVerb.Get, JsonSerializer.Serialize(1));          // caché → 200
Dispatch(HttpVerb.Get, JsonSerializer.Serialize(999));        // no existe → 404

// ─── GET /contactos (Probando las distintas variantes) ───────────────
Console.WriteLine("\n== 1. GET /contactos (Obtener TODOS) ==");
Dispatch(HttpVerb.Get, "");                       // Cadena vacía → Devuelve la lista completa (200)
// Dispatch(HttpVerb.Get, "{}");                  // O con "{}" → También devuelve todos (200)

Console.WriteLine("\n== 2. GET /contactos/{id} (Obtener por ID) ==");
Dispatch(HttpVerb.Get, JsonSerializer.Serialize(1)); // Int 1 → Devuelve el contacto 1 (200)
Dispatch(HttpVerb.Get, JsonSerializer.Serialize(999)); // Int 999 → Devuelve NotFound (404)

Console.WriteLine("\n== 3. GET /contactos/alias/{alias} (Obtener por ALIAS) ==");
Dispatch(HttpVerb.Get, JsonSerializer.Serialize("anni")); // Cadena "anni" → Devuelve a Ana García (200)
Dispatch(HttpVerb.Get, JsonSerializer.Serialize("no_existe")); // Alias inexistente → Devuelve NotFound (404)

// ─── PUT /contactos/{id}  (actualizar) ─────────────────────────
Console.WriteLine("\n== PUT /contactos/{id} ==");
Dispatch(HttpVerb.Put, JsonSerializer.Serialize(new Contacto {
    Id = 2, Nombre = "Luis Pérez Vega", Telefono = "600111333", Email = "luisv@mail.com", Alias = "luigui" }));    // 200
Dispatch(HttpVerb.Put, JsonSerializer.Serialize(new Contacto {
    Id = 3, Nombre = "Marta Ruiz", Telefono = "600111222", Email = "marta@mail.com", Alias = "marti" }));           // 409 (teléfono del 1)
Dispatch(HttpVerb.Put, JsonSerializer.Serialize(new Contacto {
    Id = 999, Nombre = "X", Telefono = "699999999", Email = "x@mail.com", Alias = "x" }));                          // 404

// ─── DELETE /contactos/{id}  (borrar) ──────────────────────────
Console.WriteLine("\n== DELETE /contactos/{id} ==");
Dispatch(HttpVerb.Delete, JsonSerializer.Serialize(3));        // borrado lógico → 200
Dispatch(HttpVerb.Delete, JsonSerializer.Serialize(999));      // no existe → 404

// ─── Verbo desconocido → 400 ───────────────────────────────────
Console.WriteLine("\n== PATCH (verbo no soportado) ==");
Mostrar(HttpVerb.Get, contactos.Dispatch(new ResquestDto((HttpVerb)99, "{}"))); // → 400 BadRequest