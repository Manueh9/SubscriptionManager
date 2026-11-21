# 🔬 Guía de Código — `DTO/`

[← Índice](./CodeGuide.es.md)

Diez ficheros, todos siguiendo el mismo patrón: un `record` de C# por forma de dato que cruza la frontera HTTP. Antes de entrar en cada uno, el patrón común:

```csharp
public record CategoryRequest(string Name);
```

- **`record`** en vez de `class`: un `record` es, por valor, comparable (dos `CategoryRequest` con el mismo `Name` se consideran iguales con `==`) e inmutable por defecto (sus propiedades son de solo lectura una vez creado el objeto) — exactamente lo que se quiere de algo que solo existe para viajar en una petición/respuesta y no se va a mutar después.
- **Sintaxis posicional** (`(string Name)` justo después del nombre): genera automáticamente una propiedad pública `Name` y un constructor que la recibe — no hace falta escribir el cuerpo de la clase.
- **`*Request`** para lo que el cliente manda (`[FromBody]` implícito en una acción `POST`/`PUT` — ASP.NET Core deserializa el JSON del cuerpo de la petición a este tipo). **`*Response`** para lo que el controller devuelve.

---

## `CategoryRequest.cs`
```csharp
public record CategoryRequest(string Name);
```
El cuerpo que espera tanto crear como renombrar una categoría (`POST /api/Categories` y `PUT /api/Categories/{id}`) — un único campo, el nombre. Nótese que **no valida nada aquí**: si `Name` viene vacío, este `record` se construye sin quejarse; es `Category.EnsureIsValid` (en el dominio) quien lo rechaza más tarde, con un `400` vía `ArgumentExceptionHandler`.

## `CategoryResponse.cs`
```csharp
public record CategoryResponse(int Id, string Name, DateTime CreatedAt);
```
Lo que se devuelve al leer una categoría. Nótese que tiene exactamente los mismos campos que la entidad `Category` — en este caso concreto no hay diferencia entre "cómo se guarda" y "cómo se expone", pero se mantiene como DTO aparte igualmente, por coherencia con el resto del proyecto y para no acoplar el contrato público a la entidad de dominio (ver [Concepts.md § DTOs](../Concepts/Concepts.es.md#-dtos-contra-entidades-dos-formas-de-los-mismos-datos)).

## `ChangePriceRequest.cs`
```csharp
public record ChangePriceRequest(decimal NewPrice);
```
El cuerpo de `PUT /api/Subscriptions/{id}/price`. Un único campo. La validación real (que no sea negativo, que no sea igual al precio actual) vive en `PriceHistory.EnsureIsValid`, no aquí.

## `ChangeStatusRequest.cs`
```csharp
public record ChangeStatusRequest([EnumDataType(typeof(SubscriptionStatus))] SubscriptionStatus NewStatus);
```
Primer DTO con un atributo de validación de verdad. `[EnumDataType(typeof(SubscriptionStatus))]` se evalúa **antes** de que la petición llegue a la acción del controller (durante el *model binding* de ASP.NET Core): si el JSON trae, por ejemplo, `{ "newStatus": 999 }` — un número que no corresponde a ningún valor definido del enum (`Active`=0, `Paused`=1, `Cancelled`=2) —, la petición se rechaza automáticamente con `400` y un `ValidationProblemDetails` generado por el propio framework, sin que ninguna línea de código de este proyecto se ejecute siquiera. Esto es justamente lo que impide que un valor de enum "inventado" llegue nunca a tocar `Subscription.ChangeStatus`.

## `ExpensesSummaryResponse.cs`
```csharp
public record ExpensesSummaryResponse(decimal MonthlyTotal, decimal YearlyTotal, int ActiveSubscriptionCount);
```
Lo que devuelve `GET /api/Expenses/summary`. Tres números, ya calculados por el controller (ver [Controllers.es.md](./Controllers.es.md)) — este DTO no tiene ninguna lógica, solo transporta el resultado.

## `PriceHistoryResponse.cs`
```csharp
public record PriceHistoryResponse(
    int Id,
    int SubscriptionId,
    decimal OldPrice,
    decimal NewPrice,
    DateTime ChangedAt);
```
Espejo de la entidad `PriceHistory`, sin la propiedad de navegación `Subscription` (no tendría sentido serializar la suscripción entera dentro de cada registro de su propio historial — causaría además una referencia circular si `Subscription` a su vez listara sus `PriceHistory`, que no lo hace, pero es el motivo general por el que los DTOs nunca exponen propiedades de navegación directamente).

## `SubscriptionCreateRequest.cs`
```csharp
public record SubscriptionCreateRequest(
    string Name,
    string Description,
    decimal Price,
    [EnumDataType(typeof(BillingCycle))] BillingCycle BillingCycle,
    DateTime StartDate,
    int CategoryId);
```
El cuerpo de `POST /api/Subscriptions`. Igual que con `ChangeStatusRequest`, el atributo sobre `BillingCycle` rechaza automáticamente un valor de enum no definido (p. ej. `"billingCycle": 999`) antes de que la petición llegue al controller. `CategoryId` es solo el **id** — es el controller quien, con él, busca la `Category` real en la base de datos antes de poder construir la `Subscription` (que exige el objeto `Category` completo en su constructor).

## `SubscriptionResponse.cs`
```csharp
public record SubscriptionResponse(
    int Id,
    string Name,
    string Description,
    decimal Price,
    BillingCycle BillingCycle,
    DateTime StartDate,
    DateTime? EndDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SubscriptionStatus Status,
    int CategoryId);
```
Lo que se devuelve al leer una suscripción — sin atributos de validación (no tiene sentido validar algo que **sale** de la aplicación, no que entra). `EndDate` es anulable (`DateTime?`), igual que en la entidad.

## `SubscriptionUpdateRequest.cs`
```csharp
public record SubscriptionUpdateRequest(
    string Name,
    string Description,
    [EnumDataType(typeof(BillingCycle))] BillingCycle BillingCycle,
    DateTime StartDate,
    DateTime? EndDate,
    int CategoryId);
```
El cuerpo de `PUT /api/Subscriptions/{id}`. Comparado con `SubscriptionCreateRequest`, le añade `EndDate` (tiene sentido poder fijar una fecha de fin al editar, no solo al crear) y, sobre todo, **no tiene `Price`** — su ausencia aquí, a propósito, es la forma en la que el contrato de la API refleja la misma regla de negocio que ya impone `Subscription.UpdateDetails`: el precio no se cambia por este camino.

## `UpcomingRenewalResponse.cs`
```csharp
public record UpcomingRenewalResponse(
    int SubscriptionId,
    string Name,
    decimal Price,
    BillingCycle BillingCycle,
    DateTime NextRenewalDate,
    int CategoryId);
```
Lo que devuelve cada elemento de `GET /api/Subscriptions/upcoming`. `NextRenewalDate` es el único campo que no existe como tal en la entidad `Subscription` — es el resultado de llamar a `GetNextRenewalDate()`, calculado por el controller antes de construir este DTO (ver [Controllers.es.md](./Controllers.es.md)).
