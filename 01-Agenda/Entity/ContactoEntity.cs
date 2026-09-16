using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _01_Agenda.Entity;


[Table("Contacto")]

public class ContactoEntity {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required]
    [MaxLength(50)]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    [MaxLength(50)]
    public string Alias { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; }
    [Required]  
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; }
}