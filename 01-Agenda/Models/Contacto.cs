using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _01_Agenda.Models;

public class Contacto {
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; }  = DateTime.Now;
    public bool IsDeleted { get; set; } = false;
}