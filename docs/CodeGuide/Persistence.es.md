# 🔬 Guía de Código — `Infrastructure/Persistence/` y `Migrations/`

[← Índice](./CodeGuide.es.md)

Todo lo relacionado con guardar y leer de SQLite.

---

## `SubscriptionManagerDbContext.cs`

```csharp
public class SubscriptionManagerDbContext(DbContextOptions<SubscriptionManagerDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubscriptionManagerDbContext).Assembly);
    }
}
```

- `public class SubscriptionManagerDbContext(DbContextOptions<SubscriptionManagerDbContext> options) : DbContext(options)` — esto es un **constructor primario** (característica reciente de C#): el parámetro `options` declarado junto al nombre de la clase se convierte automáticamente en el constructor de la clase, y además queda disponible como variable capturada en todo el cuerpo de la clase. `: DbContext(options)` pasa ese mismo valor al constructor de la clase base (`DbContext`, de EF Core), que es quien realmente sabe qué hacer con él (contiene, entre otras cosas, la cadena de conexión fijada en `Program.cs` vía `UseSqlite`).
- `public DbSet<Category> Categories => Set<Category>();` — una propiedad de solo lectura (cuerpo de expresión, `=>`) que, cada vez que se accede, llama a `Set<Category>()` (heredado de `DbContext`). `Set<T>()` devuelve el `DbSet<T>` correspondiente a esa entidad — el punto de entrada para consultarla (`dbContext.Categories.Where(...)`) o modificarla (`dbContext.Categories.Add(...)`). Se usan las tres igual, una por cada tabla.
- `OnModelCreating` es un método que EF Core llama **una vez**, la primera vez que necesita construir el modelo completo (qué tablas hay, qué columnas, qué relaciones) — no en cada petición. `modelBuilder.ApplyConfigurationsFromAssembly(...)` busca, por reflexión, **todas** las clases del ensamblado actual que implementen `IEntityTypeConfiguration<T>` (las tres de `Configurations/`, ver abajo) y las aplica automáticamente — así, añadir una entidad nueva con su configuración no obliga a tocar este fichero para "registrarla a mano".

---

## `Infrastructure/Persistence/Configurations/CategoryConfiguration.cs`

```csharp
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(100)
            .UseCollation("NOCASE");

        builder.HasIndex(category => category.Name)
            .IsUnique();
    }
}
```
Esto es **Fluent API**: en vez de poner atributos sobre las propiedades de `Category` (que la obligarían a "saber" que existe una base de datos), toda la configuración de persistencia vive aparte, en una clase cuyo único propósito es describir cómo se guarda `Category`.

- `builder.ToTable("Categories")` — el nombre de la tabla (por defecto, EF Core usaría el nombre de la propiedad `DbSet` o de la clase; aquí se fija explícitamente, aunque coincida).
- `builder.HasKey(category => category.Id)` — la clave primaria. La expresión lambda (`category => category.Id`) es la forma en que EF Core deja elegir **qué propiedad** sin usar strings mágicos — el compilador comprueba que `Id` existe de verdad en `Category`.
- `.HasMaxLength(100)` — la columna `Name` admite como máximo 100 caracteres; EF Core traduce esto al `maxLength` de la migración (ver abajo) y SQLite, aunque no impone un límite real a nivel de motor, sí lo documenta en el esquema.
- `.UseCollation("NOCASE")` — la pieza clave de la unicidad **sin distinguir mayúsculas**: `NOCASE` es una colación integrada en SQLite que compara texto ignorando mayúsculas/minúsculas (solo para caracteres ASCII). Sin esto, `"Streaming"` y `"streaming"` se considerarían nombres distintos.
- `builder.HasIndex(category => category.Name).IsUnique()` — crea un índice único sobre `Name`. Combinado con la colación `NOCASE` de arriba, el índice (y por tanto la comprobación de unicidad) también ignora mayúsculas — es lo que hace que SQLite, no solo el código C#, rechace un segundo `"streaming"` si ya existe un `"Streaming"`.

## `Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs`

```csharp
public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(subscription => subscription.Id);

        builder.Property(subscription => subscription.Name)
            .IsRequired()
            .HasMaxLength(200)
            .UseCollation("NOCASE");

        builder.HasIndex(subscription => new { subscription.Name, subscription.CategoryId })
            .IsUnique();

        builder.Property(subscription => subscription.Description)
            .HasMaxLength(1000);

        builder.Property(subscription => subscription.Price)
            .HasPrecision(10, 2);

        builder.HasOne(subscription => subscription.Category)
            .WithMany()
            .HasForeignKey(subscription => subscription.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```
Lo nuevo respecto a `CategoryConfiguration`:

- `builder.HasIndex(subscription => new { subscription.Name, subscription.CategoryId }).IsUnique()` — un **índice único compuesto**, sobre dos columnas a la vez (`new { ... }` crea un tipo anónimo solo para que EF Core sepa qué combinación de propiedades forman el índice). Esto es lo que permite que el mismo nombre de suscripción exista en dos categorías distintas, pero no dos veces dentro de la **misma** categoría — la combinación `(Name, CategoryId)` tiene que ser única, cada campo por separado no.
- `.HasPrecision(10, 2)` — para un `decimal`, fija explícitamente **precisión 10, escala 2** (hasta 10 cifras en total, de las cuales 2 son decimales — p. ej. `12345678.90` cabe, `123456789.00` no). Sin esto, EF Core con el proveedor de Sqlite usaría un valor por defecto que podría no ser el que se espera.
- `builder.HasOne(subscription => subscription.Category).WithMany().HasForeignKey(subscription => subscription.CategoryId).OnDelete(DeleteBehavior.Restrict)` — describe la relación completa: cada `Subscription` tiene **una** `Category` (`HasOne`), cada `Category` puede tener **muchas** suscripciones pero sin una propiedad de colección en el lado de `Category` que las liste (`WithMany()` sin argumento — `Category` no tiene ninguna propiedad `List<Subscription>`, a propósito, para no cargarla por accidente en cada consulta de categorías), la clave foránea es `CategoryId`, y `OnDelete(DeleteBehavior.Restrict)` dice qué hacer si se intenta borrar una categoría que todavía tiene suscripciones: **rechazarlo** a nivel de base de datos (SQLite devuelve un error de restricción de clave foránea, que `DbUpdateExceptionHandler` convierte en `409`) en vez de, por ejemplo, borrar en cascada las suscripciones o dejar `CategoryId` huérfano.

## `Infrastructure/Persistence/Configurations/PriceHistoryConfiguration.cs`

```csharp
public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("PriceHistories");

        builder.HasKey(priceHistory => priceHistory.Id);

        builder.Property(priceHistory => priceHistory.OldPrice)
            .HasPrecision(10, 2);

        builder.Property(priceHistory => priceHistory.NewPrice)
            .HasPrecision(10, 2);

        builder.HasOne(priceHistory => priceHistory.Subscription)
            .WithMany()
            .HasForeignKey(priceHistory => priceHistory.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```
Misma idea que `SubscriptionConfiguration`, con una diferencia importante al final: `OnDelete(DeleteBehavior.Cascade)` en vez de `Restrict`. Aquí **sí** tiene sentido borrar en cascada: un registro de historial de precios no significa nada sin la suscripción a la que pertenece, así que al borrar una suscripción, sus registros de `PriceHistory` se borran automáticamente con ella (al contrario que `Category` → `Subscription`, donde se prefiere bloquear el borrado antes que perder suscripciones "por accidente" al borrar su categoría).

---

## `Infrastructure/Persistence/DbUpdateExceptionExtensions.cs`

```csharp
public static class DbUpdateExceptionExtensions
{
    private const int SqliteConstraintViolation = 19; // SQLITE_CONSTRAINT

    public static bool IsConstraintViolation(this DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintViolation };
}
```
- `public static bool IsConstraintViolation(this DbUpdateException exception)` — el `this` antes del parámetro convierte esto en un **método de extensión**: a partir de aquí, puede llamarse como si fuera un método de instancia de `DbUpdateException` (`algunaExcepcion.IsConstraintViolation()`), aunque `DbUpdateException` sea una clase de EF Core que este proyecto no puede modificar.
- `exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintViolation }` — un **patrón de propiedad** de C#: comprueba a la vez que `InnerException` no es null, que es (o hereda de) `SqliteException`, **y** que su propiedad `SqliteErrorCode` vale exactamente `19`. Todo en una sola expresión, sin casts intermedios. El código `19` es el código de error oficial de SQLite para `SQLITE_CONSTRAINT` — "la operación viola una restricción" (clave única, clave foránea, etc.), documentado por el propio motor SQLite, no inventado por este proyecto.
- Por qué mirar `InnerException` y no la propia `DbUpdateException`: EF Core siempre envuelve el error real del proveedor de base de datos (aquí, SQLite) dentro de una `DbUpdateException` genérica — el código de error concreto vive en la excepción interna, no en la de EF Core.

## `Infrastructure/Persistence/SubscriptionManagerDbContextExtensions.cs`

```csharp
public static class SubscriptionManagerDbContextExtensions
{
    public static async Task<bool> TrySaveChangesAsync(this SubscriptionManagerDbContext dbContext)
    {
        try
        {
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception) when (exception.IsConstraintViolation())
        {
            return false;
        }
    }
}
```
Otro método de extensión, esta vez sobre el propio `DbContext`. Lo interesante es el `catch`:

- `catch (DbUpdateException exception) when (exception.IsConstraintViolation())` — un **filtro de excepción** (`when`): el bloque `catch` solo se ejecuta si, además de ser una `DbUpdateException`, la condición del `when` es verdadera (reutilizando exactamente el mismo método de extensión de arriba). Si es una `DbUpdateException` pero **no** es una violación de restricción (p. ej. un bloqueo transitorio), este `catch` **no la atrapa** — se propaga hacia arriba, sin tocar, hasta llegar (si nadie más la atrapa) al middleware global, donde `DbUpdateExceptionHandler` tampoco la reclamará, acabando en un `500` genuino.
- `return false;` dentro del `catch` — en vez de relanzar o devolver algo especial, simplemente informa "no se pudo guardar por un conflicto" con un booleano sencillo, que el controller que llama puede comprobar con un `if`.

**Por qué existe, si ya está `DbUpdateExceptionHandler` para lo mismo**: este helper cubre una **carrera** (*race condition*) que el manejador global, por sí solo, no evita de forma elegante. Ejemplo: dos peticiones llegan casi a la vez para crear una categoría llamada `"Streaming"`. Las dos comprueban primero "¿existe ya este nombre?" (`dbContext.Categories.AnyAsync(...)`) y las dos ven que no, todavía, porque ninguna ha guardado aún. Las dos construyen la entidad y llaman a guardar. Solo una de las dos consigue escribir primero; la segunda choca contra el índice único de SQLite en el momento de `SaveChangesAsync()`, no antes. Sin `TrySaveChangesAsync`, esa segunda petición lanzaría una excepción sin control hasta el manejador global, que **sí** la convertiría en `409` igualmente — pero el controller, al no capturarla él mismo, no tendría ocasión de responder con el mensaje específico ("ya existe una categoría llamada...") en el mismo sitio que ya usa para el caso normal (cuando la comprobación previa detecta el duplicado a tiempo). Con `TrySaveChangesAsync`, el controller trata los dos caminos — "lo detecté yo antes de guardar" y "lo detectó la base de datos al guardar, por una carrera" — con el mismo bloque `if (!await dbContext.TrySaveChangesAsync()) return Conflict(...)`, en vez de tener dos formas distintas de llegar a la misma respuesta.

---

## `DatabaseConnectionStringProvider.cs`

```csharp
public static class DatabaseConnectionStringProvider
{
    public static string Resolve(string? configuredConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubscriptionManager");

        Directory.CreateDirectory(appDataDirectory);

        var databaseFilePath = Path.Combine(appDataDirectory, "data.db");

        return $"Data Source={databaseFilePath}";
    }
}
```
- `string? configuredConnectionString` — el `?` marca el parámetro como anulable: puede venir `null` (si la clave de configuración ni existe) o una cadena vacía (como está hoy en `appsettings.json`).
- `if (!string.IsNullOrWhiteSpace(configuredConnectionString)) return configuredConnectionString;` — si hay algo configurado de verdad (ni vacío ni solo espacios), se respeta tal cual, sin tocarlo — esto es lo que permite que alguien ponga su propia ruta en `appsettings.json` o por variable de entorno y quede garantizado que se usa esa, sin ninguna lógica de por medio.
- `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` — pide al sistema operativo la ruta "oficial" para datos de aplicación del usuario actual. .NET traduce esto a la convención de cada sistema: `C:\Users\{usuario}\AppData\Roaming` en Windows, `~/Library/Application Support` en macOS (confirmado en vivo durante este mismo sub-paso), o `~/.config` en Linux.
- `Path.Combine(...)` — construye la ruta uniendo fragmentos, usando el separador de carpetas correcto para el sistema operativo actual (`\` en Windows, `/` en macOS/Linux) — nunca concatenar rutas con `+` a mano, por eso.
- `Directory.CreateDirectory(appDataDirectory)` — crea la carpeta `SubscriptionManager` dentro de esa ruta si no existe todavía (no lanza error si ya existe — es seguro llamarlo siempre, incondicionalmente).
- `return $"Data Source={databaseFilePath}";` — una cadena de conexión de SQLite es, en su forma más simple, literalmente eso: la palabra clave `Data Source` seguida de la ruta del fichero `.db`.

---

## `DatabaseMigrationService.cs`

```csharp
public static class DatabaseMigrationService
{
    private const int MaxBackupsToKeep = 10;

    public static void ApplyPendingMigrations(
        SubscriptionManagerDbContext context,
        string connectionString,
        TimeProvider timeProvider)
    {
        if (!context.Database.GetPendingMigrations().Any())
        {
            return;
        }

        var databaseFilePath = new SqliteConnectionStringBuilder(connectionString).DataSource;

        if (File.Exists(databaseFilePath))
        {
            BackUp(databaseFilePath, timeProvider);
        }

        context.Database.Migrate();
    }
```
- `context.Database.GetPendingMigrations()` — le pregunta a EF Core: de todas las migraciones que existen en el código (`Migrations/*.cs`), ¿cuáles **no** están todavía registradas como aplicadas en la tabla interna `__EFMigrationsHistory` de esta base de datos concreta? Devuelve una lista de nombres; `.Any()` solo comprueba si hay al menos una.
- Si no hay ninguna pendiente, `return;` inmediato — no hay nada que hacer, ni copia de seguridad ni migración.
- `new SqliteConnectionStringBuilder(connectionString).DataSource` — en vez de intentar "adivinar" la ruta del fichero recortando el string a mano (`connectionString.Replace("Data Source=", "")`, frágil ante mayúsculas, espacios, u otros parámetros en la cadena), se usa la clase oficial de parseo de cadenas de conexión de SQLite, que entiende el formato completo y expone `.DataSource` ya separado.
- `if (File.Exists(databaseFilePath))` — solo se hace copia de seguridad si el fichero **ya existe**. La primera vez que arranca la app (instalación nueva), no hay nada que respaldar — se migra directamente sobre una base de datos que todavía no tiene datos.
- `context.Database.Migrate()` — el método de EF Core que aplica, en orden, todas las migraciones pendientes (y ninguna más) a la base de datos real, creando además la tabla `__EFMigrationsHistory` si es la primera vez.

```csharp
    private static void BackUp(string databaseFilePath, TimeProvider timeProvider)
    {
        var backupDirectory = Path.Combine(Path.GetDirectoryName(databaseFilePath)!, "backups");
        Directory.CreateDirectory(backupDirectory);

        var baseName = Path.GetFileNameWithoutExtension(databaseFilePath);
        var extension = Path.GetExtension(databaseFilePath);
        var timestamp = timeProvider.GetUtcNow().ToString("yyyyMMddHHmmss");

        var backupFilePath = Path.Combine(backupDirectory, $"{baseName}-{timestamp}{extension}");
        File.Copy(databaseFilePath, backupFilePath, overwrite: true);

        PruneOldBackups(backupDirectory, baseName, extension);
    }
```
- `Path.GetDirectoryName(databaseFilePath)!` — obtiene la carpeta que contiene el fichero `.db` (todo menos el nombre del fichero); el `!` al final es un "confío en que esto no es null" para el compilador (no puede serlo en la práctica, porque `databaseFilePath` siempre viene de una ruta absoluta con carpeta).
- La carpeta de copias de seguridad se calcula como **hermana** del fichero real (`backups/` dentro de la misma carpeta que `data.db`) — así, si alguien configura una ruta de base de datos distinta a la de por defecto, las copias le siguen automáticamente, sin configuración aparte (la decisión que se tomó explícitamente contigo en este sub-paso).
- `Path.GetFileNameWithoutExtension` / `Path.GetExtension` — separan `"data.db"` en `"data"` y `".db"`, para poder reconstruir el nombre de la copia con el mismo patrón sea cual sea el nombre real del fichero (no asume que siempre se llama `"data"`).
- `timeProvider.GetUtcNow().ToString("yyyyMMddHHmmss")` — aquí es donde entra el `TimeProvider` inyectado: en vez de `DateTime.UtcNow` directo, se usa el que le hayan pasado como parámetro — en producción, el reloj real; en los tests, uno fijado a una fecha conocida, para poder comprobar el nombre exacto del fichero resultante. El formato `"yyyyMMddHHmmss"` da, por ejemplo, `20261121090000` — además de ser legible, al ser un número de ancho fijo, **ordenar alfabéticamente estos nombres de fichero equivale a ordenarlos cronológicamente**, lo cual se aprovecha más abajo.
- `File.Copy(databaseFilePath, backupFilePath, overwrite: true)` — copia byte a byte el fichero `.db` tal cual estaba, antes de tocarlo. `overwrite: true` evita un error si, por lo que sea, ya existiera un fichero con ese nombre exacto (en la práctica, solo pasaría si se hicieran dos copias en el mismo segundo).

```csharp
    private static void PruneOldBackups(string backupDirectory, string baseName, string extension)
    {
        var backupFiles = Directory.GetFiles(backupDirectory, $"{baseName}-*{extension}")
            .OrderByDescending(path => path, StringComparer.Ordinal)
            .Skip(MaxBackupsToKeep);

        foreach (var oldBackup in backupFiles)
        {
            File.Delete(oldBackup);
        }
    }
}
```
- `Directory.GetFiles(backupDirectory, $"{baseName}-*{extension}")` — lista solo los ficheros de esa carpeta que encajan con el patrón (p. ej. `"data-*.db"`), usando `*` como comodín — así no cuenta ningún otro fichero que, por lo que sea, hubiera en esa misma carpeta.
- `.OrderByDescending(path => path, StringComparer.Ordinal)` — los ordena como texto, de mayor a menor. Gracias a que la marca de tiempo tiene el formato de ancho fijo explicado arriba, ordenar como texto da exactamente el mismo resultado que ordenar por fecha real — del más reciente al más antiguo.
- `.Skip(MaxBackupsToKeep)` — se salta los primeros 10 (los más recientes, por el orden de arriba) y se queda con **el resto** — es decir, con los que hay que borrar.
- El `foreach` borra cada uno de esos. Resultado neto: nunca quedan más de 10 copias de seguridad, siempre las 10 más recientes.

---

## `Migrations/`

Tres ficheros por migración (`*.cs`, `*.Designer.cs`, y uno compartido `SubscriptionManagerDbContextModelSnapshot.cs`), todos **generados por `dotnet ef migrations add`** — no se editan a mano.

### `20261031181752_InitialCreate.cs`

El nombre del fichero empieza por una marca de tiempo (`20261031181752` = 31 de octubre de 2026, 18:17:52) seguida del nombre que se le dio al generarla — así quedan ordenadas cronológicamente solo por su nombre, igual que las copias de seguridad de arriba.

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "Categories",
        columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                .Annotation("Sqlite:Autoincrement", true),
            Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Categories", x => x.Id);
        });
    // ... Subscriptions y PriceHistories, con la misma forma
```
Cada migración tiene dos métodos: `Up()` (qué hacer para **aplicar** el cambio) y `Down()` (cómo **deshacerlo**, por si hiciera falta volver atrás). `Up()` de esta primera migración crea las tres tablas desde cero, con sus columnas, tipos, claves primarias (`Autoincrement` para que SQLite asigne el `Id` solo) y las dos claves foráneas con `ReferentialAction.Restrict` / `.Cascade` que ya se explicaron en las configuraciones de arriba — EF Core traduce la Fluent API directamente a estas instrucciones concretas.

`Down()` simplemente borra las tres tablas, en el orden inverso al que se crearon (primero las que dependen de otras, por las claves foráneas).

### `20261101113331_AddUniqueNameConstraints.cs`

La segunda migración, que **modifica** lo que ya existía en vez de crear algo nuevo:

```csharp
migrationBuilder.AlterColumn<string>(
    name: "Name",
    table: "Subscriptions",
    type: "TEXT",
    maxLength: 200,
    nullable: false,
    collation: "NOCASE",
    oldClrType: typeof(string),
    oldType: "TEXT",
    oldMaxLength: 200);
```
`AlterColumn` cambia una columna que ya existía — aquí, le añade la colación `NOCASE` que no tenía en la migración anterior. Los parámetros `old*` describen cómo era la columna **antes** del cambio — EF Core los necesita para poder generar correctamente el `Down()` (volver a como estaba) y, en otros motores de base de datos, para saber exactamente qué `ALTER` emitir; en SQLite en particular, cambiar la colación de una columna existente implica, por debajo, recrear la tabla entera, algo que EF Core gestiona de forma transparente.

Después, crea los dos índices únicos (`IX_Subscriptions_Name_CategoryId` e `IX_Categories_Name`) que ya se vieron en las configuraciones — esta migración es la que los añadió por primera vez; antes de ella, los nombres no tenían ninguna restricción de unicidad.

### `*.Designer.cs` y `SubscriptionManagerDbContextModelSnapshot.cs`

Los ficheros `*.Designer.cs` acompañan a cada migración y contienen metadatos que EF Core usa internamente (a qué `DbContext` pertenece la migración, el modelo completo en ese punto concreto del tiempo) — prácticamente nunca hace falta mirarlos.

`SubscriptionManagerDbContextModelSnapshot.cs` es distinto: hay **uno solo** para todo el proyecto (no uno por migración), y representa el modelo completo tal y como queda **después de aplicar todas las migraciones existentes hasta ahora**. Cuando se ejecuta `dotnet ef migrations add NombreNuevo`, EF Core compara el modelo actual de las clases C# (`Domain/Entities/` + `Configurations/`) contra esta foto, calcula la diferencia, la escribe como la migración nueva, y actualiza esta foto para que vuelva a reflejar el estado resultante. Por eso, al añadir una migración, **siempre** se actualizan dos cosas a la vez: el fichero de la migración nueva, y este snapshot.
