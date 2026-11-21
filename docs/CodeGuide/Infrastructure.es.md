# 🔬 Guía de Código — `Infrastructure/`

[← Índice](./CodeGuide.es.md)

Tres ficheros directamente bajo `Infrastructure/` (los dos manejadores de excepciones y `StartupSecurity`). Lo que cuelga de `Infrastructure/Persistence/` se explica aparte, en [Persistence.es.md](./Persistence.es.md).

---

## `ArgumentExceptionHandler.cs`

```csharp
public class ArgumentExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ArgumentException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request.",
            Detail = exception.Message
        }, cancellationToken);

        return true;
    }
}
```

`IExceptionHandler` es una interfaz de ASP.NET Core con un único método, `TryHandleAsync`. El middleware `UseExceptionHandler()` (registrado en `Program.cs`), cuando algo lanza una excepción sin capturar, prueba cada `IExceptionHandler` registrado **en el orden en que se registraron**, llamando a su `TryHandleAsync`, hasta que uno devuelva `true` (significa "yo me encargo de esto") o se acaben — si ninguno la reclama, la petición termina en un `500` genérico.

- `if (exception is not ArgumentException) return false;` — el patrón `is not T` comprueba el tipo en tiempo de ejecución; si la excepción no es (ni hereda de) `ArgumentException`, este manejador dice "esto no es para mí" devolviendo `false`, y el middleware pasa al siguiente manejador registrado. Importante: `ArgumentOutOfRangeException` **hereda** de `ArgumentException`, así que este `if` también la atrapa — de ahí que tanto `Subscription.ChangeStatus` (que lanza `ArgumentOutOfRangeException`) como `Category.EnsureIsValid` (que lanza `ArgumentException` directamente) acaben los dos aquí.
- `httpContext.Response.StatusCode = ...` — fija el código de estado HTTP de la respuesta a mano.
- `WriteAsJsonAsync(new ProblemDetails {...}, cancellationToken)` — serializa un objeto `ProblemDetails` (el formato estándar RFC 7807 — `status`/`title`/`detail`) y lo escribe como cuerpo de la respuesta, en JSON. `Detail = exception.Message` reutiliza directamente el mensaje de la excepción C# original como explicación legible del error — por ejemplo, el mensaje que genera `ArgumentException.ThrowIfNullOrWhiteSpace` o `ArgumentOutOfRangeException.ThrowIfNegative`.
- `return true;` — "ya está manejado, no sigas probando otros manejadores ni dejes que esto se convierta en un 500".

---

## `DbUpdateExceptionHandler.cs`

```csharp
public class DbUpdateExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException dbUpdateException || !dbUpdateException.IsConstraintViolation())
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict.",
            Detail = "The request conflicts with the current state of the data (e.g. a record still referenced by another, or a duplicate)."
        }, cancellationToken);

        return true;
    }
}
```

Mismo patrón que el anterior, con dos diferencias importantes:

- **La comprobación es doble**: `exception is not DbUpdateException dbUpdateException` usa el patrón de **asignación por patrón** de C# — si la comprobación de tipo pasa, el resultado queda disponible como la variable `dbUpdateException` (tipada ya como `DbUpdateException`, sin necesidad de un cast aparte) para usarla en el resto del método. El `||` encadena una segunda condición: aunque sí sea un `DbUpdateException`, **solo** se reclama si además `IsConstraintViolation()` (ver `DbUpdateExceptionExtensions.cs` más abajo) devuelve `true`. Esto es deliberado: EF Core lanza `DbUpdateException` para **cualquier** fallo al guardar, no solo por una restricción violada — por ejemplo, un bloqueo temporal de SQLite bajo escrituras concurrentes también es un `DbUpdateException`, pero **no** es un conflicto de datos; si se tratara igual, un error transitorio de infraestructura se le reportaría al cliente como "esto ya existe", que sería mentira.
- Si no es una violación de restricción genuina, este manejador devuelve `false` igualmente — la excepción sigue sin reclamar, y como no hay un tercer manejador registrado, acaba siendo un `500` real (correcto: un bloqueo transitorio de base de datos es un fallo del servidor, no un error del cliente).
- El mensaje de `Detail` aquí es genérico (no reutiliza `exception.Message`, al contrario que el manejador anterior) porque el mensaje real de una `DbUpdateException`/`SqliteException` suele incluir detalles técnicos de SQL poco útiles para quien consume la API.

---

## `StartupSecurity.cs`

```csharp
public static class StartupSecurity
{
    private static readonly string[] AllowedHosts = ["localhost", "127.0.0.1", "::1", "[::1]"];

    public static void EnsureLoopbackOnly(IEnumerable<string> urls)
    {
        foreach (var url in urls)
        {
            var host = BindingAddress.Parse(url).Host;

            if (!AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"La aplicación solo puede escuchar en localhost/127.0.0.1, no en '{host}' " +
                    $"(URL configurada: '{url}'). Revisa ASPNETCORE_URLS o launchSettings.json.");
            }
        }
    }
}
```

- `AllowedHosts` es un array **estático y de solo lectura** (`private static readonly`) — se crea una sola vez, compartida por todas las llamadas; no tiene sentido que cada llamada reconstruya la misma lista de cuatro strings.
- `["localhost", "127.0.0.1", "::1", "[::1]"]` — sintaxis de **colección simplificada** de C# reciente (equivalente a `new string[] { ... }`, pero sin repetir el tipo). Incluye tanto `"::1"` como `"[::1]"` porque, según el formato exacto de la URL de entrada, `BindingAddress` puede devolver la dirección IPv6 de loopback con o sin los corchetes — se cubren las dos formas por seguridad, en vez de asumir cuál usa.
- `public static void EnsureLoopbackOnly(IEnumerable<string> urls)` — un método **estático** (no hace falta instanciar `StartupSecurity` para usarlo; de hecho, la clase entera es `static`, así que ni se podría) que recibe cualquier colección de strings (`IEnumerable<string>` acepta tanto un `List<string>` como el `ICollection<string>` real de `app.Urls`).
- `BindingAddress.Parse(url).Host` — `BindingAddress` es una clase de Kestrel (no de `System.Uri`) pensada justamente para interpretar las URLs de escucha de ASP.NET Core, incluidos casos especiales que `System.Uri` no entendería, como `http://*:5000` (escuchar en "cualquier interfaz", con `*` literal) o `http://+:5000` — se usa deliberadamente esta clase en vez de `new Uri(url)` para que esos wildcards se reconozcan correctamente como "no es loopback" en vez de hacer que el `Uri` normal lance una excepción de formato al intentar parsearlos.
- `AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase)` — comparación de strings que ignora mayúsculas/minúsculas (`"LOCALHOST"` también valdría, aunque en la práctica nunca se escriba así).
- Si el host no está en la lista permitida, lanza `InvalidOperationException` con un mensaje en español, explicando exactamente qué URL fue el problema y dónde mirar para arreglarlo. Esta excepción **no la captura nadie** — se propaga hasta la terminal donde se ejecutó `dotnet run`, y el proceso termina ahí mismo, sin llegar a `app.Run()`. Es intencional: un fallo de seguridad de este tipo debe **impedir el arranque**, no solo registrarse como un aviso que podría pasar desapercibido.

Se llama desde `Program.cs` justo después de `builder.Build()`, pasándole `app.Urls` — ver [CodeGuide.es.md](./CodeGuide.es.md#-programcs-línea-a-línea) para el contexto completo de cuándo exactamente se ejecuta esto dentro de la secuencia de arranque.
