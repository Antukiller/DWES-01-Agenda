using _01_Agenda.Models;
using _01_Agenda.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace _01_Agenda.Test.Repositories;

public class ContactoEfRepositoryTest {
    
    [TestFixture]
    public class CasosPositivos {
        private SqliteConnection _connection = null!;


        [SetUp]
        public void Setup() {
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

        private AppDbContext _context = null!;
        private ContactoEfRepository _repository = null!;


        [Test]
        public void Create_ContactoValido_CrearCorrectamente() {
            // Arrange
            var c = new Contacto {
                Nombre = "Cristiano",
                Alias = "El Bicho",
                Email = "Cr7@gmail.com",
                Telefono = "+34687654321"
            };
            
            // Act
            var r =  _repository.Create(c);
            
            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Id.Should().Be(1);
        }

        [Test]
        public void GetById_CuandoExiste_RetornarContacto() {
            // Arrange
            var c = new Contacto {
                Nombre = "Cristiano",
                Alias = "El Bicho",
                Email = "Cr7@gmail.com",
                Telefono = "+34687654321"
            };
            
            _repository.Create(c);
            var r = _repository.GetById(1);
            
            r.Should().NotBeNull();
            r.Id.Should().Be(1);
        }

        [Test]
        public void GetByAlias_CuandoExiste_RetornaContacto() {
            // Arrange
            var c = new Contacto {
                Nombre = "Cristiano",
                Alias = "El Bicho",
                Email = "Cr7@gmail.com",
                Telefono = "+34687654321"
            };
            
            _repository.Create(c);
            
            // Act
            var r = _repository.GetByAlias("El Bicho");
            
            r.Should().NotBeNull();
            r!.Alias.Should().Be("El Bicho");
        }
        
        
    }
}