using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _01_Agenda.Entity;

[Table("Contacto")]
public class ContactoEntity {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required] [MaxLength(50)] public string Nombre { get; set; }
    
    [Required] [MaxLength(50)] public string Telefono { get; set; }
    
    [Required] [MaxLength(50)] public string Email { get; set; }
    
    [Required] [MaxLength(50)] public string Alias { get; set; }
     
    [Column(TypeName = "datetime2")] public DateTime CreatedAt { get; set; }
    
    
    [Column(TypeName = "datetime2")] public DateTime UpdatedAt { get; set; }
    
    
    public bool IsDeleted { get; set; }
}