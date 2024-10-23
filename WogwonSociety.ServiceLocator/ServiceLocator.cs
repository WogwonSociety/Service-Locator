using System.Reflection;

namespace WogwonSociety.ServiceLocator;

public enum ServiceLifetime
{
    Singleton,
    Transient
}

public class ServiceLocator
{
    private readonly Dictionary<Type, Func<object>> _transientServices = new();
    private readonly Dictionary<Type, object> _singletonServices = new();
    private readonly Dictionary<string, Func<object>> _namedServices = new();
    private readonly object _syncLock = new();
    private readonly Dictionary<string, List<Func<object>>> _taggedServices = new();

    // Fluent Registration
    public ServiceLocator Register<T>(Func<T> resolver, ServiceLifetime lifetime = ServiceLifetime.Transient,
        string name = null)
    {
        lock (_syncLock)
        {
            if (name != null)
            {
                _namedServices[name] = () => resolver();
            }
            else
            {
                Register(typeof(T), () => (object)resolver(), lifetime);
            }
        }

        return this;
    }

    // Try Register (conditional registration)
    public ServiceLocator TryRegister<T>(Func<T> resolver, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        lock (_syncLock)
        {
            if (!_singletonServices.ContainsKey(typeof(T)) && !_transientServices.ContainsKey(typeof(T)))
            {
                Register(resolver, lifetime);
            }
        }

        return this;
    }

    // Register Lazy Service
    public ServiceLocator RegisterLazy<T>(Func<T> resolver, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Register(() => new Lazy<T>(resolver).Value, lifetime);
        return this;
    }

    // Register by type and implementation
    public ServiceLocator Register(Type serviceType, Func<object> resolver, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        lock (_syncLock)
        {
            if (lifetime == ServiceLifetime.Singleton)
            {
                if (_singletonServices.ContainsKey(serviceType))
                {
                    throw new InvalidOperationException(
                        $"{serviceType.FullName} is already registered as a singleton.");
                }

                _singletonServices[serviceType] = resolver();
            }
            else
            {
                _transientServices[serviceType] = resolver;
            }
        }

        return this;
    }

    // Dynamic assembly type registration
    public ServiceLocator Register<TServiceBase>(Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var serviceTypes = assembly.GetTypes()
            .Where(t => typeof(TServiceBase).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in serviceTypes)
        {
            Register(typeof(TServiceBase), () => Activator.CreateInstance(type), lifetime);
        }

        return this;
    }

    public ServiceLocator Register(object instance, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var instanceType = instance.GetType();

        // Register the instance directly
        Register(instanceType, () => instance, lifetime);

        return this;
    }

    public ServiceLocator Register(Type serviceType, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        // Register the type by creating a new instance (constructor dependencies will be resolved)
        Register(serviceType, () => CreateInstance(serviceType), lifetime);

        return this;
    }

    private object CreateInstance(Type type)
    {
        // Get the first constructor
        var ctor = type.GetConstructors().FirstOrDefault();
        if (ctor == null)
        {
            return Activator.CreateInstance(type); // Fallback to parameterless constructor if no constructors
        }

        // Get constructor parameters and resolve them from the ServiceLocator
        var parameters = ctor.GetParameters()
            .Select(p => Get(p.ParameterType, false)) // Resolve each parameter
            .ToArray();

        return Activator.CreateInstance(type, parameters);
    }

    // Resolve by type
    public T Get<T>(bool throwOnFailure = true)
    {
        return (T)Get(typeof(T), throwOnFailure);
    }

    // Resolve by type with support for throwOnFailure
    public object Get(Type serviceType, bool throwOnFailure = true)
    {
        lock (_syncLock)
        {
            if (_singletonServices.TryGetValue(serviceType, out var singletonService))
            {
                return singletonService;
            }

            if (_transientServices.TryGetValue(serviceType, out var transientServiceFactory))
            {
                return transientServiceFactory();
            }
        }

        if (throwOnFailure)
        {
            throw new InvalidOperationException($"Service of type {serviceType} not registered.");
        }

        return null;
    }


    // Resolve by name
    public T Get<T>(string name, bool throwOnFailure = true)
    {
        lock (_syncLock)
        {
            if (_namedServices.TryGetValue(name, out var serviceFactory))
            {
                return (T)serviceFactory();
            }
        }

        return HandleMissingService<T>(throwOnFailure, $"Service named {name} not registered.");
    }

    // Resolve with default implementation
    public T GetOrDefault<T>(Func<T> defaultResolver = null)
    {
        lock (_syncLock)
        {
            if (_singletonServices.TryGetValue(typeof(T), out var singletonService))
            {
                return (T)singletonService;
            }

            if (_transientServices.TryGetValue(typeof(T), out var transientServiceFactory))
            {
                return (T)transientServiceFactory();
            }

            return defaultResolver != null ? defaultResolver() : default;
        }
    }

    // Handle missing service gracefully
    private T HandleMissingService<T>(bool throwOnFailure, string message)
    {
        if (throwOnFailure)
        {
            throw new InvalidOperationException(message);
        }

        return default;
    }

    // Scoped Resolution
// Scoped Resolution (with precedence for overrides)
    public ServiceLocator CreateScope(Dictionary<Type, Func<object>> scopedOverrides = null)
    {
        var scope = new ServiceLocator();

        lock (_syncLock)
        {
            // Copy existing singleton and transient services
            foreach (var singleton in _singletonServices)
            {
                scope._singletonServices[singleton.Key] = singleton.Value;
            }

            foreach (var transient in _transientServices)
            {
                scope._transientServices[transient.Key] = transient.Value;
            }

            // Apply overrides (they should take precedence over existing services)
            if (scopedOverrides != null)
            {
                foreach (var overrideService in scopedOverrides)
                {
                    // Replace any existing singleton/transient with the override
                    scope._singletonServices[overrideService.Key] = overrideService.Value();
                }
            }
        }

        return scope;
    }


    // Resolve by tag
    public IEnumerable<T> GetByTag<T>(string tag)
    {
        if (_taggedServices.TryGetValue(tag, out var services))
        {
            return services.Select(serviceFactory => (T)serviceFactory()).ToList();
        }

        throw new InvalidOperationException($"No services registered under tag: {tag}");
    }

    // Register with tag
    public ServiceLocator RegisterWithTag<T>(Func<T> resolver, string tag,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        lock (_syncLock)
        {
            Register(resolver, lifetime);
            if (!_taggedServices.ContainsKey(tag))
            {
                _taggedServices[tag] = new List<Func<object>>();
            }

            _taggedServices[tag].Add(() => resolver() ?? throw new InvalidOperationException());
        }

        return this;
    }
    
    public ServiceLocator Register<T>(T instance, string[] tags, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        lock (_syncLock)
        {
            Register(instance, lifetime);
            foreach (var tag in tags)
            {
                if (!_taggedServices.ContainsKey(tag))
                {
                    _taggedServices[tag] = new List<Func<object>>();
                }

                _taggedServices[tag].Add(() => instance);
            }
        }

        return this;
    }

    // Reset all services
    public void Reset()
    {
        lock (_syncLock)
        {
            _singletonServices.Clear();
            _transientServices.Clear();
            _namedServices.Clear();
            _taggedServices.Clear();
        }
    }
}