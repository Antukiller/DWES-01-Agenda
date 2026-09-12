using _01_Agenda.Models;

namespace _01_Agenda.Repositories.Base;

public interface ICrudRepository {
    IEnumerable<Contacto> GetAll(int pagina, int tamPagina, bool isDeleteInclude);

    Contacto? GetById(int id);
    
    
}