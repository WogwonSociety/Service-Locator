using System.Reflection;

namespace WogwonSociety.ServiceLocator.Tests;

public class ServiceLocatorTests
{
    [Fact]
    public void RegisterAndResolveSingletonService()
    {
        var locator = new ServiceLocator();
        locator.Register(() => "SingletonService", ServiceLifetime.Singleton);

        var service1 = locator.Get<string>();
        var service2 = locator.Get<string>();

        Assert.Equal("SingletonService", service1);
        Assert.Equal(service1, service2);
    }

    [Fact]
    public void RegisterAndResolveTransientService()
    {
        var locator = new ServiceLocator();
        locator.Register(() => new object(), ServiceLifetime.Transient);

        var service1 = locator.Get<object>();
        var service2 = locator.Get<object>();

        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void RegisterAndResolveNamedService()
    {
        var locator = new ServiceLocator();
        locator.Register(() => "NamedService", name: "MyService");

        var service = locator.Get<string>("MyService");

        Assert.Equal("NamedService", service);
    }

    [Fact]
    public void RegisterAndResolveTaggedService()
    {
        var locator = new ServiceLocator();
        locator.RegisterWithTag(() => "TaggedService", "MyTag");

        var services = locator.GetByTag<string>("MyTag");

        Assert.Contains("TaggedService", services);
    }

    [Fact]
    public void AutoRegisterServicesFromAssembly()
    {
        var locator = new ServiceLocator();
        locator.Register<IService>(Assembly.GetExecutingAssembly());

        var service = locator.Get<IService>();

        Assert.NotNull(service); // Now resolves to ConcreteService
        Assert.IsType<ConcreteService>(service); // Ensure correct type
    }


    [Fact]
    public void TryRegisterService()
    {
        var locator = new ServiceLocator();
        locator.TryRegister(() => "FirstService", ServiceLifetime.Singleton);
        locator.TryRegister(() => "SecondService", ServiceLifetime.Singleton);

        var service = locator.Get<string>();

        Assert.Equal("FirstService", service);
    }

    [Fact]
    public void RegisterLazyService()
    {
        var locator = new ServiceLocator();
        locator.RegisterLazy(() => "LazyService");

        var service = locator.Get<string>();

        Assert.Equal("LazyService", service);
    }

    [Fact]
    public void ResolveOrDefaultService()
    {
        var locator = new ServiceLocator();
        var service = locator.GetOrDefault(() => "DefaultService");

        Assert.Equal("DefaultService", service);
    }

    [Fact]
    public void CreateScopeWithOverrides()
    {
        var locator = new ServiceLocator();
        locator.Register(() => "OriginalService", ServiceLifetime.Singleton);

        var scopedLocator = locator.CreateScope(new Dictionary<Type, Func<object>>
        {
            { typeof(string), () => "OverriddenService" }
        });

        var service = scopedLocator.Get<string>();

        Assert.Equal("OverriddenService", service);
    }

    [Fact]
    public void ResetServices()
    {
        var locator = new ServiceLocator();
        locator.Register(() => "Service", ServiceLifetime.Singleton);
        locator.Reset();

        Assert.Throws<InvalidOperationException>(() => locator.Get<string>());
    }
    
    [Fact]
    public void RegisterInstance_ShouldRegisterSingletonInstance()
    {
        var locator = new ServiceLocator();
        var instance = new ConcreteService();

        locator.Register(instance);

        var resolvedInstance = locator.Get<ConcreteService>();

        Assert.Same(instance, resolvedInstance);
    }

    [Fact]
    public void RegisterType_ShouldRegisterTransientType()
    {
        var locator = new ServiceLocator();

        locator.Register(typeof(ConcreteService));

        var service1 = locator.Get<ConcreteService>();
        var service2 = locator.Get<ConcreteService>();

        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void RegisterType_ShouldRegisterSingletonType()
    {
        var locator = new ServiceLocator();

        locator.Register(typeof(ConcreteService), ServiceLifetime.Singleton);

        var service1 = locator.Get<ConcreteService>();
        var service2 = locator.Get<ConcreteService>();

        Assert.Same(service1, service2);
    }
    
    [Fact]
    public void RegisterWithTag_ShouldRegisterAndResolveTaggedService()
    {
        var locator = new ServiceLocator();
        locator.RegisterWithTag(() => "TaggedService", "MyTag");

        var services = locator.GetByTag<string>("MyTag");

        Assert.Contains("TaggedService", services);
    }

    [Fact]
    public void RegisterInstanceWithTags_ShouldRegisterAndResolveTaggedInstance()
    {
        var locator = new ServiceLocator();
        var instance = new ConcreteService();
        locator.Register(instance, new[] { "Tag1", "Tag2" });

        var servicesTag1 = locator.GetByTag<ConcreteService>("Tag1");
        var servicesTag2 = locator.GetByTag<ConcreteService>("Tag2");

        Assert.Contains(instance, servicesTag1);
        Assert.Contains(instance, servicesTag2);
    }

    [Fact]
    public void GetByTag_ShouldThrowIfTagNotRegistered()
    {
        var locator = new ServiceLocator();

        Assert.Throws<InvalidOperationException>(() => locator.GetByTag<string>("NonExistentTag"));
    }

    [Fact]
    public void CreateScope_ShouldOverrideServicesInScope()
    {
        var locator = new ServiceLocator();
        locator.Register(() => "OriginalService", ServiceLifetime.Singleton);

        var scopedLocator = locator.CreateScope(new Dictionary<Type, Func<object>>
        {
            { typeof(string), () => "OverriddenService" }
        });

        var service = scopedLocator.Get<string>();

        Assert.Equal("OverriddenService", service);
    }

    [Fact]
    public void CreateScope_ShouldInheritServicesFromParent()
    {
        var locator = new ServiceLocator();
        locator.Register(() => "OriginalService", ServiceLifetime.Singleton);

        var scopedLocator = locator.CreateScope();

        var service = scopedLocator.Get<string>();

        Assert.Equal("OriginalService", service);
    }
}



public interface IService
{
}

public class ConcreteService : IService
{
    public string Name => "ConcreteService";
}

