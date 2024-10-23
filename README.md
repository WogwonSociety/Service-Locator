# 🌙 WogwonSociety - Service Locator

**WogwonSociety.ServiceLocator** is a lightweight and flexible service locator for .NET that simplifies dependency management in your applications. With support for multiple lifetimes, named and tagged services, constructor injection, and scoped resolution, this service locator is easy to use and helps manage complex service dependencies efficiently.

## Features
- **Service Lifetime Control**: Register services as **Singleton** or **Transient**.
- **Named and Tagged Services**: Easily register and resolve services by name or tag.
- **Lazy Registration**: Services can be registered lazily and resolved only when needed.
- **Constructor Injection**: Automatically resolves and injects constructor dependencies.
- **Scoped Resolutions**: Create scoped instances with service overrides.
- **Fluent API**: Chainable method calls for easy registration.

## Installation

Install the package via NuGet:

```bash
dotnet add package WogwonSociety.ServiceLocator
```

## Getting Started

1. Register services with the service locator:

```csharp
var locator = new ServiceLocator();

// Register singleton service
locator.Register(() => new MyService(), ServiceLifetime.Singleton);

// Register transient service
locator.Register(() => new MyService(), ServiceLifetime.Transient);
```

2. Resolve services:

```csharp
// Resolve service
var service = locator.Get<MyService>();
```

3. Named services:

```csharp
locator.Register(() => new MyService(), name: "SpecialService");

var namedService = locator.Get<MyService>("SpecialService");
```

4. Scoped Resolution:

```csharp
var scopedLocator = locator.CreateScope(new Dictionary<Type, Func<object>>
{
    { typeof(MyService), () => new ScopedService() }
});

var scopedService = scopedLocator.Get<MyService>();
```

5. Lazy Registration:

```csharp
locator.RegisterLazy(() => new ExpensiveService());

var service = locator.Get<ExpensiveService>(); // Created on first resolve
```

6. Tagged Services:

```csharp
locator.Register(() => new MyService(), tags: new[] { "tag1", "tag2" });

var service = locator.Get<MyService>("tag1");
```

7. Resetting the Service Locator:

```csharp
locator.Reset();
```

## License

This project is licensed under the MIT License