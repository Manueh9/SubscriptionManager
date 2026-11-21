# 🔬 Guía de Código — `Controllers/`

[← Índice](./CodeGuide.es.md)

Cuatro controllers. Antes de entrar en cada uno, el patrón compartido por los cuatro:

```csharp
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(SubscriptionManagerDbContext dbContext) : ControllerBase
```
- `[ApiController]` activa un conjunto de comportamientos automáticos de ASP.NET Core pensados para APIs: valida el modelo de entrada antes de ejecutar la acción (si algo como `[EnumDataType]` falla, la acción ni se llega a ejecutar — responde `400` sola), infiere de dónde viene cada parámetro (cuerpo JSON, ruta, query string) sin tener que anotarlo explícitamente en la mayoría de los casos, y exige que las rutas sean explícitas (de ahí `[Route]`).
- `[Route("api/[controller]")]` — `[controller]` es un marcador que ASP.NET Core sustituye por el nombre de la clase sin el sufijo `Controller` — para `CategoriesController`, la ruta base es `api/Categories`.
- `CategoriesController(SubscriptionManagerDbContext dbContext) : ControllerBase` — constructor primario (igual que en `SubscriptionManagerDbContext`): el parámetro `dbContext` es inyectado automáticamente por el contenedor de DI cada vez que llega una petición a este controller (ver [Concepts.md § Inyección de dependencias](../Concepts/Concepts.es.md#-inyección-de-dependencias-quién-construye-qué)), y queda disponible en todos los métodos de la clase sin tener que guardarlo en un campo a mano. `ControllerBase` (no `Controller`, que además añadería soporte de vistas HTML, innecesario en una API pura) da acceso a los métodos auxiliares usados abajo: `Ok(...)`, `NotFound()`, `Conflict(...)`, `BadRequest(...)`, `CreatedAtAction(...)`, `NoContent()` — cada uno construye el `ActionResult` con el código de estado HTTP correspondiente.

---

## `CategoriesController.cs`

### `Get()` — `GET /api/Categories`
```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<CategoryResponse>>> Get()
{
    var categories = await dbContext.Categories
        .Select(category => new CategoryResponse(category.Id, category.Name, category.CreatedAt))
        .ToListAsync();

    return Ok(categories);
}
```
`dbContext.Categories` es un `IQueryable<Category>` — en este punto **todavía no se ha ejecutado ninguna consulta SQL**. `.Select(...)` añade una proyección a esa consulta (equivalente a un `SELECT Id, Name, CreatedAt FROM Categories`, no `SELECT *`): EF Core traduce la expresión lambda a SQL, así que solo viajan de la base de datos las tres columnas que hacen falta, ya con la forma del DTO — nunca se materializa un objeto `Category` completo en memoria para esto. `.ToListAsync()` es lo que de verdad dispara la consulta contra SQLite, de forma asíncrona (no bloquea el hilo mientras espera la respuesta del motor de base de datos), y trae el resultado ya como lista. `Ok(categories)` construye una respuesta `200` con esa lista como cuerpo JSON.

### `GetById(int id)` — `GET /api/Categories/{id}`
```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<CategoryResponse>> GetById(int id)
{
    var category = await dbContext.Categories
        .AsNoTracking()
        .FirstOrDefaultAsync(category => category.Id == id);

    if (category is null)
    {
        return NotFound();
    }

    return Ok(ToResponse(category));
}
```
`{id:int}` en la ruta — el `:int` es una **restricción de ruta**: esta acción solo coincide si el segmento es un número entero (`/api/Categories/abc` no llegaría aquí, sino a un `404` de enrutamiento estándar, antes incluso de ejecutar nada). `AsNoTracking()` le dice a EF Core que no necesita vigilar este objeto para detectar cambios futuros (el *change tracker*, que por defecto EF Core activa en cada entidad que carga, para poder calcular automáticamente qué `UPDATE` hacer si luego se modifica y se guarda) — como aquí solo se **lee**, desactivarlo ahorra trabajo de seguimiento innecesario. `FirstOrDefaultAsync(...)` trae el primer resultado que cumpla la condición, o `null` si no hay ninguno (de ahí el nombre: "first or default"). Si es `null`, `NotFound()` → `404`. Si no, `ToResponse(category)` (el método privado al final del fichero) construye el DTO.

### `Create(CategoryRequest request)` — `POST /api/Categories`
```csharp
[HttpPost]
public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
{
    var nameInUse = await dbContext.Categories.AnyAsync(c => c.Name == request.Name);

    if (nameInUse)
    {
        return Conflict($"A category named '{request.Name}' already exists.");
    }

    var category = new Category(request.Name);

    dbContext.Categories.Add(category);

    if (!await dbContext.TrySaveChangesAsync())
    {
        return Conflict($"A category named '{request.Name}' already exists.");
    }

    return CreatedAtAction(nameof(GetById), new { id = category.Id }, ToResponse(category));
}
```
En `[ApiController]`, un parámetro de acción que no es un tipo primitivo (como `CategoryRequest`) se enlaza automáticamente al **cuerpo** de la petición, deserializándolo de JSON — no hace falta anotarlo con `[FromBody]` explícitamente (aunque se podría). Paso a paso:
1. `AnyAsync(c => c.Name == request.Name)` — comprobación previa: ¿existe ya una categoría con ese nombre? (Gracias a la colación `NOCASE` de la columna, esta comparación con `==` ya ignora mayúsculas por sí sola, sin necesitar nada especial en el código C#). Si sí, `Conflict(...)` → `409`, con un mensaje de texto plano como cuerpo.
2. `new Category(request.Name)` — construye la entidad, pasando por el constructor del dominio (y por tanto por `EnsureIsValid`; si `request.Name` viniera vacío, aquí lanzaría y acabaría en `400` vía `ArgumentExceptionHandler`, sin llegar más allá).
3. `dbContext.Categories.Add(category)` — la marca para ser insertada la próxima vez que se guarde (todavía no toca la base de datos).
4. `TrySaveChangesAsync()` — el guardado real. Si devuelve `false` (la carrera explicada en [Persistence.es.md](./Persistence.es.md#infrastructurepersistencesubscriptionmanagerdbcontextextensionscs)), responde el mismo `409` que la comprobación previa — mismo resultado para el cliente, sea cual sea el camino por el que se detectó el duplicado.
5. `CreatedAtAction(nameof(GetById), new { id = category.Id }, ToResponse(category))` — construye una respuesta `201 Created`, con tres cosas: el cuerpo (el DTO de la categoría recién creada), y una cabecera `Location` calculada automáticamente apuntando a la URL donde se podría volver a consultar este mismo recurso (`nameof(GetById)` evita escribir el nombre de la acción como string suelto — si algún día se renombra `GetById`, esto no compilaría hasta corregirlo, en vez de quedar con una URL rota en silencio).

### `Update(int id, CategoryRequest request)` — `PUT /api/Categories/{id}`
```csharp
[HttpPut("{id:int}")]
public async Task<IActionResult> Update(int id, CategoryRequest request)
{
    var category = await dbContext.Categories.FindAsync(id);

    if (category is null)
    {
        return NotFound();
    }

    var nameInUse = await dbContext.Categories.AnyAsync(c => c.Name == request.Name && c.Id != id);

    if (nameInUse)
    {
        return Conflict($"A category named '{request.Name}' already exists.");
    }

    category.Rename(request.Name);

    if (!await dbContext.TrySaveChangesAsync())
    {
        return Conflict($"A category named '{request.Name}' already exists.");
    }

    return NoContent();
}
```
`FindAsync(id)` es distinto de `FirstOrDefaultAsync` usado antes: busca primero en la memoria del *change tracker* (por si esa entidad ya se cargó antes en esta misma petición) antes de ir a la base de datos, y además **sí** activa el seguimiento de cambios (al contrario que con `AsNoTracking()`) — necesario aquí, porque luego se va a modificar (`category.Rename(...)`) y hay que guardar ese cambio. El `AnyAsync` de comprobación de nombre duplicado añade `&& c.Id != id` — para no chocar contra **sí misma**: renombrar una categoría "Streaming" a "Streaming" (sin cambiar nada) no debería considerarse un conflicto con ella misma. `category.Rename(request.Name)` llama al método del dominio (que vuelve a validar). Al no haber llamado a `Add` ni a `Remove`, EF Core sabe que lo que hay que guardar es un `UPDATE`, gracias a que el *change tracker* detectó que `Name` cambió. `NoContent()` → `204`, la respuesta estándar para "hecho, pero no hay nada que devolver".

### `Delete(int id)` — `DELETE /api/Categories/{id}`
```csharp
[HttpDelete("{id:int}")]
public async Task<IActionResult> Delete(int id)
{
    var category = await dbContext.Categories.FindAsync(id);

    if (category is null)
    {
        return NotFound();
    }

    dbContext.Categories.Remove(category);
    await dbContext.SaveChangesAsync();

    return NoContent();
}
```
La única acción de borrado que llama a `SaveChangesAsync()` **directamente**, sin pasar por `TrySaveChangesAsync()`. Es deliberado: si la categoría todavía tiene suscripciones, la restricción `OnDelete(DeleteBehavior.Restrict)` (ver [Persistence.es.md](./Persistence.es.md)) hace que SQLite rechace el `DELETE`, lanzando una `DbUpdateException` que **nadie captura en este método** — se propaga hasta el middleware global, donde `DbUpdateExceptionHandler` la reclama y responde `409`. No hace falta ningún `try/catch` local ni `TrySaveChangesAsync` aquí porque no hay ninguna comprobación previa que "adelantarse" a una carrera — el único camino para que esto falle es la propia restricción de la base de datos, y el manejador global ya cubre justo ese caso.

### `ToResponse` (privado)
```csharp
private static CategoryResponse ToResponse(Category category) =>
    new(category.Id, category.Name, category.CreatedAt);
```
Un método privado estático, compartido por `GetById` y `Create`, para no repetir la misma construcción del DTO dos veces. `new(...)` sin especificar el tipo explícitamente es **inferencia de tipo objetivo**: el compilador ya sabe, por la firma del método (`CategoryResponse`), qué se está construyendo.

---

## `SubscriptionsController.cs`

Mismo patrón general que `CategoriesController`, con matices propios en cada acción.

### `Get()` — `GET /api/Subscriptions`
Igual que en `CategoriesController.Get()`, pero proyectando los once campos de `SubscriptionResponse` directamente en la consulta — otra vez, para que SQLite solo devuelva las columnas necesarias.

### `GetById(int id)` — `GET /api/Subscriptions/{id}`
Igual patrón que `CategoriesController.GetById`.

### `GetUpcoming([FromQuery] int withinDays = 30)` — `GET /api/Subscriptions/upcoming`
```csharp
[HttpGet("upcoming")]
public async Task<ActionResult<IEnumerable<UpcomingRenewalResponse>>> GetUpcoming([FromQuery] int withinDays = 30)
{
    var today = DateTime.UtcNow.Date;
    var horizon = today.AddDays(withinDays);

    var subscriptions = await dbContext.Subscriptions
        .AsNoTracking()
        .Where(subscription => subscription.Status == SubscriptionStatus.Active)
        .ToListAsync();

    var upcoming = subscriptions
        .Select(subscription => (subscription, nextRenewalDate: subscription.GetNextRenewalDate(today)))
        .Where(x => x.nextRenewalDate is not null && x.nextRenewalDate <= horizon)
        .OrderBy(x => x.nextRenewalDate)
        .Select(x => new UpcomingRenewalResponse(
            x.subscription.Id,
            x.subscription.Name,
            x.subscription.Price,
            x.subscription.BillingCycle,
            x.nextRenewalDate!.Value,
            x.subscription.CategoryId));

    return Ok(upcoming);
}
```
La ruta `"upcoming"`, al no llevar `{}`, es un segmento literal — se combina con la ruta base del controller dando `api/Subscriptions/upcoming`. **Importante**: esta ruta literal tiene que coincidir **antes** de que `{id:int}` de `GetById` pueda reclamarla — ASP.NET Core resuelve esto automáticamente dando prioridad a las rutas más específicas (sin parámetro) sobre las que tienen comodines, así que el orden en que están escritos los métodos en el fichero no importa para esto.

`[FromQuery] int withinDays = 30` — viene de la *query string* (`?withinDays=45`), con `30` como valor por defecto si no se especifica.

El cálculo real pasa por **dos fases**, y esto es importante entenderlo:
1. **Primera consulta** (sí llega a SQL): trae **todas** las suscripciones `Active`, sin ningún filtro de fecha — porque `GetNextRenewalDate` (la lógica de calendario de `Subscription`) no es algo que EF Core pueda traducir a una cláusula `WHERE` de SQL; es código C# normal, con un `switch` y aritmética de fechas.
2. **Resto en memoria** (sobre la lista ya cargada, con LINQ a objetos, no a SQL): `.Select(subscription => (subscription, nextRenewalDate: subscription.GetNextRenewalDate(today)))` — una **tupla con nombres** (`(subscription, nextRenewalDate: ...)`), para llevar emparejados el objeto original y el resultado calculado, sin crear una clase aparte solo para esto. Luego `.Where(...)` descarta las que no tienen próxima renovación (`null` — pausadas/canceladas no debería pasar aquí porque ya se filtró `Active` arriba, pero también cubre el caso de `EndDate` ya superado) o que caen más allá del horizonte pedido. `.OrderBy(x => x.nextRenewalDate)` las ordena de la más próxima a la más lejana. El `.Select` final construye el DTO, usando `x.nextRenewalDate!.Value` — el `!` porque, aunque es `DateTime?`, en este punto el `.Where` de arriba ya garantizó que no es `null` (el compilador no puede saberlo por sí solo, así que hace falta decírselo).

### `Create(SubscriptionCreateRequest request)` — `POST /api/Subscriptions`
```csharp
var category = await dbContext.Categories.FindAsync(request.CategoryId);

if (category is null)
{
    return BadRequest($"Category {request.CategoryId} does not exist.");
}

var nameInUse = await dbContext.Subscriptions
    .AnyAsync(s => s.Name == request.Name && s.CategoryId == request.CategoryId);

if (nameInUse)
{
    return Conflict($"A subscription named '{request.Name}' already exists in this category.");
}

var subscription = new Subscription(
    request.Name,
    request.Description,
    request.Price,
    request.BillingCycle,
    request.StartDate,
    category);

dbContext.Subscriptions.Add(subscription);

if (!await dbContext.TrySaveChangesAsync())
{
    return Conflict($"A subscription named '{request.Name}' already exists in this category.");
}

return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, ToResponse(subscription));
```
Un paso más que `CategoriesController.Create`: primero resuelve la `Category` real a partir del `CategoryId` del DTO — si no existe, `BadRequest(...)` → `400` (no `404`: la convención de este proyecto es que una referencia inválida **dentro** del cuerpo de una petición de creación es un error del propio request, `400`, mientras que `404` se reserva para "el recurso de la URL no existe"). Solo si la categoría existe se comprueba el nombre duplicado **dentro de esa categoría** (`&& s.CategoryId == request.CategoryId`), y solo entonces se construye la `Subscription`, pasándole el objeto `Category` completo que ya se cargó (no solo su id) — recordando que el constructor de `Subscription` lo necesita entero para poder sacar `category.Id` él mismo.

### `Update(int id, SubscriptionUpdateRequest request)` — `PUT /api/Subscriptions/{id}`
Mismo patrón que `Create`, pero sobre una suscripción ya existente (`FindAsync(id)` primero) y llamando a `subscription.UpdateDetails(...)` en vez de construir una nueva — recordando que este método del dominio deliberadamente no acepta ni toca el precio.

### `ChangePrice(int id, ChangePriceRequest request)` — `PUT /api/Subscriptions/{id}/price`
```csharp
[HttpPut("{id:int}/price")]
public async Task<ActionResult<SubscriptionResponse>> ChangePrice(int id, ChangePriceRequest request)
{
    var subscription = await dbContext.Subscriptions.FindAsync(id);

    if (subscription is null)
    {
        return NotFound();
    }

    var priceHistory = subscription.ChangePrice(request.NewPrice);

    dbContext.PriceHistories.Add(priceHistory);
    await dbContext.SaveChangesAsync();

    return Ok(ToResponse(subscription));
}
```
`subscription.ChangePrice(request.NewPrice)` — llama al método del dominio (ver [Domain.es.md](./Domain.es.md)), que ya deja `subscription.Price` actualizado por dentro **y** devuelve el `PriceHistory` recién construido. El controller solo tiene que añadir explícitamente ese `PriceHistory` al `DbSet` correspondiente (`dbContext.PriceHistories.Add(priceHistory)`) — la propia `subscription` **no** hace falta volver a añadirla ni marcarla, porque ya estaba siendo rastreada desde el `FindAsync` de arriba, y el *change tracker* de EF Core ya detectó que su `Price` cambió. Un único `SaveChangesAsync()` guarda las dos cosas a la vez, en la misma transacción implícita (si algo fallara, ninguna de las dos se guardaría).

Nótese que esta acción devuelve `Ok(...)` (`200`, con la suscripción actualizada en el cuerpo) en vez de `NoContent()` (`204`) como hace `Update` — es intencional: después de cambiar el precio, es útil para quien llama ver inmediatamente el nuevo estado completo de la suscripción, sin tener que hacer una segunda petición `GET`.

### `ChangeStatus(int id, ChangeStatusRequest request)` — `PUT /api/Subscriptions/{id}/status`
Mismo patrón que `ChangePrice`, pero llamando a `subscription.ChangeStatus(request.NewStatus)` — que, recordemos, lanza si el nuevo estado es igual al actual, acabando en `400`.

### `Delete(int id)` — `DELETE /api/Subscriptions/{id}`
Mismo patrón que `CategoriesController.Delete`, pero sin ningún motivo para fallar por restricción de clave foránea aquí — borrar una suscripción borra en cascada su `PriceHistory` (`OnDelete(DeleteBehavior.Cascade)`), así que siempre tiene éxito si la suscripción existe.

### `ToResponse` (privado)
Igual que en `CategoriesController`, un método privado que construye el DTO a partir de la entidad, reutilizado por varias acciones.

---

## `PriceHistoriesController.cs`

El más simple de los cuatro: **solo lectura**, sin `POST`/`PUT`/`DELETE` — los registros de historial nacen únicamente como efecto secundario de `SubscriptionsController.ChangePrice`, nunca directamente.

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<PriceHistoryResponse>>> Get([FromQuery] int? subscriptionId)
{
    var query = dbContext.PriceHistories.AsQueryable();

    if (subscriptionId is not null)
    {
        query = query.Where(priceHistory => priceHistory.SubscriptionId == subscriptionId);
    }

    var priceHistories = await query
        .Select(priceHistory => new PriceHistoryResponse(
            priceHistory.Id,
            priceHistory.SubscriptionId,
            priceHistory.OldPrice,
            priceHistory.NewPrice,
            priceHistory.ChangedAt))
        .ToListAsync();

    return Ok(priceHistories);
}
```
`[FromQuery] int? subscriptionId` — un filtro **opcional** (de ahí el `?`): si no se manda `?subscriptionId=5` en la URL, vale `null`. `dbContext.PriceHistories.AsQueryable()` empieza la consulta sin ningún filtro todavía (es casi un no-op — `DbSet<T>` ya es un `IQueryable<T>`, pero escribirlo así deja claro que `query` es una variable que se va a ir construyendo). Si `subscriptionId` viene informado, se le añade el `.Where(...)` correspondiente; si no, la consulta sigue sin filtrar, trayendo el historial completo de todas las suscripciones. Este patrón — construir la consulta **condicionalmente**, antes de ejecutarla — es lo que permite tener un único endpoint que sirve tanto "todo el historial" como "el historial de una suscripción", sin duplicar la consulta entera en un `if`/`else`.

`GetById` sigue exactamente el mismo patrón visto ya en los otros controllers.

---

## `ExpensesController.cs`

```csharp
[HttpGet("summary")]
public async Task<ActionResult<ExpensesSummaryResponse>> GetSummary()
{
    var activeSubscriptions = await dbContext.Subscriptions
        .Where(subscription => subscription.Status == SubscriptionStatus.Active)
        .Select(subscription => new { subscription.Price, subscription.BillingCycle })
        .ToListAsync();

    var yearlyTotal = activeSubscriptions.Sum(subscription => ToYearlyCost(subscription.Price, subscription.BillingCycle));

    return Ok(new ExpensesSummaryResponse(
        MonthlyTotal: Math.Round(yearlyTotal / 12, 2),
        YearlyTotal: Math.Round(yearlyTotal, 2),
        ActiveSubscriptionCount: activeSubscriptions.Count));
}

private static decimal ToYearlyCost(decimal price, BillingCycle billingCycle) => billingCycle switch
{
    BillingCycle.Day => price * 365,
    BillingCycle.Monthly => price * 12,
    BillingCycle.Yearly => price,
    _ => throw new ArgumentOutOfRangeException(nameof(billingCycle))
};
```
- `.Select(subscription => new { subscription.Price, subscription.BillingCycle })` — proyecta a un **tipo anónimo** con solo las dos columnas que hacen falta para el cálculo, en vez de traer la suscripción entera (ni siquiera como DTO) — no hace falta nada más para sumar.
- `.ToListAsync()` — aquí es donde la consulta se convierte en una lista real **en memoria**, en C#. A partir de esta línea, ya no es SQL.
- `activeSubscriptions.Sum(subscription => ToYearlyCost(...))` — la suma se hace **en memoria**, no con un `SUM()` de SQL, precisamente porque SQLite guarda los `decimal` como texto (ver [Concepts.md § SQLite](../Concepts/Concepts.es.md#-sqlite-por-qué-este-motor-de-base-de-datos)) y no podría sumarlos correctamente por sí solo. `ToYearlyCost` (un método privado, con un `switch` de expresión) convierte el precio de cada suscripción a su equivalente anual según su ciclo: diaria ×365, mensual ×12, anual tal cual. El `_ => throw ...` final es la rama por defecto del `switch` — cubre, en teoría, un valor de `BillingCycle` que no sea ninguno de los tres conocidos; en la práctica nunca debería ejecutarse, porque `[EnumDataType]` en los DTOs de entrada ya impide que llegue a guardarse un valor así, pero está ahí como red de seguridad explícita en vez de dejar que el `switch` devuelva silenciosamente algo incorrecto.
- `Math.Round(yearlyTotal / 12, 2)` / `Math.Round(yearlyTotal, 2)` — el total mensual se calcula **a partir** del anual (dividiendo entre 12), no sumando por separado los precios ya convertidos a "mensual" uno a uno — así, los dos números (`MonthlyTotal` y `YearlyTotal`) son matemáticamente coherentes entre sí por construcción, nunca pueden "descuadrarse" por redondeos independientes.
