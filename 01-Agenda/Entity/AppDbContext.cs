using _01_Agenda.Models;
using Microsoft.EntityFrameworkCore;


// ─── DB CONTEXT ───

/// <summary>
/// Contexto de base de datos para la aplicación de agenda de contactos.
/// </summary>
public class AppDbContext : DbContext {
    private readonly string? _connectionString;

    /// <summary>
    /// Constructor para uso manual (ej. en tests o herramientas de CLI).
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a la base de datos.</param>
    public AppDbContext(string connectionString) {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Constructor estándar para Inyección de Dependencias.
    /// </summary>
    /// <param name="options">Opciones de configuración del contexto.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
    }

    /// <summary>
    /// Conjunto de entidades de tipo contacto.
    /// </summary>
    public DbSet<Contacto> Contacto { get; set; } = null!;

    /// <summary>
    /// Configura el contexto si no viene ya configurado desde el ServiceCollection.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_connectionString)) {
            optionsBuilder.UseSqlite(_connectionString);
        }
    }

    /// <summary>
    /// Garantiza que la base de datos y sus tablas estén creadas.
    /// </summary>
    public void EnsureCreated() {
        Database.EnsureCreated();
    }
}