# 📇 DWES-01 · Agenda de Contactos (.NET 10)

Mini-API REST **simulada en consola**: una agenda de contactos montada sobre
**EF Core + SQLite**, con **repositorio genérico**, **caché LRU** y
**Result + DomainError** (patrón *Railway Oriented Programming*), todo desde un
`Program.cs` que se comporta como una pequeña capa de **rutas HTTP**.

> El objetivo del ejercicio es ver **cómo se monta un sistema por capas** y
> **dónde vive cada responsabilidad**, sin frameworks web: las "rutas" las
> pinta el `Program.cs` y el servicio "devuelve" códigos HTTP a través de enums.

---

## 🧱 Arquitectura (por capas)

```
petición HTTP  ──>  PROGRAM.CAP (rutas)  ──>  SERVICES  ──>  REPOSITORY  ──>  EF Core  ──>  SQLite
                           │                    │                │
                           └── HttpVerb         └── Result       └── ICrudRepository
                               HttpCodes            +DomainError     (reglas: 409/404)
```

| Capa | Carpeta | Responsabilidad |
|------|---------|-----------------|
| **Rutas / demo** | `Program.cs` | "Capa de rutas": simula verbos y códigos HTTP, pinta logs |
| **Servicio** | `Services/` | Orquesta repo + caché, orquesta respuestas `Result` |
| **Repositorio** | `Repositories/` | Acceso a datos, **reglas de negocio** (duplicados → 409) |
| **Caché** | `Cache/` | Caché **LRU** genérica para `GetById` |
| **Modelo** | `Models/` | Entidad `Contacto` + `AppDbContext` (SQLite/EF Core) |
| **Errores** | `Error/` | `DomainError` + errores de dominio de `Contacto` |
| **Enums** | `Enum/` | `HttpVerb` (verbos) y `HttpCodes` (códigos HTTP) |

**Regla del ejercicio:** *las reglas de negocio van en el repositorio; el
`Program.cs` solo "dirige" peticiones.* Por eso en `Program.cs` hay un **switch**
para decir el verbo y para mostrar el código — añadir algo nuevo = añadir un
`case`, es decir, **así se cambia lo que sea, rápido.**

---

## 🗂️ Estructura de archivos

```
DWES-01-Agenda/
├─ 01-Agenda/                         # proyecto principal (consola)
│  ├─ Program.cs                      # capa de RUTAS: demo completa de CRUD + logs
│  ├─ Cache/
│  │  ├─ ICache.cs                    # contrato genérico de caché
│  │  └─ LruCache.cs                  # implementación LRU (custom, sin NuGet)
│  ├─ Enum/
│  │  ├─ HttpVerb.cs                  # Get / Post / Put / Delete
│  │  └─ HttpCodes.cs                 # Ok(200) Created(201) BadRequest(400)...
│  ├─ Error/
│  │  ├─ Common/DomainError.cs        # error base del dominio
│  │  └─ Contacto/
│  │     ├─ ContactoError.cs          # NotFound(404) · Alias/Telefono duplicado(409) · Database(500)
│  │     └─ ContactoErrors.cs         # factories (ContactoErrors.NotFound(...), ...)
│  ├─ Models/
│  │  ├─ Contacto.cs                  # entidad (Id, Nombre, Telefono, Email, Alias, IsDeleted)
│  │  └─ AppDbContext.cs              # contexto EF Core (SQLite)
│  ├─ Repositories/
│  │  ├─ ICrudRepository.cs           # contrato CRUD
│  │  └─ ContactoEfRepository.cs      # impl EF Core con reglas (409 Conflict)
│  ├─ Services/
│  │  ├─ IAgendaservices.cs           # contrato del servicio
│  │  └─ AgendaServices.cs            # monta repo + caché; devuelve Result
│  └─ (se genera agenda.db al ejecutar)
│
└─ 01-Agenda.Test/                    # proyectos de pruebas NUnit + EF InMemory
   ├─ Services/AgendaServicesTest.cs
   ├─ Repositories/ContactoEfRepositoryTest.cs
   ├─ Cache/LruCacheTest.cs
   └─ UnitTest1.cs
```

---

## 🚀 Cómo ejecutarlo

Requisitos: **.NET 10 SDK**.

```bash
# 1. Ejecutar la app (programa de consola / "rutas")
dotnet run

# 2. Ejecutar todos los tests (NUnit)
dotnet test
```

Al lanzarla se crea `agenda.db` (SQLite) y se muestra un "log de servidor" con
los verbos y códigos de cada llamada:

```
== POST /contactos =====
[POST 201 Created] Ana García | anni | 600111222
[PUT 400 BadRequest] falta el {id} en la ruta
[GET 200 Ok] Luis Pérez | lucho | 600111333
...
```

---

## 🔑 Lo que se demuestra

### 1. Caché LRU (por qué "la 2ª vez toca menos la BD")
`LruCache<int, Contacto>` con capacidad fija:

- 1ª `GetById(1)` → **no está en caché** → consulta BD y la rellena.
- 2ª `GetById(1)` → **sale de la CACHÉ** (resultado idéntico, sin tocar BD).
- Si la caché se llena → se expulsa el elemento **menos usado** (LRU).

`Program.cs` lo verifica llamando dos veces al mismo id y mostrando el mismo
`GET 200` (pero la 2ª viene de caché, no de la BD).

### 2. Result + DomainError (patrón "servidor honesto")
Todo método del servicio devuelve `Result<Contacto, DomainError>` en vez de
lanzar excepciones. El error "sabe" su código HTTP:

```csharp
Result<Contacto, DomainError> GetById(int id);
```

- `GetById(999)` → `Fail(NotFound)` → **404**
- `Save(teléfono duplicado)` → **409 Conflict** (regla del repositorio)
- Error de BD → **500 InternalServerError**

### 3. Enums HttpVerb y HttpCodes (capa de rutas)
En `Program.cs` se usan los enums — **no** strings mágicos ni números sueltos:

```csharp
string Verbo(HttpVerb v) => v switch {
    HttpVerb.Post   => "POST",
    HttpVerb.Get    => "GET",
    HttpVerb.Put    => "PUT",
    HttpVerb.Delete => "DELETE",
    _               => "?"   // verbo nuevo sin case = saludar con "?"
};

void Mostrar(HttpVerb verbo, HttpCodes codeOk, Result<Contacto, DomainError> r) {
    if (r.IsSuccess) Console.WriteLine($"[{Verbo(verbo)} {(int)codeOk} {codeOk}] ...");
    else            Console.WriteLine($"[{Verbo(verbo)} {(int)r.Error.Code} {r.Error.Code}] {r.Error.Message}");
}
```

**"Así se cambia":** quieres un verbo nuevo → añades un `case`; un código nuevo → un
valor más en `HttpCodes`. El switch se convierte en el único sitio a tocar.

### 4. Códigos HTTP usados en la demo de Program.cs
| Código | Enum | Cuándo sale en `Program.cs` |
|--------|------|------------------------------|
| `200 Ok` | `HttpCodes.Ok` | GET / PUT / DELETE con éxito |
| `201 Created` | `HttpCodes.Created` | POST al crear un contacto |
| `400 BadRequest` | `HttpCodes.BadRequest` | Petición mal formada (falta `{id}` en la ruta) |
| `404 NotFound` | `HttpCodes.NotFound` | El contacto (o el id) no existe |
| `409 Conflict` | `HttpCodes.Conflict` | Teléfono/alias duplicado (regla del repositorio) |
| `500 ServerError` | `HttpCodes.InternalServerError` | Error de BD |

---

## ✅ Cómo se testea

Usa **NUnit** (59 tests) sobre **repo/servicio/caché** con **EF InMemory**
(no toca el SQLite de la app):

```bash
cd 01-Agenda.Test
dotnet test
```

Cubren:
- `LruCache`: inserción, acierto/fallo, desalojo (evict) del menos usado.
- `ContactoEfRepository`: guardar, duplicados (teléfono/alias) → 409, lectura, update, delete lógico.
- `AgendaServices`: integración servicio + caché + repo.

Resultado esperado:

```
Superado: 59, Omitido: 0, Con error: 0   ✓ 59/59
```

---

## 🧠 Ideas para seguir (si te piden "cambiar" algo)

- **Añadir un verbo** → añadir `case` en el switch de `Verbo(...)`.
- **Añadir un código HTTP** → añadirlo a `HttpCodes` (déjale su `=` número).
- **Nueva regla de negocio** → en `ContactoEfRepository.Save/Update` y devolver un
  nuevo `ContactoError` (p. ej. `UnsupportedOperationException`... mejor: un
  `DomainError` propio, p. ej. `Validation` para un **400**).
- **Validar datos vacíos (400 real)** → donde toca, en el repositorio/servicio:
  si `Nombre` o `Telefono` vienen en blanco → devolver
  `ContactoErrors.Validation("falta el Nombre")` (400), no inventarlo en Program.
- **CrudRepository genérico** → ya está como `ICrudRepository`; puedes hacer
  `ContactoEfRepository` heredar de un `CrudRepositoryBase<Contacto>`.

---

Hecho con ☕ y .NET · Ejercicio de **Desarrollo Web en Entorno Servidor (DWES)** — 2º DAW.
