# 🔬 Guía de Código — `Domain/`

[← Índice](./CodeGuide.es.md)

Las entidades de dominio son el único sitio del código con permiso para decidir si un dato es válido. Ninguna tiene setters públicos en sus propiedades (`private set` en todas) — la única forma de cambiar algo es a través de un método que, por dentro, aplica la regla correspondiente.

---

## `Domain/enum/BillingCycle.cs`

```csharp
public enum BillingCycle
{
    Day,
    Monthly,
    Yearly
}
```
Un enum normal de C#. Sin asignar valores explícitos, el compilador numera automáticamente desde `0`: `Day = 0`, `Monthly = 1`, `Yearly = 2`. Estos números son los que EF Core guarda en la columna `BillingCycle` de la tabla `Subscriptions` (ver [Database.es.md](../Database/Database.es.md#-referencia-de-enums)) — **no cambiar el orden** de los valores existentes sin pensar en la migración de datos, porque eso cambiaría el significado de los números ya guardados.

## `Domain/enum/SubscriptionStatus.cs`

```csharp
public enum SubscriptionStatus
{
    Active,
    Paused,
    Cancelled
}
```
Igual que el anterior: `Active = 0`, `Paused = 1`, `Cancelled = 2`.

---

## `Domain/Entities/Category.cs`

```csharp
public class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
```
Tres propiedades, todas con `get` público pero `set` **privado**: se pueden leer desde cualquier sitio (`category.Name`), pero solo se pueden asignar desde dentro de la propia clase `Category`. `Name` se inicializa a `string.Empty` como valor por defecto (relevante para el constructor privado de abajo, que no le asigna nada).

```csharp
    public Category(string name)
    {
        EnsureIsValid(name);

        Name = name;
        CreatedAt = DateTime.UtcNow;
    }
```
El **constructor público**, la única forma de crear una `Category` válida desde el código de la aplicación. Primero valida (si `EnsureIsValid` lanza, el resto del constructor nunca se ejecuta — la categoría nunca llega a "existir" en un estado inválido), luego asigna. `DateTime.UtcNow` captura el instante exacto, en UTC (no la hora local del servidor) — convención usada en todo el proyecto para marcas de tiempo, para que no dependan de en qué zona horaria esté corriendo el proceso.

```csharp
    public void Rename(string name)
    {
        EnsureIsValid(name);
        Name = name;
    }
```
La única forma de cambiar el nombre después de creada. Misma validación que el constructor — si alguna vez se añadiera una regla nueva para "qué es un nombre válido", solo hay que tocarla en `EnsureIsValid`, y automáticamente se aplica tanto al crear como al renombrar.

```csharp
    private static void EnsureIsValid(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }
```
Método `private static` — no forma parte de la API pública de la clase, es un detalle interno de implementación compartido entre el constructor y `Rename`. `ArgumentException.ThrowIfNullOrWhiteSpace` es un método estático de .NET (añadido en versiones recientes del framework) que lanza `ArgumentException` si el string es `null`, `""`, o solo espacios en blanco — ahorra escribir el `if` a mano. Esta excepción es la que `ArgumentExceptionHandler` convierte en una respuesta `400` (ver [Infrastructure.es.md](./Infrastructure.es.md)).

```csharp
    private Category()
    {
        // Required by EF Core.
    }
}
```
Un **segundo constructor**, sin parámetros y **privado**. Nunca lo llama el código de la aplicación — existe exclusivamente porque EF Core, al leer una fila de la base de datos y reconstruir el objeto `Category` correspondiente, necesita poder crear una instancia vacía y luego rellenar sus propiedades por reflexión (sin pasar por el constructor público ni, por tanto, por `EnsureIsValid`). Que sea `private` impide que el código de la aplicación lo llame por error — solo EF Core, que tiene acceso especial a miembros privados para esto, puede usarlo.

---

## `Domain/Entities/Subscription.cs`

La entidad más rica del dominio. Repasando cada miembro:

```csharp
public class Subscription
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public decimal Price { get; private set; }
    public BillingCycle BillingCycle { get; private set; }

    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
```
`EndDate` es `DateTime?` (anulable) — a diferencia de `StartDate`, puede no tener valor (una suscripción activa, sin fecha de fin todavía). `Category Category` es la **propiedad de navegación**: EF Core, cuando carga una suscripción con su categoría incluida, rellena este objeto entero, no solo `CategoryId`. El `= null!` es una promesa al compilador ("confía en mí, esto nunca será null cuando se use de verdad") — necesaria porque el proyecto tiene `<Nullable>enable</Nullable>` activado (el compilador avisaría de que una propiedad no anulable de tipo referencia debería inicializarse), pero en la práctica siempre se rellena, bien en el constructor público, bien por EF Core al cargar de la base de datos.

```csharp
    public Subscription(string name, string description, decimal price, BillingCycle billingCycle, DateTime startDate, Category category)
    {
        EnsureIsValid(name, price);

        Name = name;
        Description = description;
        Price = price;
        BillingCycle = billingCycle;
        StartDate = startDate;
        Category = category;
        CategoryId = category.Id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = SubscriptionStatus.Active;
    }
```
El constructor recibe el objeto `Category` entero (no solo su id) — así puede sacar `category.Id` él mismo (`CategoryId = category.Id`), en vez de que quien llama tenga que pasar el id por duplicado y arriesgarse a que no coincida con el objeto. Toda suscripción nueva nace `Active` — no hay forma de crear una ya pausada o cancelada; tiene sentido, porque algo que acabas de dar de alta, por definición, está activo.

```csharp
    public PriceHistory ChangePrice(decimal newPrice)
    {
        var priceHistory = new PriceHistory(this, Price, newPrice);

        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;

        return priceHistory;
    }
```
La única forma de cambiar `Price`. Importante el orden: primero construye el `PriceHistory` (pasándole `this` — la propia suscripción —, el precio **viejo** que todavía está en `Price`, y el **nuevo**), y solo después actualiza `Price`. Si `new PriceHistory(...)` lanzara (por ejemplo, porque `newPrice` es igual al actual — ver más abajo), `Price` nunca llegaría a cambiar: la operación es "todo o nada". El método **devuelve** el `PriceHistory` creado — no lo guarda él mismo en la base de datos (esta clase no sabe nada de persistencia); es responsabilidad de quien llama (el controller) añadirlo al `DbContext` y guardar.

```csharp
    public void ChangeStatus(SubscriptionStatus newStatus)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(newStatus, Status);

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
```
`ArgumentOutOfRangeException.ThrowIfEqual(newStatus, Status)` lanza si los dos valores son iguales — es decir, **rechaza explícitamente un cambio que no cambia nada** (pasar de `Active` a `Active`). Esto no es un detalle cosmético: evita que alguien, por error, piense que "reactivar" algo que ya estaba activo tiene sentido como operación — si el estado no cambia, no hay nada que hacer, y el código lo deja claro lanzando en vez de aceptarlo en silencio.

```csharp
    public DateTime? GetNextRenewalDate(DateTime asOf)
    {
        if (Status != SubscriptionStatus.Active)
        {
            return null;
        }

        var today = asOf.Date;
        var start = StartDate.Date;

        var next = start >= today
            ? start
            : BillingCycle switch
            {
                BillingCycle.Day => today,
                BillingCycle.Monthly => NextMonthlyOccurrence(start, today, monthsPerCycle: 1),
                BillingCycle.Yearly => NextMonthlyOccurrence(start, today, monthsPerCycle: 12),
                _ => throw new ArgumentOutOfRangeException(nameof(BillingCycle))
            };

        if (EndDate is not null && next > EndDate.Value.Date)
        {
            return null;
        }

        return next;

        static DateTime NextMonthlyOccurrence(DateTime start, DateTime today, int monthsPerCycle)
        {
            var cyclesElapsed = (((today.Year - start.Year) * 12) + today.Month - start.Month) / monthsPerCycle;
            var next = start.AddMonths(cyclesElapsed * monthsPerCycle);

            return next < today ? start.AddMonths((cyclesElapsed + 1) * monthsPerCycle) : next;
        }
    }
```
El método más delicado de todo el backend — calcula, dada una fecha de referencia (`asOf`, normalmente "hoy"), cuándo cae la próxima renovación. Desglosado:

- **Guarda inicial**: si no está `Active`, no hay próxima renovación — `null`. Una suscripción pausada o cancelada nunca va a cobrarse.
- **`asOf.Date` / `StartDate.Date`**: `.Date` descarta la parte de hora, dejando solo año/mes/día a medianoche — así la comparación es por **día de calendario**, sin que una suscripción con hora `14:30:00` y una comparación hecha a las `09:00:00` den resultados raros.
- **Si la fecha de inicio todavía no ha llegado** (`start >= today`), la próxima renovación es, lógicamente, la propia fecha de inicio — todavía no se ha cobrado nunca.
- **Si ya empezó**, el cálculo depende del ciclo:
  - `Day` → simplemente `today`: una suscripción diaria siempre "se renueva hoy" (se cobra todos los días).
  - `Monthly` / `Yearly` → delegan en la función local `NextMonthlyOccurrence`, con `monthsPerCycle` 1 o 12 respectivamente (un año son 12 meses, así que "cada año" se trata como "cada 12 meses" — reutilizando la misma lógica).
- **El tope de `EndDate`**: si la próxima renovación calculada cae **después** de la fecha de fin, se devuelve `null` en su lugar — no tiene sentido anunciar un cobro que nunca va a llegar a pasar porque la suscripción ya habrá terminado.

**La función local `NextMonthlyOccurrence`** es donde está el bug que se corrigió antes en este proyecto (suscripciones que empiezan el día 31, o el 29 de febrero). La clave:
- `cyclesElapsed` calcula, con aritmética de enteros, **cuántos ciclos completos** han pasado entre `start` y `today`, contando en meses: `(today.Year - start.Year) * 12 + today.Month - start.Month` da la diferencia total en meses entre las dos fechas (ignorando el día), y dividir por `monthsPerCycle` da cuántos ciclos completos de ese tamaño han pasado. La división de enteros en C# **trunca** (descarta el resto), que es exactamente lo que se quiere: "cuántos ciclos completos ya han empezado".
- `start.AddMonths(cyclesElapsed * monthsPerCycle)` calcula la fecha de ese ciclo, **siempre a partir de la fecha original `start`**, nunca sumando mes a mes de forma iterativa sobre un resultado anterior. Esto es la corrección clave: `AddMonths` de .NET, cuando el mes de destino tiene menos días que el de origen (p. ej. ir de 31 de enero a febrero), recorta automáticamente al último día de ese mes (28 o 29 de febrero). Si en vez de recalcular siempre desde `start` se fuera sumando sobre el resultado anterior (31 ene → 28 feb → 28 mar, en vez de 31 ene → 28 feb → 31 mar), el recorte de un mes corto "contaminaría" todos los meses siguientes, y la suscripción se quedaría clavada en el día 28 para siempre en vez de volver al 31 en cuanto el mes lo permitiera. Calcular siempre `cyclesElapsed * monthsPerCycle` meses por delante de la fecha **original** evita ese arrastre.
- La última línea comprueba si el resultado quedó **antes** de `today` (puede pasar por cómo trunca la división de enteros cuando el día de inicio es mayor que el día de hoy dentro del mismo mes) y, si es así, suma un ciclo más.

```csharp
    public void UpdateDetails(string name, string description, BillingCycle billingCycle, DateTime startDate, DateTime? endDate, Category category)
    {
        EnsureIsValid(name, Price);

        Name = name;
        Description = description;
        BillingCycle = billingCycle;
        StartDate = startDate;
        EndDate = endDate;
        Category = category;
        CategoryId = category.Id;
        UpdatedAt = DateTime.UtcNow;
    }
```
Actualiza todo **salvo el precio** (nótese que `EnsureIsValid(name, Price)` usa el `Price` **actual**, el que ya tenía la entidad — no recibe uno nuevo como parámetro). Esto es intencional: cambiar el precio tiene una regla de negocio propia (dejar rastro en `PriceHistory`), así que se hace solo a través de `ChangePrice`, nunca aquí — de ahí que este método ni siquiera acepte un parámetro `price`.

```csharp
    private static void EnsureIsValid(string name, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);
    }

    private Subscription()
    {
        // Required by EF Core.
    }
}
```
Mismo patrón que en `Category`: validación compartida, y constructor privado sin parámetros solo para que EF Core pueda materializar instancias desde filas de la base de datos.

---

## `Domain/Entities/PriceHistory.cs`

```csharp
public class PriceHistory
{
    public int Id { get; private set; }

    public int SubscriptionId { get; private set; }
    public Subscription Subscription { get; private set; } = null!;

    public decimal OldPrice { get; private set; }
    public decimal NewPrice { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public PriceHistory(Subscription subscription, decimal oldPrice, decimal newPrice)
    {
        EnsureIsValid(oldPrice, newPrice);

        Subscription = subscription;
        SubscriptionId = subscription.Id;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        ChangedAt = DateTime.UtcNow;
    }

    private static void EnsureIsValid(decimal oldPrice, decimal newPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(oldPrice);
        ArgumentOutOfRangeException.ThrowIfNegative(newPrice);
        ArgumentOutOfRangeException.ThrowIfEqual(newPrice, oldPrice);
    }

    private PriceHistory()
    {
        // Required by EF Core.
    }
}
```
Una entidad deliberadamente **inmutable**: no tiene ningún método para modificarla después de creada (ni `Update`, ni setters públicos) — un registro histórico no debería poder cambiarse una vez escrito, o deja de ser fiable como historial. Su único constructor (aparte del privado para EF Core) hace tres comprobaciones: ni el precio viejo ni el nuevo pueden ser negativos, y **no pueden ser iguales entre sí** — esta última es la que obliga a `Subscription.ChangePrice` a no poder "no hacer nada" en silencio: si alguien intenta cambiar el precio al mismo precio que ya tenía, aquí es donde explota con un `ArgumentOutOfRangeException`, que se traduce en un `400` para quien hizo la petición.
