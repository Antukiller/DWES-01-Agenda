using _01_Agenda.Entity;
using _01_Agenda.Models;

namespace _01_Agenda.Mapper;

/// <summary>
/// Mapeos entre la entidad de base de datos <see cref="ContactoEntity"/>
/// y el modelo de dominio <see cref="Models.Contacto"/>.
/// </summary>
/// <remarks>

public static class ContactoMapper {
    /// <summary>
    /// Convierte una entidad persistida en el modelo de dominio.
    /// </summary>
    /// <param name="entity">Entidad de base de datos.</param>
    /// <returns>Modelo de dominio equivalente.</returns>
    public static Contacto ToModel(this ContactoEntity entity) {
        return new Contacto {
            Id = entity.Id,
            Nombre = entity.Nombre,
            Alias = entity.Alias,
            Telefono = entity.Telefono,
            Email = entity.Email,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted
        };
    }

    /// <summary>
    /// Convierte un modelo de dominio en la entidad que persistira la base de datos.
    /// </summary>
    /// <param name="model">Modelo de dominio.</param>
    /// <returns>Entidad equivalente lista para persistir.</returns>
    public static ContactoEntity ToEntity(this Contacto model) {
        return new ContactoEntity {
            Id = model.Id,
            Nombre = model.Nombre,
            Alias = model.Alias,
            Telefono = model.Telefono,
            Email = model.Email,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            IsDeleted = model.IsDeleted
        };
    }
}