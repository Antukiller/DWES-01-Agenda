using _01_Agenda.Cache;
using _01_Agenda.Error.Common;
using _01_Agenda.Error.Contacto;
using _01_Agenda.Models;
using _01_Agenda.Repositories.Base;
using _01_Agenda.Services;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;

namespace _01_Agenda.Test.Services;

/// <summary>
/// Tests for AgendaServices. Uses mocked repository and cache.
/// Organized into CasosPositivos (positive cases), CasosNegativos (negative cases)
/// and CasosMixtos (mixed cases: cache invalidation between several operations).
/// El servicio NO contiene reglas de negocio (están en el repositorio);
/// por eso aquí solo se prueban: delegación al repo, lógica de caché y errores.
/// </summary>
[TestFixture]
public class AgendaServicesTest {

    // Mock del repositorio: simula la BD sin tocar SQLite.
    private Mock<ICrudRepository> _repositoryMock = null!;

    // Mock de la caché: simula el comportamiento de la LruCache sin probarla (eso ya está en LruCacheTest).
    private Mock<ICache<int, Contacto>> _cacheMock = null!;

    private AgendaServices _service = null!;

    [SetUp]
    public void SetUp() {
        _repositoryMock = new Mock<ICrudRepository>();
        _cacheMock = new Mock<ICache<int, Contacto>>();

        _service = new AgendaServices(
            _repositoryMock.Object,
            _cacheMock.Object
        );
    }

    /// <summary>
    /// Factoría auxiliar: crea un Contacto válido por defecto.
    /// </summary>
    private static Contacto NuevoContacto(int id = 1, string nombre = "Ana García",
        string telefono = "600111222", string alias = "anni")
        => new() { Id = id, Nombre = nombre, Telefono = telefono, Email = $"c{id}@mail.com", Alias = alias };

    // ─── CASOS POSITIVOS ─────────────────────────────────────────────

    /// <summary>
    /// Casos positivos de AgendaServices.
    /// </summary>
    [TestFixture]
    public class CasosPositivos : AgendaServicesTest {

        /// <summary>
        /// GetAll sin parámetros debería delegar al repositorio con los valores por defecto.
        /// </summary>
        [Test]
        public void GetAll_SinParametros_RetornarTodosLosContactos() {
            // Arrange
            var lista = new List<Contacto> { NuevoContacto(1), NuevoContacto(2, alias: "lucho") };
            _repositoryMock.Setup(r => r.GetAll(1, 10, true)).Returns(lista);

            // Act
            var r = _service.GetAll();

            // Assert
            r.Should().HaveCount(2);
            _repositoryMock.Verify(r => r.GetAll(1, 10, true), Times.Once);
        }

        /// <summary>
        /// GetAll con parámetros debería pasarlos tal cual al repositorio.
        /// </summary>
        [Test]
        public void GetAll_ConParametros_DelegarEnRepositorio() {
            // Arrange
            _repositoryMock.Setup(r => r.GetAll(2, 5, false)).Returns(new List<Contacto>());

            // Act
            var r = _service.GetAll(page: 2, pageSize: 5, includeDeleted: false);

            // Assert
            r.Should().BeEmpty();
            _repositoryMock.Verify(r => r.GetAll(2, 5, false), Times.Once);
        }

        /// <summary>
        /// TotalContacto debería contar TODOS los contactos (usa página gigante con todo incluido).
        /// </summary>
        [Test]
        public void TotalContacto_RetornarTotal() {
            // Arrange
            _repositoryMock.Setup(r => r.GetAll(1, int.MaxValue, true))
                .Returns(new List<Contacto> { NuevoContacto(1), NuevoContacto(2), NuevoContacto(3) });

            // Act
            var r = _service.TotalContacto;

            // Assert
            r.Should().Be(3);
            _repositoryMock.Verify(r => r.GetAll(1, int.MaxValue, true), Times.Once);
        }

        /// <summary>
        /// GetById con dato en caché debería devolverlo SIN consultar la BD.
        /// </summary>
        [Test]
        public void GetById_ConCache_RetornarDeCache() {
            // Arrange
            var c = NuevoContacto(1);
            _cacheMock.Setup(c => c.Get(1)).Returns(c);

            // Act
            var r = _service.GetById(1);

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Nombre.Should().Be("Ana García");
            _cacheMock.Verify(c => c.Get(1), Times.Once);
            _repositoryMock.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// GetById sin caché debería leer del repositorio y GUARDAR en caché.
        /// </summary>
        [Test]
        public void GetById_SinCache_BuscarEnRepositorioYAgregarACache() {
            // Arrange
            var c = NuevoContacto(1);
            _cacheMock.Setup(c => c.Get(1)).Returns((Contacto?)null);
            _repositoryMock.Setup(r => r.GetById(1)).Returns(c);

            // Act
            var r = _service.GetById(1);

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Alias.Should().Be("anni");
            _cacheMock.Verify(cache => cache.Get(1), Times.Once);
            _cacheMock.Verify(cache => cache.Add(1, c), Times.Once);
            _repositoryMock.Verify(r => r.GetById(1), Times.Once);
        }

        /// <summary>
        /// GetByAlias cuando existe debería devolver el contacto y guardarlo en caché por su Id.
        /// </summary>
        [Test]
        public void GetByAlias_CuandoExiste_RetornarContacto() {
            // Arrange
            var c = NuevoContacto(5, alias: "anni");
            _repositoryMock.Setup(r => r.GetByAlias("anni")).Returns(c);

            // Act
            var r = _service.GetByAlias("anni");

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Id.Should().Be(5);
            _repositoryMock.Verify(r => r.GetByAlias("anni"), Times.Once);
            _cacheMock.Verify(cache => cache.Add(5, c), Times.Once);
        }

        /// <summary>
        /// Save con contacto válido debería persistir y añadir a caché.
        /// </summary>
        [Test]
        public void Save_ConContactoValido_GuardarCorrectamente() {
            // Arrange
            var c = NuevoContacto(1);
            _repositoryMock.Setup(r => r.Create(It.IsAny<Contacto>()))
                .Returns((Contacto c) => Result.Success<Contacto, DomainError>(c));

            // Act
            var r = _service.Save(c);

            // Assert
            r.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(r => r.Create(c), Times.Once);
            _cacheMock.Verify(cache => cache.Add(c.Id, c), Times.Once);
        }

        /// <summary>
        /// Update sobre un contacto existente debería actualizar e INVALIDAR la caché.
        /// </summary>
        [Test]
        public void Update_ConContactoExistente_ActualizarYLimpiarCache() {
            // Arrange
            var actualizado = NuevoContacto(1, nombre: "Ana García Vega");
            _repositoryMock.Setup(r => r.Update(1, It.IsAny<Contacto>()))
                .Returns((int id, Contacto c) => Result.Success<Contacto, DomainError>(c));

            // Act
            var r = _service.Update(1, actualizado);

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Nombre.Should().Be("Ana García Vega");
            _repositoryMock.Verify(r => r.Update(1, actualizado), Times.Once);
            _cacheMock.Verify(c => c.Remove(1), Times.Once);
        }

        /// <summary>
        /// Delete sobre un contacto existente debería borrar (lógico por defecto) y limpiar caché.
        /// </summary>
        [Test]
        public void Delete_ConContactoExistente_EliminarYLimpiarCache() {
            // Arrange
            _repositoryMock.Setup(r => r.Delete(1, true))
                .Returns((int id, bool _) => Result.Success<Contacto, DomainError>(NuevoContacto(id)));

            // Act
            var r = _service.Delete(1);

            // Assert
            r.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(r => r.Delete(1, true), Times.Once);
            _cacheMock.Verify(c => c.Remove(1), Times.Once);
        }

        /// <summary>
        /// Delete físico debería pasar isLogical=false al repositorio.
        /// </summary>
        [Test]
        public void Delete_Fisico_DelegarConIsLogicalFalse() {
            // Arrange
            _repositoryMock.Setup(r => r.Delete(1, false))
                .Returns((int id, bool _) => Result.Success<Contacto, DomainError>(NuevoContacto(id)));

            // Act
            var r = _service.Delete(1, isLogical: false);

            // Assert
            r.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(r => r.Delete(1, false), Times.Once);
            _cacheMock.Verify(c => c.Remove(1), Times.Once);
        }
    }

    // ─── CASOS NEGATIVOS ─────────────────────────────────────────────

    /// <summary>
    /// Casos negativos de AgendaServices.
    /// </summary>
    [TestFixture]
    public class CasosNegativos : AgendaServicesTest {

        /// <summary>
        /// GetById de un contacto inexistente debería devolver NotFound (404) y NO tocar la caché.
        /// </summary>
        [Test]
        public void GetById_ConContactoNoExistente_RetornarErrorNotFound() {
            // Arrange
            _cacheMock.Setup(c => c.Get(999)).Returns((Contacto?)null);
    
            // Configurar el mock para que devuelva un Result.Failure con ContactoError.NotFound
            _repositoryMock
                .Setup(r => r.GetById(999))
                .Returns(Result.Failure<Contacto, DomainError>(new ContactoError.NotFound("999")));

            // Act
            var r = _service.GetById(999);

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.NotFound>();
            r.Error.Message.Should().Contain("999");
    
            _cacheMock.Verify(c => c.Get(999), Times.Once);
            _repositoryMock.Verify(r => r.GetById(999), Times.Once);
            _cacheMock.Verify(c => c.Add(It.IsAny<int>(), It.IsAny<Contacto>()), Times.Never);
        }
        
        /// <summary>
        /// GetByAlias de un alias inexistente debería devolver NotFound.
        /// </summary>
        [Test]
        public void GetByAlias_CuandoNoExiste_RetornarNotFound() {
            // Arrange
            _repositoryMock.Setup(r => r.GetByAlias("noexiste")).Returns((Contacto?)null);

            // Act
            var r = _service.GetByAlias("noexiste");

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.NotFound>();
            r.Error.Message.Should().Contain("noexiste");
            _cacheMock.Verify(c => c.Add(It.IsAny<int>(), It.IsAny<Contacto>()), Times.Never);
        }

        /// <summary>
        /// Save con teléfono duplicado debería propagar el error del repositorio (409) sin caché.
        /// </summary>
        [Test]
        public void Save_ConTelefonoDuplicado_RetornarErrorTelefonoAlreadyExists() {
            // Arrange
            var c = NuevoContacto(telefono: "600111222");
            _repositoryMock.Setup(r => r.Create(It.IsAny<Contacto>()))
                .Returns(Result.Failure<Contacto, DomainError>(ContactoErrors.TelefonoAlreadyExists("600111222")));

            // Act
            var r = _service.Save(c);

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.TelefonoAlreadyExists>();
            r.Error.Message.Should().Contain("600111222");
            _repositoryMock.Verify(r => r.Create(c), Times.Once);
            _cacheMock.Verify(c => c.Add(It.IsAny<int>(), It.IsAny<Contacto>()), Times.Never);
        }

        /// <summary>
        /// Save con alias duplicado debería propagar el error del repositorio (409).
        /// </summary>
        [Test]
        public void Save_ConAliasDuplicado_RetornarErrorAliasAlreadyExists() {
            // Arrange
            var c = NuevoContacto(alias: "anni");
            _repositoryMock.Setup(r => r.Create(It.IsAny<Contacto>()))
                .Returns(Result.Failure<Contacto, DomainError>(ContactoErrors.AliasAlreadyExists("anni")));

            // Act
            var r = _service.Save(c);

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.AliasAlreadyExists>();
            r.Error.Message.Should().Contain("anni");
            _cacheMock.Verify(c => c.Add(It.IsAny<int>(), It.IsAny<Contacto>()), Times.Never);
        }

        /// <summary>
        /// Update de un contacto inexistente debería devolver NotFound y NO limpiar la caché.
        /// </summary>
        [Test]
        public void Update_ConContactoNoExistente_RetornarErrorNotFound() {
            // Arrange
            _repositoryMock.Setup(r => r.Update(999, It.IsAny<Contacto>()))
                .Returns(Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound("999")));

            // Act
            var r = _service.Update(999, NuevoContacto(id: 999));

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.NotFound>();
            r.Error.Message.Should().Contain("999");
            _cacheMock.Verify(c => c.Remove(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Delete de un contacto inexistente debería devolver NotFound y NO limpiar la caché.
        /// </summary>
        [Test]
        public void Delete_ConContactoNoExistente_RetornarErrorNotFound() {
            // Arrange
            _repositoryMock.Setup(r => r.Delete(999, It.IsAny<bool>()))
                .Returns(Result.Failure<Contacto, DomainError>(ContactoErrors.NotFound("999")));

            // Act
            var r = _service.Delete(999);

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<ContactoError.NotFound>();
            r.Error.Message.Should().Contain("999");
            _repositoryMock.Verify(r => r.Delete(999, true), Times.Once);
            _cacheMock.Verify(c => c.Remove(It.IsAny<int>()), Times.Never);
        }
    }

    // ─── CASOS MIXTOS ────────────────────────────────────────────────

    /// <summary>
    /// Casos mixtos: lógica de caché combinando varias operaciones.
    /// </summary>
    [TestFixture]
    public class CasosMixtos : AgendaServicesTest {

        /// <summary>
        /// Tras un Update y un Delete, la caché queda invalidada:
        /// el siguiente GetById vuelve a leer del repositorio.
        /// </summary>
        [Test]
        public void UpdateYDelete_InvalidanCache_GetByIdVuelveALeerDeLaBD() {
            // Arrange
            var actualizado = NuevoContacto(1, nombre: "Ana García Vega");
            _repositoryMock.Setup(r => r.GetById(1)).Returns(NuevoContacto(1));
            _repositoryMock.Setup(r => r.Update(1, It.IsAny<Contacto>()))
                .Returns((int id, Contacto c) => Result.Success<Contacto, DomainError>(c));
            _repositoryMock.Setup(r => r.Delete(1, It.IsAny<bool>()))
                .Returns((int id, bool _) => Result.Success<Contacto, DomainError>(actualizado));
            _cacheMock.Setup(c => c.Get(It.IsAny<int>())).Returns((Contacto?)null);

            // Act
            _service.GetById(1).IsSuccess.Should().BeTrue();   // 1º: lee BD y guarda en caché
            _service.Update(1, actualizado);                    // 2º: invalida la caché
            _service.Delete(1);                                 // 3º: vuelve a invalidar

            // El 4º GetById NO debe encontrar nada en caché → consulta la BD otra vez
            var r = _service.GetById(1);

            // Assert
            r.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(repo => repo.GetById(1), Times.Exactly(2));
            _cacheMock.Verify(c => c.Remove(1), Times.Exactly(2));
        }

        /// <summary>
        /// Tras un Save exitoso, el contacto queda en caché:
        /// un GetById posterior no debe consultar la BD.
        /// </summary>
        [Test]
        public void Save_DejaElContactoEnCache_GetByIdNoConsultaBD() {
            // Arrange
            // Simulamos la caché "de verdad" con una variable local que se llena al hacer Add.
            Contacto? guardadoEnCache = null;
            _repositoryMock.Setup(r => r.Create(It.IsAny<Contacto>()))
                .Returns((Contacto c) => Result.Success<Contacto, DomainError>(c));
            _repositoryMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Contacto?)null);
            _cacheMock.Setup(c => c.Add(It.IsAny<int>(), It.IsAny<Contacto>()))
                .Callback<int, Contacto>((id, c) => guardadoEnCache = c);
            _cacheMock.Setup(c => c.Get(It.IsAny<int>()))
                .Returns((int id) => guardadoEnCache?.Id == id ? guardadoEnCache : null);

            // Act
            _service.Save(NuevoContacto(1));
            var r = _service.GetById(1);

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Id.Should().Be(1);
            _repositoryMock.Verify(repo => repo.GetById(It.IsAny<int>()), Times.Never);
        }
    }
}