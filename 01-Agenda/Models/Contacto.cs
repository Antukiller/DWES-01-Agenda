using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _01_Agenda.Models;

/// <summary>
/// Contacto es a la vez el MODELO (datos de la app) y la ENTIDAD de EF Core (tabla "Contacto").
/// Por eso no hay clase Entity aparte ni mappers: EF mapea esta clase directamente a SQLite.
/// </summary>
[Table("Contacto")] // nombre real de la tabla en la base de datos
public class Contacto {
    [Key]                                                           // clave primaria
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]           // la BD asigna el Id automáticamente
    public int Id { get; set; }

    // Atributos de validación: [Required] = columna NOT NULL, MaxLength(50) = tamaño de la columna.
    // El "= \"\"" evita el warning de que el string podría ser null.
    [Required, MaxLength(50)] public string Nombre { get; set; } = "";
    [Required, MaxLength(50)] public string Telefono { get; set; } = "";
    [Required, MaxLength(50)] public string Email { get; set; } = "";
    [Required, MaxLength(50)] public string Alias { get; set; } = "";

    // Auditoría: cuándo se creó / se actualizó, y si está borrado lógico.
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } // true = borrado lógico (no se muestra en las listas normales)
}