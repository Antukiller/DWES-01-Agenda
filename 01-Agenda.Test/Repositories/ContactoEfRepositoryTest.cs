using _01_Agenda.Error.Common;
using _01_Agenda.Error.Contacto;
using _01_Agenda.Models;
using _01_Agenda.Models.Enum;
using _01_Agenda.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace _01_Agenda.Test.Repositories;

/// <summary>
/// Tests para ContactoEfRepository.
/// Configuración de tests: SQLite en memoria (:memory:) para cada clase de test.
/// La conexión se mantiene abierta durante toda la clase para compartir la BD en memoria.
///
/// Esta configuración es ideal para tests porque:
/// 1. SQLite real en memoria (más realista que InMemory)
/// 2. Soporta foreign keys, transacciones, SQL real
/// 3. Cada clase de test tiene su propia BD independiente
/// 4. Los IDs siempre empiezan desde 1 en cada test
/// </summary>
[TestFixture]
public class ContactoEfRepositoryTests {

    /// <summary>
    /// Factoría auxiliar: crea un Contacto con datos válidos por defecto.
    /// </summary>
    private static Contacto NuevoContacto(string nombre = "Ana García", string telefono = "600111222",
        string email = "ana@mail.com", string alias = "anni")
        => new() { Nombre = nombre, Telefono = telefono, Email = email, Alias = alias };

    // ─── CASOS POSITIVOS ─────────────────────────────────────────────

    /// <summary>
    /// Casos positivos de ContactoEfRepository.
    /// </summary>
    [TestFixture]
    public class CasosPositivos {
        private SqliteConnection _connection = null!;
        private AppDbContext _context = null!;
        private ContactoEfRepository _repository = null!;

        [SetUp]
        public void SetUp() {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _repository = new ContactoEfRepository(_context);
        }

        [TearDown]
        public void TearDown() {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// Crear un contacto válido debería funcionar y asignar el ID 1.
        /// </summary>
        [Test]
        public void Create_ContactoValido_CrearCorrectamente() {
            // Act
            var r = _repository.Create(NuevoContacto());

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Id.Should().Be(1); // BD limpia → el primer ID es 1
            r.Value.IsDeleted.Should().BeFalse();
            r.Value.CreatedAt.Should().NotBe(default);
        }

        /// <summary>
        /// GetById debería devolver el contacto cuando existe.
        /// </summary>
        [Test]
        public void GetById_CuandoExiste_RetornarContacto() {
            // Arrange
            _repository.Create(NuevoContacto());

            // Act
            var r = _repository.GetById(1);

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value!.Id.Should().Be(1);
        }

        /// <summary>
        /// GetByAlias debería devolver el contacto cuando el alias existe.
        /// </summary>
        [Test]
        public void GetByAlias_CuandoExiste_RetornarContacto() {
            // Arrange
            _repository.Create(NuevoContacto());

            // Act
            var r = _repository.GetByAlias("anni");

            // Assert
            r.Should().NotBeNull();
            r!.Alias.Should().Be("anni");
        }

        /// <summary>
        /// ExistsAlias debería devolver true cuando el alias existe.
        /// </summary>
        [Test]
        public void ExistsAlias_CuandoExiste_RetornarTrue() {
            // Arrange
            _repository.Create(NuevoContacto());

            // Act
            var r = _repository.ExistsAlias("anni");

            // Assert
            r.Should().BeTrue();
        }

        /// <summary>
        /// ExistsTelefono debería devolver true cuando el teléfono existe.
        /// </summary>
        [Test]
        public void ExistsTelefono_CuandoExiste_RetornarTrue() {
            // Arrange
            _repository.Create(NuevoContacto());

            // Act
            var r = _repository.ExistsTelefono("600111222");

            // Assert
            r.Should().BeTrue();
        }

        /// <summary>
        /// GetAll sin filtros debería devolver todos los contactos.
        /// </summary>
        [Test]
        public void GetAll_SinParametros_RetornarTodos() {
            // Arrange
            _repository.Create(NuevoContacto(telefono: "600111222", alias: "anni"));
            _repository.Create(NuevoContacto(nombre: "Luis Pérez", telefono: "600111333", email: "luis@mail.com", alias: "lucho"));

            // Act
            var r = _repository.GetAll(1, 10, true);

            // Assert
            r.Should().HaveCount(2);
        }

        /// <summary>
        /// GetAll con paginación debería devolver la página correcta.
        /// </summary>
        [Test]
        public void GetAll_ConPaginacion_RetornarPagina() {
            // Arrange: creamos 5 contactos
            for (var i = 1; i <= 5; i++)
                _repository.Create(NuevoContacto(
                    nombre: $"Contacto{i}",
                    telefono: $"600111{i:D3}",
                    email: $"c{i}@mail.com",
                    alias: $"alias{i}"));

            // Act: página 2 con tamaño 2 → contactos 3 y 4
            var r = _repository.GetAll(2, 2, true);

            // Assert
            r.Should().HaveCount(2);
            r.First().Id.Should().Be(3);
            r.Last().Id.Should().Be(4);
        }

        /// <summary>
        /// GetAll sin incluir borrados debería devolver solo los activos.
        /// </summary>
        [Test]
        public void GetAll_ExcluyendoBorrados_RetornarSoloActivos() {
            // Arrange
            _repository.Create(NuevoContacto(telefono: "600111222", alias: "anni"));
            var v2 = _repository.Create(NuevoContacto(telefono: "600111333", alias: "lucho")).Value;
            _repository.Delete(v2.Id);

            // Act
            var r = _repository.GetAll(1, 10, false);

            // Assert
            r.Should().HaveCount(1);
            r.First().Alias.Should().Be("anni");
        }

        /// <summary>
        /// Update con datos válidos debería actualizar y devolver la entidad.
        /// </summary>
        [Test]
        public void Update_ConDatosValidos_RetornarActualizacion() {
            // Arrange
            var creado = _repository.Create(NuevoContacto()).Value;

            // Act
            var r = _repository.Update(creado.Id, NuevoContacto(
                nombre: "Ana García Vega", telefono: "600111999",
                email: "anav@mail.com", alias: "anniv"));

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Nombre.Should().Be("Ana García Vega");
            r.Value.Alias.Should().Be("anniv");
            r.Value.UpdatedAt.Should().NotBe(default);
        }

        /// <summary>
        /// Update sin cambiar el teléfono no debería colisionar consigo mismo.
        /// </summary>
        [Test]
        public void Update_SinCambiarTelefono_NoColisionarConsigoMismo() {
            // Arrange
            var creado = _repository.Create(NuevoContacto()).Value;

            // Act: cambio el nombre pero mantengo el mismo teléfono
            var r = _repository.Update(creado.Id, NuevoContacto(
                nombre: "Ana García Actualizada", telefono: "600111222", alias: "anni"));

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Nombre.Should().Be("Ana García Actualizada");
        }

        /// <summary>
        /// Delete lógico debería marcar IsDeleted como true.
        /// </summary>
        [Test]
        public void Delete_Logico_CuandoExiste_MarcarIsDeleted() {
            // Arrange
            _repository.Create(NuevoContacto());

            // Act
            var r = _repository.Delete(1);

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.IsDeleted.Should().BeTrue();
        }
    }

    // ─── CASOS NEGATIVOS ─────────────────────────────────────────────

    /// <summary>
    /// Casos negativos de ContactoEfRepository.
    /// </summary>
    [TestFixture]
    public class CasosNegativos {
        private SqliteConnection _connection = null!;
        private AppDbContext _context = null!;
        private ContactoEfRepository _repository = null!;

        [SetUp]
        public void SetUp() {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _repository = new ContactoEfRepository(_context);
        }

        [TearDown]
        public void TearDown() {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// Create con teléfono existente debería devolver failure.
        /// </summary>
        [Test]
        public void Create_ConTelefonoExistente_RetornarFailure() {
            // Arrange
            _repository.Create(NuevoContacto());

            // Act
            var r = _repository.Create(NuevoContacto(nombre: "Clon", alias: "clon"));

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.TelefonoAlreadyExists>();
            (r.Error as ContactoError.TelefonoAlreadyExists)?.Telefono.Should().Be("600111222");
            r.Error.Message.Should().Contain("600111222");
        }

        /// <summary>
        /// GetById para un id inexistente debería devolver null.
        /// </summary>
        [Test]
        public void GetById_CuandoNoExiste_RetornarNull() {
            // Act
            var r = _repository.GetById(999);

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.NotFound>();
        }

        /// <summary>
        /// GetByAlias para un alias inexistente debería devolver null.
        /// </summary>
        [Test]
        public void GetByAlias_CuandoNoExiste_RetornarNull() {
            // Act
            var r = _repository.GetByAlias("noexiste");

            // Assert
            r.Should().BeNull();
        }

        /// <summary>
        /// ExistsAlias para un alias inexistente debería devolver false.
        /// </summary>
        [Test]
        public void ExistsAlias_CuandoNoExiste_RetornarFalse() {
            // Act
            var r = _repository.ExistsAlias("noexiste");

            // Assert
            r.Should().BeFalse();
        }

        /// <summary>
        /// ExistsTelefono para un teléfono inexistente debería devolver false.
        /// </summary>
        [Test]
        public void ExistsTelefono_CuandoNoExiste_RetornarFalse() {
            // Act
            var r = _repository.ExistsTelefono("600999999");

            // Assert
            r.Should().BeFalse();
        }

        /// <summary>
        /// Update para un id inexistente debería devolver failure (NotFound).
        /// </summary>
        [Test]
        public void Update_CuandoNoExiste_RetornarFailure() {
            // Act
            var r = _repository.Update(9999, NuevoContacto());

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.NotFound>();
            (r.Error as ContactoError.NotFound)?.Id.Should().Be("9999");
            r.Error.Message.Should().Contain("9999");
        }

        /// <summary>
        /// Update con un teléfono usado por otro contacto debería fallar.
        /// </summary>
        [Test]
        public void Update_ConTelefonoDeOtroContacto_RetornarFailure() {
            // Arrange
            _repository.Create(NuevoContacto(telefono: "600111222", alias: "anni"));
            var v2 = _repository.Create(NuevoContacto(telefono: "600111333", alias: "lucho")).Value;

            // Act: el contacto 2 intenta coger el teléfono del contacto 1
            var r = _repository.Update(v2.Id, NuevoContacto(telefono: "600111222", alias: "lucho"));

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.TelefonoAlreadyExists>();
        }

        /// <summary>
        /// Delete para un id inexistente debería devolver failure (NotFound).
        /// </summary>
        [Test]
        public void Delete_CuandoNoExiste_RetornarFailure() {
            // Act
            var r = _repository.Delete(999);

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.NotFound>();
            r.Error.Message.Should().Contain("999");
        }
    }

    // ─── CASOS MIXTOS ────────────────────────────────────────────────

    /// <summary>
    /// Casos mixtos de ContactoEfRepository.
    /// </summary>
    [TestFixture]
    public class CasosMixtos {
        private SqliteConnection _connection = null!;
        private AppDbContext _context = null!;
        private ContactoEfRepository _repository = null!;

        [SetUp]
        public void SetUp() {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _repository = new ContactoEfRepository(_context);
        }

        [TearDown]
        public void TearDown() {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// Un contacto borrado lógicamente no debería aparecer en la lista sin borrados.
        /// </summary>
        [Test]
        public void Delete_Logico_NoApareceEnListaSinBorrados() {
            // Arrange
            _repository.Create(NuevoContacto(telefono: "600111222", alias: "anni"));
            var v2 = _repository.Create(NuevoContacto(telefono: "600111333", alias: "lucho")).Value;
            _repository.Delete(v2.Id);

            // Act
            var activos = _repository.GetAll(1, 10, false);
            var todos = _repository.GetAll(1, 10, true);

            // Assert
            activos.Should().HaveCount(1);
            activos.Should().NotContain(c => c.Id == v2.Id);
            todos.Should().HaveCount(2); // en la lista "todos" sigue estando
        }

        /// <summary>
        /// Tras un delete físico, GetById debería devolver null.
        /// </summary>
        [Test]
        public void Delete_Fisico_GetByIdDevuelveNull() {
            // Arrange
            var creado = _repository.Create(NuevoContacto()).Value;

            // Act
            _repository.Delete(creado.Id, isLogical: false);

            // Assert
            _repository.GetById(creado.Id).IsFailure.Should().BeTrue();
        }

        /// <summary>
        /// El teléfono de un contacto borrado lógicamente se puede reutilizar
        /// (la regla de negocio ignora a los borrados).
        /// </summary>
        [Test]
        public void Create_TelefonoDeBorradoLogico_SePuedeReutilizar() {
            // Arrange
            var v1 = _repository.Create(NuevoContacto()).Value;
            _repository.Delete(v1.Id, isLogical: true);

            // Act: nuevo contacto con el mismo teléfono
            var r = _repository.Create(NuevoContacto(nombre: "Nuevo Dueño", alias: "nuevo"));

            // Assert
            r.IsSuccess.Should().BeTrue();
        }
    }

    // ─── CASOS DE EXCEPCIÓN DE BASE DE DATOS ─────────────────────────

    /// <summary>
    /// Casos de excepción de base de datos: cubren todos los bloques catch del repositorio.
    /// Se fuerza una excepción DESECHANDO (Dispose) el contexto antes de operar:
    /// cualquier consulta contra un contexto dispuesto lanza ObjectDisposedException.
    /// Cada método del repositorio captura esa excepción y la convierte en:
    ///   - null (GetById, GetByAlias)
    ///   - false (ExistsAlias, ExistsTelefono)
    ///   - Result.Failure con ContactoError.Database (Create, Update, Delete)
    /// </summary>
    [TestFixture]
    public class CasosExcepcionDb {
        private SqliteConnection _connection = null!;
        private AppDbContext _context = null!;
        private ContactoEfRepository _repository = null!;

        [SetUp]
        public void SetUp() {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _repository = new ContactoEfRepository(_context);
        }

        [TearDown]
        public void TearDown() {
            // El contexto puede estar desechado por el propio test; EnsureDeleted no debe fallar.
            try { _context.Database.EnsureDeleted(); } catch { /* ya desechado */ }
            _context.Dispose();
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// Create con fallo de BD debería devolver ContactoError.Database (500).
        /// </summary>
        [Test]
        public void Create_CuandoFallaLaBD_RetornarErrorDatabase() {
            // Arrange: contexto desechado → cualquier operación lanza excepción
            _context.Dispose();

            // Act
            var r = _repository.Create(NuevoContacto());

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.Database>();
            r.Error.Code.Should().Be(HttpCodes.InternalServerError);
        }

        /// <summary>
        /// Update con fallo de BD debería devolver ContactoError.Database (500).
        /// </summary>
        [Test]
        public void Update_CuandoFallaLaBD_RetornarErrorDatabase() {
            // Arrange
            _context.Dispose();

            // Act
            var r = _repository.Update(1, NuevoContacto());

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.Database>();
            r.Error.Code.Should().Be(HttpCodes.InternalServerError);
        }

        /// <summary>
        /// Delete con fallo de BD debería devolver ContactoError.Database (500).
        /// </summary>
        [Test]
        public void Delete_CuandoFallaLaBD_RetornarErrorDatabase() {
            // Arrange
            _context.Dispose();

            // Act
            var r = _repository.Delete(1);

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.Database>();
            r.Error.Code.Should().Be(HttpCodes.InternalServerError);
        }

        /// <summary>
        /// GetById con fallo de BD debería devolver null (catch → null).
        /// </summary>
        [Test]
        public void GetById_CuandoFallaLaBD_RetornarNull() {
            // Arrange
            _context.Dispose();

            // Act
            var r = _repository.GetById(1);

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.Database>();
        }

        /// <summary>
        /// GetByAlias con fallo de BD debería devolver null (catch → null).
        /// </summary>
        [Test]
        public void GetByAlias_CuandoFallaLaBD_RetornarNull() {
            // Arrange
            _context.Dispose();

            // Act
            var r = _repository.GetByAlias("anni");

            // Assert
            r.Should().BeNull();
        }

        /// <summary>
        /// ExistsAlias con fallo de BD debería devolver false (catch → false).
        /// </summary>
        [Test]
        public void ExistsAlias_CuandoFallaLaBD_RetornarFalse() {
            // Arrange
            _context.Dispose();

            // Act
            var r = _repository.ExistsAlias("anni");

            // Assert
            r.Should().BeFalse();
        }

        /// <summary>
        /// ExistsTelefono con fallo de BD debería devolver false (catch → false).
        /// </summary>
        [Test]
        public void ExistsTelefono_CuandoFallaLaBD_RetornarFalse() {
            // Arrange
            _context.Dispose();

            // Act
            var r = _repository.ExistsTelefono("600111222");

            // Assert
            r.Should().BeFalse();
        }
    }
}