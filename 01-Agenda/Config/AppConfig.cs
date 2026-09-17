
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace _01_Agenda.Config;

public class AppConfig {
    private static readonly IConfiguration Configuration;

    /// <summary>
    /// Inicializa la configuración leyendo el archivo appsettings.json.
    /// </summary>
    static AppConfig() {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", false, true)
            .Build();
    }

    public static CultureInfo Locale => CultureInfo.GetCultureInfo("es-ES");

    /// <summary>Directorio donde se almacenan los datos.</summary>
    public static string DataFolder => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        Configuration.GetValue<string>("Repository:Directory") ?? "data");

    public static string Repository => Configuration.GetValue<string>("Repository:Directory");

    /// <summary>Tamaño de la caché LRU.</summary>
    public static int CacheSize => Configuration.GetValue("Cache:Size", 15);

    public static bool UseLogicalDelete => Configuration.GetValue("Repository:UseLogicalDelete", true);
}
