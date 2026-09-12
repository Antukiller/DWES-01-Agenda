
namespace _01_Agenda.Models;

public class Contacto {
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Telefono { get; set; }
    public string Email { get; set; }
    public string Alias { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public bool IsDeleted { get; set; }
}