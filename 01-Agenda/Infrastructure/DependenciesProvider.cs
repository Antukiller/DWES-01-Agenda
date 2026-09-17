using _01_Agenda.Cache;
using _01_Agenda.Config;
using _01_Agenda.Controller;
using _01_Agenda.Models;
using _01_Agenda.Repositories;
using _01_Agenda.Repositories.Base;
using _01_Agenda.Services;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace _01_Agenda.Infrastructure;

public static class DependenciesProvider {
    public static IServiceProvider BuildServiceProvider(Action<IServiceCollection>? configureAdditional = null) {
        var services = new ServiceCollection();

        RegisterCaches(services);
        RegisterRepositories(services);
        RegisterServices(services);
        RegisterControllers(services);

        return services.BuildServiceProvider();
    }



    private static void RegisterCaches(IServiceCollection services) {
        services.AddSingleton<ICache<int, Contacto>>(sp => 
            new LruCache<int, Contacto>(AppConfig.CacheSize));
    }

    private static void RegisterRepositories(IServiceCollection services) {
        services.AddScoped<AppDbContext>(sp => new AppDbContext("Data Source=agenda.db"));
        services.AddSingleton<ICrudRepository, ContactoEfRepository>();
    }

    private static void RegisterServices(IServiceCollection services) {
        services.AddScoped<IAgendaServices, AgendaServices>(sp =>
            new AgendaServices(
                sp.GetRequiredService<ICrudRepository>(),
                sp.GetRequiredService<ICache<int, Contacto>>()
            ));
    }
    
    
    /// <summary>
    ///     En frameworks web y arquitecturas estándar, los controladores se registran como Transient
    //      Llega la petición: El sistema le pide al contenedor DI un ContactoController.
    //      Se crea la instancia: C# crea un controlador fresco, le inyecta los servicios que necesita y ejecuta el método Dispatch().
    //      Se devuelve la respuesta: En cuanto termina de procesar y devuelve el ResponseDto, el controlador deja de usarse y el recolector de basura (Garbage Collector) lo destruye.
    //      Nueva petición: Cuando llega otra solicitud, se crea un objeto ContactoController completamente nuevo desde cero.
    /// </summary>
    /// <param name="services"></param>
    private static void RegisterControllers(IServiceCollection services) {
       // Si el controlador fuera un Singleton (una sola instancia para siempre), cualquier variable global o atributo que tuviera esa clase
       // mantendría los datos de la persona anterior.
       // Si el controlador fuera un Singleton (una sola instancia para siempre), cualquier variable global o atributo que tuviera esa clase mantendría los datos de la persona anterior.
        services.AddTransient<ContactoController>();
    }
}