using _01_Agenda.Cache;
using _01_Agenda.Config;
using _01_Agenda.Models;
using _01_Agenda.Repositories;
using _01_Agenda.Repositories.Base;
using _01_Agenda.Services;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace _01_Agenda.Infrastructure;

public static class DependenciesProvide {
    public static IServiceProvider BuildServiceProvider(Action<IServiceCollection>? configureAdditional = null) {
        var services = new ServiceCollection();

        RegisterCaches(services);
        RegisterRepositories(services);
        RegisterServices(services);

        return services.BuildServiceProvider();
    }



    private static void RegisterCaches(IServiceCollection services) {
        services.AddSingleton<ICache<int, Contacto>>(sp => 
            new LruCache<int, Contacto>(AppConfig.CacheSize));
    }

    private static void RegisterRepositories(IServiceCollection services) {
        services.AddSingleton<ICrudRepository, ContactoEfRepository>();
    }

    private static void RegisterServices(IServiceCollection services) {
        services.AddScoped<IAgendaServices, AgendaServices>(sp =>
            new AgendaServices(
                sp.GetRequiredService<ICrudRepository>(),
                sp.GetRequiredService<ICache<int, Contacto>>()
            ));
    }
}