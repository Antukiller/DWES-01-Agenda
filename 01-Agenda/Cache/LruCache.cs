using Serilog;

namespace _01_Agenda.Cache;

public class LruCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : notnull {
    private readonly int _capacity;
    private readonly Dictionary<TKey, TValue> _data = new();
    private readonly ILogger _logger = Log.ForContext<LruCache<TKey, TValue>>();
    private readonly LinkedList<TKey> _usageOrder = new();

    /// <summary>
    /// Inicializa una nueva instancia de la caché LRU con la capacidad especificada.
    /// </summary>
    /// <param name="capacity">Capacidad máxima de elementos en la caché.</param>
    public LruCache(int capacity) {
        if (capacity <= 0)
            throw new ArgumentException("La capacidad debe ser mayor que 0.", nameof(capacity));
        _capacity = capacity;
    }

    /// <inheritdoc cref="ICache{TKey,TValue}.Add" />
    public void Add(TKey key, TValue value) {
        _logger.Debug("[LRU-ADD] Intentando añadir clave: {Key}", key);

        if (_data.TryGetValue(key, out var existingValue)) {
            _logger.Debug("[LRU-ADD] Clave {Key} ya existe. Actualizando valor: {Old} -> {New}",
                key, existingValue, value);
            _data[key] = value;
            RefreshUsage(key);
            return;
        }

        _logger.Debug("[LRU-ADD] Clave {Key} es nueva. Capacidad actual: {Used}/{Total}",
            key, _data.Count, _capacity);

        if (_data.Count >= _capacity) {
            var oldestKey = _usageOrder.First!.Value;
            var oldestValue = _data[oldestKey];
            _logger.Debug("[LRU-EVICT] Cache llena. Desalojando elemento más antiguo: {Key} = {Value}",
                oldestKey, oldestValue);
            _usageOrder.RemoveFirst();
            _data.Remove(oldestKey);
        }

        _data.Add(key, value);
        _usageOrder.AddLast(key);
        _logger.Debug("[LRU-ADD] Elemento añadido. Nueva lista de uso: {Order}",
            string.Join(" -> ", _usageOrder));
    }

    /// <inheritdoc cref="ICache{TKey,TValue}.Get" />
    public TValue? Get(TKey key) {
        _logger.Debug("[LRU-GET] Buscando clave: {Key}", key);

        if (!_data.TryGetValue(key, out var value)) {
            _logger.Debug("[LRU-GET] Clave {Key} NO encontrada en cache", key);
            return default;
        }

        _logger.Debug("[LRU-GET] Clave {Key} encontrada con valor: {Value}. Rejuveneciendo...",
            key, value);
        RefreshUsage(key);
        _logger.Debug("[LRU-GET] Lista tras rejuvenecimiento: {Order}",
            string.Join(" -> ", _usageOrder));

        return value;
    }

    /// <inheritdoc cref="ICache{TKey,TValue}.Remove" />
    public bool Remove(TKey key) {
        _logger.Debug("[LRU-REMOVE] Intentando eliminar clave: {Key}", key);

        if (!_data.Remove(key)) {
            _logger.Debug("[LRU-REMOVE] Clave {Key} no encontrada", key);
            return false;
        }

        _usageOrder.Remove(key);
        _logger.Debug("[LRU-REMOVE] Clave {Key} eliminada correctamente", key);
        return true;
    }

    /// <summary>
    /// Muestra el estado actual de la caché (capacidad usada y orden de uso).
    /// </summary>
    public void DisplayStatus() {
        _logger.Information("[LRU-STATUS] Capacidad: {Used}/{Total}", _data.Count, _capacity);
        _logger.Information("[LRU-STATUS] Uso (Menos reciente -> Más reciente): {Order}",
            string.Join(" -> ", _usageOrder));
    }

    /// <summary>
    ///     Mueve una clave existente a la última posición de la lista de uso.
    ///     Este método es el corazón del algoritmo LRU.
    /// </summary>
    /// <param name="key">La clave del elemento que acaba de ser utilizado.</param>
    private void RefreshUsage(TKey key) {
        _logger.Verbose("[LRU-REFRESH] Moviendo clave {Key} al final de la lista", key);
        _usageOrder.Remove(key);
        _usageOrder.AddLast(key);
    }
}
