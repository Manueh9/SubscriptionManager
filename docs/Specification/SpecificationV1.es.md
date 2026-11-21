# Subscription Manager · Especificación de la V1 para Claude Code

> Documento de referencia principal para la reescritura a V1 (de gestor de suscripciones a gestor de todos los gastos periódicos del hogar). Pegado tal cual lo entregó Manuel el 2026-11-21, para que no viva solo en el historial de la conversación. Si algo del código actual, del prototipo o de otro documento lo contradice, **manda este documento**. Ver [Roadmap](../Roadmap/Roadmap.es.md) para el estado de cada fase y [Funcionalidades V1 (resumen)](../Features/FeaturesV1.es.md) para la lista corta de capacidades.
>
> Nota de sincronización: el fichero que este documento llama `subscription-manager-dashboard-spec.md` es, en este repositorio, [`docs/Design/Dashboard.es.md`](../Design/Dashboard.es.md) — mismo contenido, otro nombre. Se trata como el mismo documento.

Este documento describe todo lo que tiene que hacer la primera versión (V1) de Subscription Manager. Va dirigido a Claude Code y es la referencia principal: si algo del código actual, del prototipo o de otro documento lo contradice, manda este documento.

## 0. Cómo trabajar con este documento

1. Lee el documento entero antes de tocar nada.
2. Revisa el código actual del backend (entidades, `DbContext`, endpoints, tests, migraciones) y compáralo con el apartado 3. Si algo no coincide, adapta el plan al código real; no supongas nombres de ficheros.
3. Trabaja por fases (apartado 13), en orden. Cada fase acaba con los tests en verde y sus criterios de aceptación cumplidos.
4. **No añadas funciones que no estén aquí.** La lista de lo que queda fuera (apartado 2.2) es tan importante como la de lo que entra. Si crees que falta algo imprescindible, propónselo a Manuel antes de hacerlo.
5. Si algo es ambiguo y bloquea, pregunta. Si no bloquea, toma la opción más simple, déjala anotada con un comentario `// DECISION:` y sigue.

Documentos y material de apoyo:

- **Prototipo navegable** (lienzo de diseño "Subscription Manager — Primera versión"): https://claude.ai/artifact/Sk983Kdo43VJ4rmQFxJDAd. Siete pantallas: `V1Bienvenida`, `V1Importar`, `V1TuFoto`, `V1Resumen`, `V1Detalle`, `V1Prevision`, `V1NuevoGasto`. Manuel te pasará el artefacto.
- **`subscription-manager-dashboard-spec.md`**: especificación visual detallada del Dashboard (tokens, tipografía, tarjetas, animaciones). Sigue valiendo para el aspecto visual, con los cambios del apartado 12.3.
- **Ficheros de licencia** (te los pasa Manuel): `LICENSE` (texto oficial de la AGPL-3.0), `CLA.md` (acuerdo de contribución) y `CONTRIBUTING.md`. Van en la raíz del repositorio tal cual (apartado 16).
- Los importes y nombres del prototipo son datos de ejemplo. Donde el prototipo y este documento den cifras distintas, manda este documento.

## 1. Contexto del producto

### 1.1 Qué es

Subscription Manager empezó como gestor de suscripciones y en la V1 pasa a cubrir **todos los gastos periódicos de un hogar**: suscripciones, luz, agua, gas, fibra, seguros, impuestos (IBI), comunidad, cuotas (gimnasio, colegio), préstamos y otros.

El usuario la usa para:

- ver cuánto paga al mes y al año en gastos fijos;
- saber qué se le va a cobrar pronto y cuánto apartar;
- no pasarse de la **fecha límite para darse de baja** de algo que se renueva solo;
- detectar subidas de precio.

### 1.2 Principios (no negociables)

- **Local primero.** Todo corre en el ordenador del usuario: backend .NET y base de datos SQLite. Sin nube, sin cuentas y sin login.
- **Sin red.** La app no hace ninguna llamada saliente: nada de telemetría, ni descarga de logos, ni APIs externas. El catálogo va dentro de la app.
- **Privacidad.** Los ficheros del banco se procesan en memoria y no se guardan. No se registra en logs el contenido de movimientos, conceptos ni importes.
- **Honestidad.** La interfaz no promete nada que la app no haga. **Sin textos legales en la V1:** nada de citar artículos de leyes ni decir "hay que avisar por escrito". La fecha límite se presenta como un cálculo a partir del preaviso que indica el usuario.
- **España primero.** Interfaz en español, euros, formato de fecha y número español.
- **Código abierto.** Licencia **AGPL-3.0-or-later**, con acuerdo de contribución (CLA) para poder ofrecer también licencias comerciales. Detalles en el apartado 16.

### 1.3 Stack

- **Backend (ya existe):** .NET con SQLite. Comprueba en el código si usa EF Core y Minimal API o controladores, y sigue lo que haya.
- **Frontend (aún no empezado):** React + TypeScript + Vite + Tailwind CSS + Framer Motion.
- **Idioma del código:** identificadores y comentarios en inglés. Textos de cara al usuario en español, incluidos los mensajes de validación que devuelve la API.

## 2. Alcance

### 2.1 Lo que entra en la V1 (13 puntos)

1. **Gasto periódico con tipo.** Cualquier gasto que se repite, con tipo y estado.
2. **Calendario flexible.** Cada N días, semanas, meses o años, más "solo en algunos meses".
3. **Importes y registro de cobros.** Importe fijo, variable o aproximado. Cada cobro real queda guardado.
4. **Datos del contrato (opcionales).** Permanencia, precio promocional, prueba gratuita y preaviso.
5. **Fecha límite de baja.** Fecha de renovación menos preaviso.
6. **Subida de precio desde el historial.**
7. **Cálculos del resumen.** Totales, gasto por tipo, próximos 30 días, hucha y lo que vence pronto.
8. **Cálculos de la previsión.** Próximos 90 días, totales por mes, nómina y pagas, y lo que deberías llevar apartado.
9. **Importar extracto del banco.** Excel (.xlsx y .xls) o CSV tal como lo descarga el banco, con detección de cobros que se repiten.
10. **Catálogo de servicios.** Unos 30 servicios típicos en España, embebido.
11. **Calendario ICS.** Archivo con cobros, fechas límite y avisos.
12. **Exportar e importar.** Copia de seguridad propia en JSON, más importar desde Wallos.
13. **Datos de ejemplo.** Modo demo.

### 2.2 Lo que queda fuera de la V1 (no implementar)

- Cartas de baja o de reclamación, y cualquier plantilla legal.
- Registrar si la aseguradora avisó del nuevo precio (regla de los dos meses) y cualquier texto legal.
- Comparación con el IPC.
- Formatos preparados para bancos concretos. El importador es genérico.
- Asistente de preguntas sobre la casa.
- Documentos adjuntos (pólizas, contratos).
- Varias cuentas bancarias.
- **Categorías libres.** En la V1 solo existe el tipo de gasto.
- Calendario que se sincronice solo (suscripción por URL). El ICS es un archivo que se vuelve a generar.
- Saltar un cobro concreto.
- Hogar compartido, varias monedas, sincronización entre dispositivos, app móvil, notificaciones del sistema operativo.
- Instalador y empaquetado de escritorio. Va en otra fase, fuera de este documento.

## 3. Estado actual del backend (punto de partida)

Según la especificación original, el backend ya construido tiene esto. Verifícalo en el código:

- **`Category`**: `Name` (texto libre), `CreatedAt`.
- **`Subscription`**:
  - `Name`, `Description`, `Price` (decimal);
  - `BillingCycle`: enum `Day` / `Monthly` / `Yearly`;
  - `StartDate`, `EndDate` (opcional);
  - `Status`: enum `Active` / `Paused` / `Cancelled`;
  - pertenece a una `Category`.
- **`PriceHistory`**: registro inmutable por cada cambio de precio, con `OldPrice`, `NewPrice` y `ChangedAt`.
- **Endpoints existentes**:
  - resumen de gastos: total mensual, total anual y número de activas;
  - próximas renovaciones en los próximos N días.

> Verificado el 2026-11-21 contra el código real (3 exploraciones en paralelo): coincide exactamente, sin discrepancias.

## 4. Modelo de datos objetivo

### 4.1 Decisiones de modelo

- `Subscription` se renombra a **`RecurringExpense`** (gasto periódico). El nombre de la app no cambia.
- `Category` **se elimina**. La sustituye el enum `ExpenseKind`.
- `BillingCycle` se sustituye por **intervalo + unidad** (`IntervalCount` + `IntervalUnit`).
- `Price` pasa a **`Amount`**, con modo de importe (`AmountMode`).
- Se añaden **`ChargeRecord`** (cobros reales), **`ImportProfile`** (columnas recordadas del importador) y **`AppSettings`** (ajustes globales).
- `PriceHistory` se mantiene; su clave foránea pasa a `RecurringExpenseId`.
- Moneda: siempre euros. No se guarda campo de moneda.
- Fechas de calendario: `DateOnly`. Marcas de tiempo (`CreatedAt`, `UpdatedAt`): `DateTime` en UTC.
- Dinero: `decimal`. SQLite guarda los `decimal` de EF Core como texto y no los suma ni ordena bien en SQL, así que **todos los cálculos se hacen en memoria** (son pocos registros).

### 4.2 Enums

| Enum | Valores (código) | Etiqueta en la interfaz |
|---|---|---|
| `ExpenseKind` | `Subscription`, `Utility`, `Insurance`, `Tax`, `Fee`, `Loan`, `Other` | Suscripción, Suministro, Seguro, Impuesto, Cuota, Préstamo, Otro |
| `ExpenseStatus` | `Active`, `Paused`, `Cancelled` | Activo, Pausado, Dado de baja |
| `IntervalUnit` | `Day`, `Week`, `Month`, `Year` | días, semanas, meses, años |
| `AmountMode` | `Fixed`, `Variable`, `Approximate` | Fijo, Variable, Aproximado |
| `NoticeUnit` | `Day`, `Week`, `Month` | días, semanas, meses |
| `ChargeSource` | `Manual`, `BankImport`, `WallosImport`, `Demo` | (interno) |

Los enums se guardan como texto en la base de datos y se serializan como texto en JSON (`"Insurance"`, no `2`).

### 4.3 `RecurringExpense`

| Campo | Tipo | Obligatorio | Reglas |
|---|---|---|---|
| `Id` | int | sí | |
| `Name` | string(100) | sí | No vacío tras recortar espacios |
| `Description` | string(500) | no | Notas libres |
| `Kind` | `ExpenseKind` | sí | |
| `Status` | `ExpenseStatus` | sí | Por defecto `Active` |
| `IntervalCount` | int | sí | 1–365. Por defecto 1 |
| `IntervalUnit` | `IntervalUnit` | sí | Por defecto `Month` |
| `AnchorDate` | DateOnly? | no* | Una fecha en la que hay (o habrá) un cobro. A partir de ella se calculan todos los cobros |
| `ActiveMonths` | int? | no | Máscara de bits de 12: bit 0 = enero … bit 11 = diciembre. `null` = todos los meses. Solo con unidad `Day`, `Week` o `Month`; al menos un mes marcado |
| `StartDate` | DateOnly? | no | Antes de esta fecha no hay cobros |
| `EndDate` | DateOnly? | no | Después de esta fecha no hay cobros. Si hay `StartDate`, debe ser ≥ |
| `AmountMode` | `AmountMode` | sí | Por defecto `Fixed` |
| `Amount` | decimal? | no* | ≥ 0. En `Fixed`, el importe de cada cobro. En `Variable`, la estimación inicial mientras no haya cobros registrados. En `Approximate` no se usa |
| `AmountMin` | decimal? | no* | Solo `Approximate`. ≥ 0 |
| `AmountMax` | decimal? | no* | Solo `Approximate`. ≥ `AmountMin` |
| `CommitmentEndDate` | DateOnly? | no | Fin de permanencia |
| `EarlyCancellationFee` | decimal? | no | Penalización por salir antes. ≥ 0 |
| `PromoEndDate` | DateOnly? | no | Fin del precio promocional |
| `PriceAfterPromo` | decimal? | no | Precio por cobro tras la promoción, si se sabe. ≥ 0 |
| `TrialEndDate` | DateOnly? | no | Fin de la prueba gratuita |
| `NoticeAmount` | int? | no | Preaviso para darse de baja. 1–365. Va con `NoticeUnit` (los dos o ninguno) |
| `NoticeUnit` | `NoticeUnit`? | no | |
| `RemindBeforeCharge` | bool | sí | Aviso en el ICS antes de cada cobro |
| `RemindBeforeDeadline` | bool | sí | Aviso en el ICS antes de la fecha límite y de los fines de promoción, prueba y permanencia. Por defecto `true` |
| `AccentColor` | string(7) | sí | `#RRGGBB`. Por defecto según el tipo (apartado 4.9) |
| `Icon` | string(20) | sí | Clave de icono genérico (apartado 4.9) |
| `CatalogId` | string(50)? | no | Id del elemento del catálogo del que salió, si salió de uno |
| `IsDemo` | bool | sí | `true` en los datos de ejemplo |
| `CreatedAt`, `UpdatedAt` | DateTime (UTC) | sí | |

*Un gasto es **incompleto** si le falta `AnchorDate`, si es `Fixed` o `Variable` sin `Amount`, o si es `Approximate` sin `AmountMin` o `AmountMax`. Los incompletos se pueden guardar, por ejemplo los anuales sugeridos por el importador sin importe, pero **no cuentan en ningún total, previsión ni calendario** hasta que se completan. La API los marca con `isIncomplete: true` y la lista `missingFields`.

`RemindBeforeCharge` se pone por defecto a `true` si el gasto tiene 6 cobros al año o menos (apartado 5.3) y a `false` en los demás. Este valor se fija al crear el gasto y el usuario puede cambiarlo.

**Decidido con Manuel (2026-11-21): `Name` no tiene ninguna restricción de unicidad.** Se permite más de un `RecurringExpense` con el mismo nombre (p. ej. "Netflix" personal y "Netflix" familiar) — no hay índice único equivalente al `(Name, CategoryId)` de hoy.

### 4.4 `ChargeRecord` (cobro real)

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | int | |
| `RecurringExpenseId` | int | Borrado en cascada con el gasto |
| `Date` | DateOnly | |
| `Amount` | decimal | > 0 |
| `Source` | `ChargeSource` | |
| `BankConcept` | string(200)? | Concepto original del extracto, si viene de importación |
| `DedupKey` | string(64)? | Solo importaciones. Índice único (`RecurringExpenseId`, `DedupKey`) |
| `IsDemo` | bool | |
| `CreatedAt` | DateTime (UTC) | |

Los cobros solo existen si se registran, a mano o importados. **La app no inventa cobros pasados.**

### 4.5 `PriceHistory` (existente, se mantiene)

- `Id`, `RecurringExpenseId`, `OldPrice`, `NewPrice`, `ChangedAt`, más `IsDemo`.
- Se crea un registro cada vez que cambia `Amount` en un gasto `Fixed`, como hasta ahora. También cuando el usuario acepta un cambio de precio detectado al importar (apartado 9.8); en ese caso `ChangedAt` es la fecha del cobro.

### 4.6 `ImportProfile` (columnas recordadas)

- `Id`;
- `HeaderSignature` string(64), único: SHA-256 en hexadecimal de las celdas de cabecera normalizadas (apartado 9.4) unidas con `|`;
- `DateColumn` int, `ConceptColumn` int;
- columnas de importe, todas `int?`: `AmountColumn`, o `DebitColumn` + `CreditColumn`, y opcionalmente `SignColumn`;
- `DateFormat` string?, `DecimalSeparator` char?;
- `CreatedAt`, `LastUsedAt`.

### 4.7 `AppSettings` (una sola fila, `Id = 1`)

| Campo | Tipo | Por defecto | Notas |
|---|---|---|---|
| `PaydayDay` | int? | null | Día de cobro de la nómina, 1–31. Si es mayor que los días del mes, se usa el último día. `null` desactiva lo relacionado con la nómina |
| `PaymentsPerYear` | int | 12 | 12 o 14 |
| `ExtraPayMonths` | int | junio + diciembre | Máscara de bits como `ActiveMonths`. Solo con 14 pagas; debe tener exactamente 2 meses |
| `ReminderDaysBefore` | int | 7 | Días de antelación de los avisos del ICS, 1–30 |
| `ReminderHour` | int | 9 | Hora local del aviso, 0–23 |
| `IsDemo` | bool | false | Modo demo activo |

No se guarda el importe de la nómina: no hace falta y es un dato sensible.

### 4.8 Migración desde el modelo actual

Una sola migración para los puntos 1 a 4, en este orden:

1. **Antes de migrar**, copia el fichero `.db` a una carpeta `backups/` junto a la base de datos, con marca de tiempo en el nombre. Hazlo siempre que haya migraciones pendientes al arrancar, no solo en esta. Conserva las 10 copias más recientes.
2. Renombra la tabla de `Subscription` a `RecurringExpense` y la columna `Price` a `Amount`.
3. Añade las columnas nuevas.
4. Convierte los datos:
   - `BillingCycle`: `Day` → 1 `Day`, `Monthly` → 1 `Month`, `Yearly` → 1 `Year`;
   - `AmountMode = Fixed`, `Kind = Subscription`, `AnchorDate = StartDate`;
   - `AccentColor` e `Icon` con los valores por defecto del tipo;
   - `RemindBeforeCharge` según la regla del apartado 4.3, y `RemindBeforeDeadline = true`;
   - si el gasto tenía categoría, añade al final de `Description` el texto `Categoría anterior: {nombre}`, para no perder el dato.
5. Elimina `BillingCycle`, la clave foránea a `Category` y la tabla `Category`.
6. Crea `ChargeRecord`, `ImportProfile` y `AppSettings`, e inserta la fila por defecto de ajustes.
7. Renombra la clave foránea de `PriceHistory`.

Si el proyecto no usa migraciones de EF Core, aplica el mismo cambio con el mecanismo que use.

### 4.9 Valores visuales por defecto

**Color e icono por tipo**, cuando el gasto no sale del catálogo:

| Tipo | Color | Icono |
|---|---|---|
| Suscripción | `#8C9BB4` | `play` |
| Suministro | `#F5B437` | `bolt` |
| Seguro | `#50BEAA` | `shield` |
| Impuesto | `#B4B9C8` | `landmark` |
| Cuota | `#E65A82` | `receipt` |
| Préstamo | `#9678DC` | `bank` |
| Otro | `#8F99AA` | `receipt` |

**Claves de icono válidas:** `play`, `music`, `cloud`, `box`, `terminal`, `code`, `book`, `bolt`, `flame`, `drop`, `wifi`, `phone`, `home`, `car`, `shield`, `heart`, `paw`, `building`, `landmark`, `gym`, `school`, `bank`, `key`, `receipt`.

Son iconos genéricos de trazo, **nunca logotipos de marcas**. Valida la clave en la API.

## 5. Motor de calendario y cálculos

Toda la lógica de este apartado vive en el backend, en un servicio sin dependencias de base de datos y con tests unitarios. La API devuelve los valores ya calculados; el frontend no repite estas reglas.

**Fecha de hoy:** usa `TimeProvider` inyectado y la fecha local del equipo. En los tests, `FakeTimeProvider` (paquete `Microsoft.Extensions.TimeProvider.Testing`).

**Redondeo:** calcula siempre sin redondear y redondea solo al devolver importes por la API, a 2 decimales con `MidpointRounding.AwayFromZero`. Ojo: `Math.Round` redondea por defecto al par, y con eso 146,325 daría 146,32 en vez de 146,33.

### 5.1 Fechas de cobro (ocurrencias)

Los cobros de un gasto se calculan desde `AnchorDate`:

- `Day`: `AnchorDate + k × N días`
- `Week`: `AnchorDate + k × 7N días`
- `Month`: `AnchorDate.AddMonths(k × N)`
- `Year`: `AnchorDate.AddYears(k × N)`

Aquí `k` es cualquier entero, positivo o negativo, y `N = IntervalCount`.

**Calcula siempre desde la fecha de referencia** (`AnchorDate`) y nunca sumando sobre el cobro anterior. Así no se arrastra el ajuste de fin de mes. Por ejemplo, con un cobro mensual que empieza el 31 de enero de 2027, los siguientes son el 28 de febrero, el 31 de marzo y el 30 de abril (no 28, 28, 28). `AddMonths` de .NET ya ajusta al último día del mes. Con un cobro anual el 29 de febrero de 2028, el siguiente es el 28 de febrero de 2029 y se vuelve al 29 en 2032.

Se descartan los cobros que:

- caen en un mes desactivado en `ActiveMonths` (se descartan, no se mueven);
- son anteriores a `StartDate` o posteriores a `EndDate`;
- son anteriores a `TrialEndDate`. El cobro que cae justo en `TrialEndDate` sí se cobra.

Los gastos `Paused`, `Cancelled` o incompletos no tienen cobros previstos.

Para el **anillo de días** de la tarjeta se usan el cobro anterior y el siguiente de la serie, aplicando solo el filtro de meses activos:

- `nextChargeDate`: el primer cobro con fecha ≥ hoy.
- `previousChargeDate`: el último cobro con fecha < hoy.
- `daysUntilNextCharge = nextChargeDate − hoy`.
- `cycleDays = nextChargeDate − previousChargeDate`.
- `cycleProgress = max(0,04; 1 − daysUntilNextCharge / cycleDays)`.

### 5.2 Importe estimado de cada cobro

Para un cobro en la fecha `d`:

1. Si hay `PromoEndDate`, `d ≥ PromoEndDate` y hay `PriceAfterPromo`, el importe es `PriceAfterPromo`.
2. Si no, depende del modo:
   - `Fixed`: `Amount`.
   - `Variable`: la media de los **3 últimos** `ChargeRecord` del gasto, o de los que haya si son menos. Si no hay ninguno, `Amount`.
   - `Approximate`: `(AmountMin + AmountMax) / 2`.

El cobro se marca como estimado (`isEstimate: true`) si el modo es `Variable` o `Approximate`. La interfaz pone `≈` delante de los estimados.

### 5.3 Coste anual y mensual equivalente

Es el coste **al precio actual**, sin tener en cuenta promociones futuras ni fechas de fin.

- `chargesPerYear`:
  - `Day`: 365 / N
  - `Week`: 52 / N
  - `Month`: 12 / N
  - `Year`: 1 / N
- Si hay `ActiveMonths` y la unidad no es `Year`, se multiplica por `mesesActivos / 12`.
- `currentChargeAmount`: el importe estimado (apartado 5.2) de un cobro con fecha de hoy. Si la promoción ya terminó, se usa el precio tras la promoción.
- `annualCost = currentChargeAmount × chargesPerYear`
- `monthlyEquivalent = annualCost / 12`

Cuentan 0 (y se excluyen de los totales) los gastos no activos, los incompletos y los que **están en prueba gratuita** (`TrialEndDate > hoy`): todavía no se pagan y el usuario puede cancelarlos. Los cobros previstos después de la prueba sí aparecen en la previsión.

Ejemplos:

- Extraescolares, 60 € al mes de septiembre a junio: 12 × 10/12 = 10 cobros, 600 € al año, 50 € al mes.
- IBI en dos plazos de 198 € (mensual, con meses activos junio y noviembre): 2 cobros, 396 € al año.

### 5.4 Fecha límite de baja

Solo existe si el gasto tiene preaviso (`NoticeAmount` + `NoticeUnit`) y es activo y completo.

1. Recorre los cobros futuros `o` (≥ hoy) en orden.
2. Para cada uno calcula `limite = o − preaviso`:
   - `Day`: `o.AddDays(−n)`
   - `Week`: `o.AddDays(−7n)`
   - `Month`: `o.AddMonths(−n)`
3. La fecha límite es el primer `limite` que sea ≥ hoy.

Devuelve `cancellationDeadline: { date, daysLeft, renewalDate }`, donde `renewalDate` es el cobro `o` correspondiente.

Ejemplos:

- Seguro anual que se renueva el 15 de enero de 2027, preaviso de 1 mes, hoy 20 de noviembre de 2026: límite el 15 de diciembre de 2026, faltan 25 días.
- Si hoy fuera el 20 de diciembre, ya habría pasado. El siguiente límite sería el 15 de diciembre de 2027, para la renovación de 2028.

La permanencia no genera fecha límite. Su fin aparece como evento (apartado 5.5).

### 5.5 Lo que vence pronto (eventos)

Lista de eventos con fecha entre hoy y hoy + 90 días, inclusive, ordenada por fecha y después por nombre del gasto:

| Tipo | Fecha | Datos para la interfaz |
|---|---|---|
| `CancellationDeadline` | fecha límite (apartado 5.4) | `renewalDate`, preaviso |
| `PromoEnd` | `PromoEndDate` | importe actual por cobro y `PriceAfterPromo` (puede ser null) |
| `TrialEnd` | `TrialEndDate` | importe del primer cobro tras la prueba |
| `CommitmentEnd` | `CommitmentEndDate` | `EarlyCancellationFee` |

Cada evento lleva `expenseId`, `expenseName`, `kind`, `accentColor`, `type`, `date`, `daysLeft` y los datos de la tabla.

Textos que mostrará la interfaz (los compone el frontend):

- "Seguro de hogar · fecha límite de baja", con detalle "Preaviso de 1 mes".
- "Fibra y móvil · fin de la promoción", con detalle "Pasa de 45 € a 55 € al mes".
- "Audible · fin de la prueba gratuita", con detalle "Después: 9,99 € al mes".
- "… · fin de la permanencia", con detalle "Sin penalización desde hoy".

### 5.6 Subida de precio

Solo para gastos `Fixed`:

1. **Con cobros registrados:** toma el último cobro y el cobro más reciente con fecha al menos 11 meses anterior. Si existen los dos y el importe es distinto: `priceChange = { from, to, percent, fromDate, toDate }`, con `percent = (to − from) / from × 100` redondeado a 2 decimales.
2. **Sin dos cobros válidos:** usa `PriceHistory`. Toma el último cambio de los últimos 12 meses, con `from = OldPrice`, `to = NewPrice` y fechas `ChangedAt`.
3. Si no hay ninguna de las dos cosas, `priceChange = null`.

Además:

- `totalPaid` es la suma de todos los `ChargeRecord` del gasto.
- `priceHistorySeries` es la lista de cobros (fecha e importe) para el gráfico del detalle. Si no hay cobros, se usan los puntos de `PriceHistory`.

Ejemplo, el seguro de hogar del juego de datos de prueba. Con cobros de 241,80 € (2023), 249,00 € (2024), 262,40 € (2025) y 286,00 € (2026):

- `priceChange`: de 262,40 € a 286,00 €, `percent = 8,99`. La interfaz muestra "+9,0 %".
- `totalPaid` = 1.039,20 €.

### 5.7 Resumen

Sobre los gastos activos y completos que no están en prueba:

- `annualTotal` y `monthlyTotal`: suma de `annualCost` y de `monthlyEquivalent`.
- `activeCount`: número de gastos que cuentan. Aparte se devuelven `incompleteCount`, `trialCount` y `inactiveCount` (pausados más dados de baja).
- `next30Days`: cobros previstos entre hoy y hoy + 30 días, inclusive. Devuelve `total`, `count` y la fecha `until`. Sí incluye los cobros posteriores al fin de una prueba que caigan en la ventana.
- `monthlyToSetAside` (la hucha): suma de `monthlyEquivalent` de los gastos con `chargesPerYear ≤ 6`, es decir, los que se cobran de media cada dos meses o menos a menudo. Ejemplos: agua bimestral, comunidad trimestral, IBI en plazos, seguros y anuales.
- `byKind`: por cada tipo con gasto, `annual`, `monthly` y la lista de sus gastos (`id`, `name`, `annual`, `monthly`, `accentColor`), ordenados de mayor a menor. Es lo que alimenta el gráfico de dos anillos.
- `upcomingEvents`: la lista del apartado 5.5.

### 5.8 Previsión y nómina

**Ventana:** de hoy a hoy + 90 días, inclusive.

- `charges`: cada cobro previsto con `date`, `expenseId`, `name`, `kind`, `amount`, `isEstimate` y `accentColor`.
- `months`: un bloque por cada mes natural que toque la ventana, con:
  - `year`, `month`, `from` y `to` (recortados a la ventana);
  - `isPartial`, `total` y `count`;
  - `isMostExpensive`: `true` en el mes de mayor total, solo uno; si hay empate, el primero.

**Nómina** (solo si `PaydayDay` no es null):

- `paydays`: fechas de nómina dentro de la ventana. Cada una con `isExtraPay` si es un mes de paga extra con 14 pagas.
- `beforeNextPayday`: cobros entre hoy (inclusive) y la próxima nómina (exclusive). La próxima nómina es la primera fecha de nómina **posterior a hoy**: si hoy es día de nómina, cuenta la del mes siguiente. Devuelve `until` (la fecha de nómina), `total` y `count`.
- `betweenPaydays`: cobros entre la próxima nómina (inclusive) y la siguiente (exclusive). Devuelve `from`, `to`, `total`, `count` y `includesExtraPay`.

### 5.9 Hucha y "deberías llevar apartados"

Para cada gasto de la hucha (`chargesPerYear ≤ 6`, activo y completo):

- `monthlyShare = monthlyEquivalent`
- `previousCharge`: el último cobro de la serie ≤ hoy. `nextCharge`: el primero > hoy.
- `shouldHaveSaved = importe(nextCharge) × (hoy − previousCharge) / (nextCharge − previousCharge)`, con las diferencias en días.

Devuelve `hucha: { monthlyAmount, items: [{ expenseId, name, nextDate, nextAmount, monthlyShare, shouldHaveSaved }] }`.

## 6. Juego de datos de prueba (y de la demo)

Hoy = **viernes 20 de noviembre de 2026**. Catorce gastos, todos activos:

| # | Nombre | Tipo | Intervalo | Fecha de referencia | Importe | Modo | Extra |
|---|---|---|---|---|---|---|---|
| 1 | Spotify | Suscripción | 1 mes | 2026-11-21 | 11,99 | Fijo | |
| 2 | Gas natural | Suministro | 1 mes | 2026-11-24 | 36,00 | Variable | |
| 3 | Luz | Suministro | 1 mes | 2026-11-26 | 58,00 | Variable | |
| 4 | iCloud+ | Suscripción | 1 mes | 2026-11-27 | 2,99 | Fijo | |
| 5 | Fibra y móvil | Suministro | 1 mes | 2026-12-01 | 45,00 | Fijo | Promo hasta 2027-01-01, después 55,00 |
| 6 | Gimnasio | Cuota | 1 mes | 2026-12-01 | 39,90 | Fijo | |
| 7 | Extraescolares | Cuota | 1 mes | 2026-12-01 | 60,00 | Fijo | Meses activos: septiembre a junio |
| 8 | Agua | Suministro | 2 meses | 2026-12-10 | 42,00 | Variable | |
| 9 | Netflix | Suscripción | 1 mes | 2026-12-18 | 14,99 | Fijo | |
| 10 | Comunidad | Cuota | 3 meses | 2027-01-01 | 90,00 | Fijo | |
| 11 | Seguro de hogar | Seguro | 1 año | 2027-01-15 | 286,00 | Fijo | Preaviso 1 mes. Cobros: 2023-01-15 241,80; 2024-01-15 249,00; 2025-01-15 262,40; 2026-01-15 286,00 |
| 12 | Seguro del coche | Seguro | 1 año | 2027-03-03 | 412,00 | Fijo | Preaviso 1 mes |
| 13 | Amazon Prime | Suscripción | 1 año | 2027-03-12 | 49,90 | Fijo | |
| 14 | IBI | Impuesto | 1 mes | 2027-06-05 | 198,00 | Fijo | Meses activos: junio y noviembre |

Colores e iconos: los del catálogo (apartado 10). Ajustes: nómina el día 28, 14 pagas.

**Resultados esperados** (tests de aceptación del motor):

| Cálculo | Resultado |
|---|---|
| Total anual | 4.862,34 € |
| Total mensual | 405,20 € (sin redondear: 405,195) |
| Por tipo (anual) | Suministro 1.920,00 · Cuota 1.438,80 · Seguro 698,00 · Suscripción 409,54 · Impuesto 396,00 |
| Hucha | 146,33 € al mes (sin redondear: 146,325, prueba el redondeo) |
| Próximos 30 días | 9 cobros, 310,87 €, hasta el 2026-12-20 |
| Antes de la próxima nómina | 4 cobros, 108,98 €, hasta el 2026-11-28 |
| Entre nóminas (28 nov – 28 dic) | 9 cobros, 310,87 € |
| Meses de la previsión | nov (20–30) 108,98 € · dic 310,87 € · ene 654,87 € (el más caro) · feb (1–18) 211,89 € |
| Fin de la ventana de 90 días | 2027-02-18 |
| Fecha límite, seguro de hogar | 2026-12-15, faltan 25 días |
| Fecha límite, seguro del coche | 2027-02-03, faltan 75 días |
| Lo que vence pronto | 15 dic límite seguro de hogar · 1 ene fin de promoción de la fibra · 3 feb límite seguro del coche |
| Deberías llevar apartados | Agua 28,23 · Comunidad 48,91 · Seguro de hogar 242,12 · Seguro del coche 295,74 · Amazon Prime 34,59 · IBI 14,01 |
| Anillo de días | Agua: próximo 2026-12-10, 20 días, ciclo de 61 · Comunidad 42 / 92 · IBI 197 / 212 · Seguro de hogar 56 / 365 |
| Subida, seguro de hogar | de 262,40 a 286,00, 8,99 %; total pagado 1.039,20 € |
| En la previsión de enero | La fibra aparece a 55,00 € (fin de la promoción) |

Algunas cifras del prototipo no coinciden con estas. Por ejemplo, el prototipo dice 311,87 € entre nóminas: es una errata y valen 310,87 €. Manda esta tabla.

## 7. API

Convenciones:

- Rutas bajo `/api`. JSON en camelCase, fechas `yyyy-MM-dd` e importes como número con 2 decimales.
- Errores con `ProblemDetails` (RFC 7807). Los errores de validación devuelven 400 con `errors` por campo y mensajes en español ("El nombre es obligatorio").
- Cada gasto que devuelve la API incluye sus **campos calculados**: `nextChargeDate`, `previousChargeDate`, `daysUntilNextCharge`, `cycleDays`, `cycleProgress`, `nextChargeAmount`, `isEstimate`, `chargesPerYear`, `annualCost`, `monthlyEquivalent`, `cancellationDeadline`, `priceChange`, `totalPaid`, `isIncomplete`, `missingFields`, `inTrial` y `upcomingEvents`.

### 7.1 Gastos

| Método y ruta | Qué hace |
|---|---|
| `GET /api/expenses?status=active\|paused\|cancelled\|all` | Lista con campos calculados. Por defecto `active` (incluye los incompletos activos). Ordenada por `nextChargeDate`, con los incompletos al final |
| `GET /api/expenses/{id}` | Detalle: campos calculados, más `charges` (los últimos 24), `priceHistory` y `priceHistorySeries` |
| `POST /api/expenses` | Crea un gasto. Si trae `catalogId`, los campos que no vengan se rellenan con los valores por defecto del catálogo |
| `PUT /api/expenses/{id}` | Actualiza. Si cambia `Amount` en un gasto `Fixed`, crea un registro en `PriceHistory` |
| `POST /api/expenses/{id}/status` | Cambia el estado. Cuerpo: `{ "status": "Cancelled" }` |
| `DELETE /api/expenses/{id}` | Borra el gasto con sus cobros e historial |
| `POST /api/expenses/preview` | Recibe el mismo cuerpo que `POST` y devuelve los campos calculados **sin guardar**. Alimenta la vista previa "Así se verá" del formulario |
| `POST /api/expenses/{id}/charges` | Registra un cobro a mano. Cuerpo: `{ "date": "2026-11-26", "amount": 61.40 }` |
| `DELETE /api/expenses/{id}/charges/{chargeId}` | Borra un cobro |

Ejemplo de `GET /api/expenses/11`, abreviado:

```json
{
  "id": 11,
  "name": "Seguro de hogar",
  "kind": "Insurance",
  "status": "Active",
  "intervalCount": 1,
  "intervalUnit": "Year",
  "anchorDate": "2027-01-15",
  "activeMonths": null,
  "amountMode": "Fixed",
  "amount": 286.00,
  "noticeAmount": 1,
  "noticeUnit": "Month",
  "accentColor": "#50BEAA",
  "icon": "home",
  "remindBeforeCharge": true,
  "remindBeforeDeadline": true,
  "nextChargeDate": "2027-01-15",
  "previousChargeDate": "2026-01-15",
  "daysUntilNextCharge": 56,
  "cycleDays": 365,
  "cycleProgress": 0.85,
  "nextChargeAmount": 286.00,
  "isEstimate": false,
  "chargesPerYear": 1,
  "annualCost": 286.00,
  "monthlyEquivalent": 23.83,
  "cancellationDeadline": { "date": "2026-12-15", "daysLeft": 25, "renewalDate": "2027-01-15" },
  "priceChange": { "from": 262.40, "to": 286.00, "percent": 8.99, "fromDate": "2025-01-15", "toDate": "2026-01-15" },
  "totalPaid": 1039.20,
  "isIncomplete": false,
  "missingFields": [],
  "inTrial": false,
  "upcomingEvents": [ { "type": "CancellationDeadline", "date": "2026-12-15", "daysLeft": 25 } ]
}
```

### 7.2 Resumen, previsión y ajustes

| Método y ruta | Qué hace |
|---|---|
| `GET /api/summary` | Resumen (apartado 5.7) |
| `GET /api/forecast?days=90` | Previsión, nómina y hucha (apartados 5.8 y 5.9). `days` entre 1 y 365, por defecto 90 |
| `GET /api/settings` / `PUT /api/settings` | Ajustes (apartado 4.7) |

Los endpoints antiguos de resumen y próximas renovaciones se sustituyen por estos. Bórralos y actualiza sus tests.

### 7.3 Catálogo

| Método y ruta | Qué hace |
|---|---|
| `GET /api/catalog?q=iber` | Busca por nombre y alias, sin distinguir mayúsculas ni tildes. Máximo 10 resultados. Sin `q`, devuelve todo |
| `GET /api/catalog/{id}` | Un elemento |

### 7.4 Importación de extractos

Ver el apartado 9 para la lógica.

| Método y ruta | Qué hace |
|---|---|
| `POST /api/imports` | Sube un fichero (multipart, campo `file`). Crea una sesión de importación y devuelve el análisis |
| `POST /api/imports/{id}/files` | Añade otro fichero a la misma sesión (otro banco u otro periodo) |
| `PUT /api/imports/{id}/files/{fileId}/mapping` | Corrige la asignación de columnas o la hoja. Vuelve a analizar |
| `GET /api/imports/{id}/detections` | Cobros detectados, descartados y anuales sugeridos |
| `POST /api/imports/{id}/commit` | Crea los gastos elegidos con sus cobros. Cierra la sesión |
| `DELETE /api/imports/{id}` | Cancela y borra la sesión |

Respuesta de `POST /api/imports`, abreviada:

```json
{
  "importId": "8f0c…",
  "files": [
    {
      "fileId": "a1…",
      "fileName": "movimientos_jun-nov.xlsx",
      "sheetName": "Movimientos",
      "sheets": ["Movimientos"],
      "rowCount": 618,
      "dateFrom": "2026-06-01",
      "dateTo": "2026-11-19",
      "mapping": { "dateColumn": 0, "conceptColumn": 2, "amountColumn": 3, "debitColumn": null, "creditColumn": null, "signColumn": null },
      "columns": ["Fecha operación", "Fecha valor", "Concepto", "Importe", "Saldo"],
      "mappingConfidence": "High",
      "needsConfirmation": false,
      "usedSavedProfile": false,
      "sampleRows": [ { "date": "2026-11-02", "concept": "RECIBO ENDESA ENERGIA", "amount": -58.40 } ],
      "skippedRows": 3
    }
  ]
}
```

### 7.5 Calendario

| Método y ruta | Qué hace |
|---|---|
| `GET /api/calendar.ics` | Calendario completo de los próximos 12 meses |
| `GET /api/expenses/{id}/calendar.ics` | Solo los eventos de un gasto: fecha límite, fines de promoción, prueba y permanencia, y sus cobros de los próximos 12 meses. Alimenta el botón "Añadir el límite a mi calendario" |

### 7.6 Copia de seguridad, Wallos y demo

| Método y ruta | Qué hace |
|---|---|
| `GET /api/backup` | Descarga la copia JSON (apartado 11.1) |
| `POST /api/backup/restore` | Restaura una copia (multipart). **Sustituye todos los datos** |
| `POST /api/import/wallos/preview` | Analiza un JSON de Wallos y devuelve la lista con avisos, sin guardar |
| `POST /api/import/wallos/commit` | Crea los gastos elegidos. Recibe la lista del paso anterior, quizá editada por el usuario |
| `POST /api/demo` | Activa el modo demo. Solo si no hay ningún gasto; si hay, devuelve 409 |
| `DELETE /api/demo` | Sale del modo demo y borra todos los datos `IsDemo` |

## 8. Seguridad y arranque

La API no tiene autenticación, así que:

- **Escucha solo en `127.0.0.1`**, nunca en `0.0.0.0`.
- **Comprueba la cabecera `Host`**: acepta solo `localhost` y `127.0.0.1`, con cualquier puerto, y devuelve 400 en el resto. Esto evita ataques de *DNS rebinding* desde páginas web abiertas en el navegador.
- **CORS:** solo en desarrollo y solo para el origen de Vite.
- **Límite de subida:** 10 MB por fichero.

**Ubicación de la base de datos:** configurable. Por defecto, en la carpeta de datos de la aplicación del usuario: `Environment.SpecialFolder.ApplicationData` + `SubscriptionManager/data.db`. Las copias automáticas van en `SubscriptionManager/backups/`.

**Producción local:** el backend sirve el frontend compilado desde `wwwroot`, con una sola URL local y *fallback* a `index.html` para las rutas del frontend. En desarrollo, Vite con *proxy* a la API.

## 9. Importador de extractos

### 9.1 Objetivo

El usuario descarga los movimientos de su banco en Excel o CSV y los arrastra a la app. **No se le pide ninguna plantilla ni que toque el fichero.** La app lo convierte a una estructura interna única y sobre ella detecta los cobros que se repiten.

Estructura interna de cada movimiento: `{ date: DateOnly, concept: string, amount: decimal }`. El importe es negativo para cargos y positivo para abonos.

### 9.2 Lectura

- **Formatos:** `.xlsx`, `.xls` y `.csv`. Se identifican por el contenido, no solo por la extensión: un xlsx empieza por `PK` y un xls por la firma OLE `D0 CF 11 E0`.
- **Excel:** con **ExcelDataReader** (MIT), que lee `.xlsx` y `.xls`. Registra `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` al arrancar; los `.xls` lo necesitan. **No uses EPPlus**: desde la versión 5 no es gratuito para uso comercial.
  - Si hay varias hojas, elige la que tenga más filas con cabecera detectable. La respuesta lista las hojas para que el usuario pueda cambiarla.
- **CSV:**
  - Codificación: si hay BOM, la del BOM. Si no, prueba UTF-8 estricto y, si falla, Windows-1252, que muchos bancos españoles usan.
  - Separador: detecta `;`, `,`, tabulador o `|` por la constancia del número de campos en las primeras 20 líneas.
  - Comillas según RFC 4180. Puedes usar **CsvHelper** (bajo su licencia Apache-2.0) o un lector propio con tests.
- Todo se convierte a una cuadrícula de texto (filas × celdas). Las celdas numéricas y de fecha de Excel se conservan con su valor tipado para el paso 9.5.
- **Límites:** 10 MB y 50.000 filas. Si se superan, error claro en español.
- **Privacidad:** el fichero se lee en memoria, nunca se escribe en disco y no se registra en logs.

### 9.3 Encontrar la cabecera

Muchos bancos ponen antes de la tabla filas con el titular, la cuenta o el periodo.

1. Recorre las primeras 30 filas.
2. Puntúa cada fila como posible cabecera: la mayoría de celdas son texto, coincide con sinónimos (apartado 9.4) y **las filas de debajo tienen fechas y números en columnas estables**.
3. Elige la de mayor puntuación.
4. Si ninguna parece cabecera pero hay filas de datos claras (fecha, texto, número), trabaja **sin cabecera** con la detección por contenido.

### 9.4 Asignar las columnas

La asignación se hace **sobre todo por el contenido**, que no depende del idioma. Los nombres de las cabeceras solo ayudan a desempatar.

**Detección por contenido**, sobre las filas de datos:

- **Fecha:** al menos el 90 % de las celdas no vacías se leen como fecha (apartado 9.5). Si hay dos columnas de fecha (operación y valor), se prefiere la que coincide con un sinónimo de "operación"; si no hay pista, la primera.
- **Importe:** columna numérica, con al menos el 90 % de celdas numéricas, con decimales y mezclando signos.
- **Saldo frente a importe:** el saldo cumple que `saldo[i] ≈ saldo[i−1] + importe[i]`, con tolerancia de 0,01 y probando los dos sentidos de orden de las filas. La columna que cumple esa relación con otra es el saldo y se ignora.
- **Cargo y abono:** dos columnas numéricas en las que, en al menos el 90 % de las filas, hay exactamente una llena. Importe = abono − cargo, en valor absoluto.
- **Columna de signo:** si el importe no tiene negativos y hay una columna con valores tipo `D`/`H`, `Debe`/`Haber`, `Cargo`/`Abono` o `-`/`+`, esa columna da el signo.
- **Concepto:** la columna de texto con más valores distintos y mayor longitud media. Si hay varias de texto, se queda con la mejor y las demás se ignoran.

**Sinónimos** (en minúsculas, sin tildes, recortados), solo como pista:

- Fecha: fecha, fecha operacion, f. operacion, fecha valor, f. valor, data, data operacio, date, transaction date, booking date.
- Concepto: concepto, descripcion, detalle, movimiento, observaciones, concepte, descripcio, description, details, payee.
- Importe: importe, cantidad, importe (eur), import, amount.
- Cargo: cargo, debe, salidas, carrec, debit, withdrawal.
- Abono: abono, haber, entradas, abonament, credit, deposit.
- Saldo: saldo, saldo disponible, balance.
- Signo: d/h, signo, tipo.

**Confianza de la asignación:**

- `High`: fecha, concepto e importe (o cargo y abono) asignados sin ambigüedad y validados por contenido.
- `Low`: en cualquier otro caso, o si se ha elegido entre candidatos parecidos. Entonces `needsConfirmation: true` y la interfaz muestra 5 filas de muestra con un desplegable por dato (fecha, concepto, importe, o cargo y abono) para que el usuario elija.

Aunque la confianza sea alta, la interfaz **siempre** enseña las filas de muestra con las columnas elegidas antes de seguir, para que el usuario pueda corregirlas.

**Perfiles recordados:** al confirmar la asignación, guarda un `ImportProfile` con la firma de la cabecera. Para la firma, normaliza las celdas así: minúsculas, sin tildes, recortadas, espacios internos simples, y unidas con `|`. Si un fichero nuevo tiene la misma firma, se aplica el perfil directamente (`usedSavedProfile: true`, confianza alta).

### 9.5 Normalizar valores

**Fechas:**

- Celdas de fecha de Excel (incluido el número de serie: `DateTime.FromOADate`).
- Textos `dd/MM/yyyy`, `dd-MM-yyyy`, `dd.MM.yyyy`, `dd/MM/yy`, `yyyy-MM-dd` y `yyyy/MM/dd`, con o sin hora (la hora se ignora).
- Orden día y mes ambiguo: se decide **por columna**. Si algún primer número es mayor que 12, es `dd/MM`. Si algún segundo número es mayor que 12, es `MM/dd`. Si no se puede saber, `dd/MM`.

**Importes:**

- Celdas numéricas de Excel: se convierten a `decimal` y se redondean a 2 decimales.
- Textos como "1.234,56", "-58,40", "58,40-", "1,234.56", "58.40 €", "EUR 58,40" o "(58,40)" (contable, negativo).
- Separador decimal decidido **por columna**: si aparecen `.` y `,`, el último que aparece es el decimal. Si solo aparece `,` seguida de 1 o 2 dígitos al final, es decimal. Si solo aparece `.` seguido de exactamente 3 dígitos y hay otros valores con `,` decimal, es separador de miles.

**Filas que se saltan**, contándolas en `skippedRows`: vacías, sin fecha válida o sin importe, y las de totales o saldo. Por ejemplo, filas cuyo concepto empieza por "saldo", "total" o "saldo anterior".

**Duplicados:** si dos ficheros de la misma sesión se solapan, los movimientos idénticos (misma fecha, importe y concepto normalizado) cuentan una vez.

### 9.6 Detectar cobros que se repiten

Sobre los **cargos** (importe < 0, tomados en valor absoluto) de todos los ficheros de la sesión:

**1. Normalizar el concepto para agrupar:**

- mayúsculas y sin tildes;
- quitar secuencias de 4 o más dígitos (referencias, fechas, números de tarjeta), máscaras tipo `XXXX1234` y fechas `dd/mm`;
- quitar prefijos habituales: "RECIBO", "ADEUDO", "ADEUDO SEPA", "CARGO", "DOMICILIACION", "COMPRA TARJ.", "COMPRA TARJETA", "PAGO", "TRANSFERENCIA A", "TRANSF.";
- espacios internos simples.

**2. Agrupar y separar en series:**

- **Agrupa por concepto normalizado idéntico.** El catálogo **no** se usa para agrupar: si se agrupara por empresa, se mezclarían contratos distintos de la misma empresa y la serie parecería irregular.
- **Separa cada grupo en series.** Si un grupo tiene más de un cargo dentro del mismo periodo (por ejemplo, dos cargos en el mismo mes), reparte los cargos en series:
  - por día del mes, con ±3 días de margen;
  - y por importe: en los fijos, ±1 %; en los variables, cada cargo va a la serie cuya media esté más cerca.
  - Cada serie se clasifica por separado (paso 3).
  - Ejemplos: la luz y el gas de la misma compañía con el mismo concepto; dos líneas de móvil; varias suscripciones de Apple.
- **Une grupos distintos solo si** coinciden con el mismo elemento del catálogo **y** juntos forman una única serie regular. Es el caso del banco que cambia el texto del concepto a mitad de periodo.
- **El catálogo pone nombre, tipo, color e icono** a cada serie: si el concepto contiene un `bankPattern`, la serie es ese elemento. Gana el patrón más largo, para que "MOVISTAR PLUS" gane a "MOVISTAR" y "GOOGLE *GOOGLE STORAGE" gane a "GOOGLE *GOOGLE". Si no coincide con ningún elemento, se prueban los `kindHints`.
- **Luz o gas.** Varias compañías venden las dos cosas con el mismo nombre. Si la serie coincide con `luz` o `gas`:
  1. quita del concepto el texto del patrón que ha coincidido;
  2. si lo que queda contiene "LUZ", "ELECTRICIDAD" o "ELECTRICA", es luz; si contiene "GAS", es gas;
  3. si no hay pista, se queda el elemento del patrón y el usuario lo corrige en la revisión.

**3. Clasificar cada grupo con 2 o más cargos:**

- Calcula los días entre cargos consecutivos y su mediana `m`. Asigna intervalo según `m`:
  - 6–8: 1 semana
  - 13–16: 2 semanas
  - 27–33: 1 mes
  - 55–66: 2 meses
  - 85–97: 3 meses
  - 170–195: 6 meses
  - 350–380: 1 año
- **Regular:** todos los intervalos están a ±20 % de `m`. En los mensuales, a ±5 días.
- **Importe:**
  - fijo si `max − min ≤ max(0,05 €; 1 % de la media)`;
  - variable si el coeficiente de variación es ≤ 0,5;
  - si no, irregular.
- **Confianza:**
  - `High`: 3 o más cargos, intervalos regulares, intervalo asignado e importe fijo o variable. También coincidencia con catálogo, 2 o más cargos y regular.
  - `Medium`: 2 cargos regulares con intervalo asignado, 3 o más con alguna irregularidad leve (intervalos a ±35 %), o coincidencia con catálogo con 1 solo cargo (ver más abajo).
  - El resto de grupos con 2 o más cargos va a **"probablemente no son gastos fijos"**, ordenados por número de cargos, máximo 10. Ejemplos: el supermercado o Bizum con importes distintos.

**4. Casos especiales:**

- **Un solo cargo de un elemento del catálogo anual** (por ejemplo, un seguro): se propone como anual con ese importe y esa fecha, con confianza `Medium`.
- **Coincidencia con catálogo con 2 cargos que no encajan en ningún intervalo** (por ejemplo, el IBI fraccionado: junio y noviembre): se propone con el intervalo y los meses por defecto del catálogo y los importes reales, con confianza `Medium`.
- **Transferencias:** no se descartan por ser transferencias. Un alquiler pagado por transferencia mensual del mismo importe es un gasto fijo. Se aplica el mismo test de regularidad.

**5. Cada propuesta lleva:**

- `detectionId`;
- `name`: el del catálogo o, si no hay, el concepto normalizado en formato título;
- `kind`, `accentColor` e `icon`: del catálogo, o los valores por defecto de `Other`;
- `intervalCount` e `intervalUnit`;
- `amountMode`: `Fixed` o `Variable`;
- `amount`: el último importe si es fijo, o la media de los 3 últimos si es variable;
- `anchorDate`: la fecha del último cargo;
- `activeMonthsSuggestion`: la del catálogo, si existe. La interfaz la muestra como pregunta, por ejemplo "¿de septiembre a junio?";
- `confidence`;
- `bankConcept`: el concepto original más frecuente;
- `movements`: fecha e importe de cada cargo, que serán los `ChargeRecord`;
- `catalogId`.

### 9.7 Anuales sugeridos

Si el periodo cubierto por la sesión es menor de 13 meses, se proponen los elementos del catálogo con `suggestIfMissing: true` que **no** se hayan detectado. Van con texto explicativo en la interfaz: "Con seis meses de movimientos no se ven los cobros de una vez al año".

Se proponen **sin importe y sin fecha**: si el usuario los marca, se crean como gastos **incompletos** con los valores por defecto del catálogo.

### 9.8 Confirmar (commit)

El cuerpo trae:

- las detecciones aceptadas, con los cambios que haya hecho el usuario: nombre, tipo, importe, intervalo y meses activos;
- los ids de catálogo de los anuales sugeridos que ha marcado.

Para cada detección aceptada, dentro de una transacción:

- Crea el `RecurringExpense`.
- Crea un `ChargeRecord` por cada movimiento, con `Source = BankImport`, `BankConcept` y `DedupKey` = SHA-256 de `fecha|importe|concepto normalizado`.
- **Si ya existe un gasto con el mismo `catalogId`, o con el mismo nombre y tipo**, no crea otro: añade los cobros nuevos (los duplicados se saltan por `DedupKey`). Si es `Fixed` y el último cobro importado tiene un importe distinto de `Amount`, la respuesta lo indica como `priceChangeCandidates`. La interfaz pregunta "¿Actualizar el precio a X €?" y, si el usuario acepta, se hace el `PUT` normal, que crea el registro en `PriceHistory`.

Devuelve los ids creados y actualizados. La sesión se borra.

### 9.9 Sesiones

- Se guardan en `IMemoryCache` con caducidad deslizante de 30 minutos y nunca se escriben en disco.
- Una sesión puede tener varios ficheros.
- Si caduca, la API devuelve 404 con el mensaje "La importación ha caducado. Vuelve a subir el fichero".

## 10. Catálogo de servicios

### 10.1 Formato

Fichero JSON embebido como recurso (`Catalog/catalog.es.json`), cargado una vez al arrancar. Es de solo lectura y no va en la base de datos. Así la comunidad puede añadir servicios con un *pull request* sin migraciones.

```json
{
  "version": 1,
  "items": [
    {
      "id": "netflix",
      "name": "Netflix",
      "aliases": [],
      "kind": "Subscription",
      "intervalCount": 1,
      "intervalUnit": "Month",
      "amountMode": "Fixed",
      "activeMonths": null,
      "notice": null,
      "suggestIfMissing": false,
      "accentColor": "#E12D37",
      "icon": "play",
      "bankPatterns": ["NETFLIX"]
    }
  ],
  "kindHints": [
    { "kind": "Insurance", "intervalCount": 1, "intervalUnit": "Year", "notice": { "amount": 1, "unit": "Month" }, "icon": "shield",
      "bankPatterns": ["MAPFRE", "MUTUA MADRILENA", "LINEA DIRECTA", "AXA", "ALLIANZ", "GENERALI", "ZURICH", "SANTALUCIA", "OCASO", "REALE", "CASER", "VERTI", "QUALITAS", "PELAYO", "LIBERTY", "ASEGURADORA", "SEGUROS", "SEGURO"] },
    { "kind": "Tax", "intervalCount": 1, "intervalUnit": "Year", "notice": null, "icon": "landmark",
      "bankPatterns": ["AYUNTAMIENTO", "AJUNTAMENT", "CONCELLO", "UDALA", "DIPUTACION", "DIPUTACIO", "ORGT", "SUMA GESTION", "BASE GESTIO", "ORGANISMO AUTONOMO", "RECAUDACION", "AGENCIA TRIBUTARIA"] }
  ]
}
```

Notas sobre el formato:

- **`kindHints`**: patrones que indican el tipo pero no el servicio concreto. Por ejemplo, una aseguradora puede ser el seguro de hogar o el del coche. Si un cargo coincide, la detección usa ese tipo y esos valores por defecto, con el nombre sacado del concepto, y el usuario elige cuál es.
- **Sin precios.** Cambian a menudo y quedarían viejos.
- **`notice`** solo se rellena en los seguros: 1 mes. En la interfaz se presenta como "valor habitual en seguros, compruébalo en tu póliza", nunca como norma legal.
- **Los `bankPatterns` son una primera aproximación** y hay que afinarlos con extractos reales. Se comparan con el concepto normalizado (mayúsculas, sin tildes). El apartado 10.3 dice cuáles están contrastados con fuentes públicas y cuáles no.
- **Elementos genéricos (`apple`, `google`).** Apple y Google cobran muchos servicios distintos con el mismo concepto ("APPLE.COM/BILL", "GOOGLE *…"), así que no se puede saber cuál es solo por el texto. La detección los propone como "Apple (suscripción)" o "Google (suscripción)" y la revisión le pide al usuario que ponga el nombre del servicio. Apple, además, puede agrupar varias compras en un solo cargo, así que una serie de Apple puede salir con importe variable.

### 10.2 Contenido inicial (33 elementos)

| id | Nombre | Tipo | Intervalo | Modo | Otros | Icono | Color | bankPatterns |
|---|---|---|---|---|---|---|---|---|
| netflix | Netflix | Suscripción | 1 mes | Fijo | | play | #E12D37 | NETFLIX |
| spotify | Spotify | Suscripción | 1 mes | Fijo | | music | #28BE69 | SPOTIFY |
| disney-plus | Disney+ | Suscripción | 1 mes | Fijo | | play | #415AEB | DISNEY |
| hbo-max | HBO Max | Suscripción | 1 mes | Fijo | | play | #8C5AF0 | HBO, HBOMAX, MAX.COM |
| amazon-prime | Amazon Prime | Suscripción | 1 año | Fijo | suggestIfMissing | box | #FFA03C | AMAZON PRIME, AMZN PRIME, PRIME VIDEO |
| youtube-premium | YouTube Premium | Suscripción | 1 mes | Fijo | | play | #F0503C | GOOGLE *GOOGLE, YOUTUBE |
| icloud | iCloud+ | Suscripción | 1 mes | Fijo | solo para alta manual | cloud | #5AB9FF | (ninguno: los cargos de Apple van a `apple`) |
| apple | Apple (suscripción) | Suscripción | 1 mes | Variable | genérico: el usuario pone el nombre | cloud | #A0AAB9 | APPLE.COM/BILL, ITUNES.COM |
| google-one | Google One | Suscripción | 1 mes | Fijo | | cloud | #5A8CF0 | GOOGLE *GOOGLE STORAGE, GOOGLE ONE |
| google | Google (suscripción) | Suscripción | 1 mes | Fijo | genérico: el usuario pone el nombre | cloud | #7896C8 | GOOGLE * |
| microsoft-365 | Microsoft 365 | Suscripción | 1 año | Fijo | | cloud | #E8743B | MICROSOFT 365, MICROSOFT*365, MSFT |
| dazn | DAZN | Suscripción | 1 mes | Fijo | | play | #D2DC3C | DAZN |
| movistar-plus | Movistar Plus+ | Suscripción | 1 mes | Fijo | | play | #2D9BF0 | MOVISTAR PLUS, MOVISTAR+ |
| filmin | Filmin | Suscripción | 1 mes | Fijo | | play | #F0645A | FILMIN |
| audible | Audible | Suscripción | 1 mes | Fijo | | book | #F5A032 | AUDIBLE |
| luz | Luz | Suministro | 1 mes | Variable | | bolt | #F5B437 | IBERDROLA, ENDESA, REPSOL, TOTALENERGIES, HOLALUZ, OCTOPUS, SOM ENERGIA, ENERGIA XXI, CURENERGIA, COMERCIALIZADORA REGULADA, GAS & POWER, BASER, REGSITI, CHC, TERAMELCOR |
| gas | Gas natural | Suministro | 1 mes | Variable | | flame | #F06E3C | NATURGY, GAS NATURAL |
| agua | Agua | Suministro | 2 meses | Variable | | drop | #46AAF0 | CANAL DE ISABEL, CANAL ISABEL, AIGUES, AGBAR, EMASESA, EMAYA, EMIVASA, AGUAS DE, AGUA, AIGUA |
| fibra-movil | Fibra y móvil | Suministro | 1 mes | Fijo | | wifi | #C850C8 | MOVISTAR, TELEFONICA, O2, VODAFONE, LOWI, ORANGE, JAZZTEL, SIMYO, XFERA, YOIGO, MASMOVIL, PEPEPHONE, LLAMAYA, LEBARA, VIRGIN TELCO, DIGI, FINETWORK |
| seguro-hogar | Seguro de hogar | Seguro | 1 año | Fijo | preaviso 1 mes · suggestIfMissing | home | #50BEAA | (vía kindHints) |
| seguro-coche | Seguro del coche | Seguro | 1 año | Fijo | preaviso 1 mes · suggestIfMissing | car | #5A82F0 | (vía kindHints) |
| seguro-vida | Seguro de vida | Seguro | 1 año | Fijo | preaviso 1 mes | heart | #DC6E8C | (vía kindHints) |
| seguro-salud | Seguro de salud | Seguro | 1 mes | Fijo | preaviso 1 mes | heart | #3CB4A0 | SANITAS, ADESLAS, DKV, ASISA |
| seguro-mascota | Seguro de mascota | Seguro | 1 año | Fijo | preaviso 1 mes | paw | #C8A050 | (vía kindHints) |
| ibi | IBI | Impuesto | 1 año | Fijo | suggestIfMissing | landmark | #B4B9C8 | IBI, IBIU, IBI URBANA, BIENES INMUEBLES, BENS IMMOBLES |
| circulacion | Impuesto de circulación | Impuesto | 1 año | Fijo | suggestIfMissing | car | #A0AABE | IVTM, VEHICULOS TRACCION, IMPUESTO CIRCULACION, VEHICLES TRACCIO |
| basuras | Tasa de basuras | Impuesto | 1 año | Fijo | | landmark | #8C96AA | BASURA, BASURAS, RESIDUOS, RESIDUS, ESCOMBRARIES |
| gimnasio | Gimnasio | Cuota | 1 mes | Fijo | | gym | #E65A82 | GIMNASIO, GYM, FITNESS, BASIC-FIT, ALTAFIT, MCFIT, DIR, ANYTIME, METROPOLITAN, SYNERGYM, VIVAGYM, GO FIT |
| comunidad | Comunidad de propietarios | Cuota | 3 meses | Fijo | | building | #9678DC | COMUNIDAD PROP, COMUNIDAD DE PROPIETARIOS, COM PROP, COM. PROP, CDAD PROP, CMDAD PROP, C. PROPIETARIOS, COMUNITAT PROP, C.P. |
| colegio | Colegio | Cuota | 1 mes | Fijo | meses: septiembre a junio | school | #64BE8C | COLEGIO, ESCUELA, ESCOLA |
| extraescolares | Extraescolares | Cuota | 1 mes | Fijo | meses: septiembre a junio | book | #78D25A | EXTRAESC, ACTIV. EXTRAESC, EXTRAESCOLAR |
| hipoteca | Hipoteca | Préstamo | 1 mes | Fijo | | bank | #9678DC | PRESTAMO HIPOTECARIO, HIPOTECA, CUOTA PRESTAMO, AMORTIZACION PRESTAMO |
| alquiler | Alquiler | Otro | 1 mes | Fijo | | key | #8F99AA | ALQUILER, LLOGUER |

`seguro-salud` tiene patrones propios porque esas aseguradoras son sobre todo de salud. Si un patrón aparece en un elemento y en `kindHints`, gana el elemento.

Patrones cortos o ambiguos (`DIR`, `O2`, `HBO`, `CHC`, `IBI`, `C.P.`, `AGUA`, `AIGUA`, `DIGI`, `AXA`, `GYM`, `SEGURO`) deben coincidir como **palabra completa** (límites de palabra), no como subcadena. Los patrones que terminan en `*` (como `GOOGLE *`) coinciden con el inicio del texto que sigue.

### 10.3 Qué patrones están contrastados

Contrastados con fuentes públicas (noviembre de 2026):

- **Grupo MásMóvil:** los cargos de Yoigo, MásMóvil, Pepephone, Llamaya, Lebara y Virgin Telco llegan a nombre de **Xfera Móviles**. De ahí el patrón `XFERA`.
- **Jazztel:** sus recibos llegan a nombre de **Orange Espagne**. Lo cubre `ORANGE`.
- **Movistar:** el emisor del recibo es **Telefónica de España SAU**, y también se han visto conceptos "Movistar Móvil". Lo cubren `TELEFONICA` y `MOVISTAR`.
- **Lowi:** desde octubre de 2022 el emisor del cargo es **Vodafone** y en el concepto aparece "Lowi". Lo cubren `VODAFONE` y `LOWI`.
- **Comercializadoras de referencia de la luz** (tarifa regulada): Energía XXI (Endesa), Curenergía (Iberdrola), Comercializadora Regulada Gas & Power (Naturgy), Baser COR (TotalEnergies), Régsiti (Repsol), CHC COR, Teramelcor y Energía Ceuta XXI.
- **Google:** los cargos empiezan por `GOOGLE*` seguido del producto. Según Google, `GOOGLE *GOOGLE` es YouTube Premium y `GOOGLE *Google Storage` es el almacenamiento de Google Drive (Google One).
- **Apple:** `APPLE.COM/BILL` aparece en compras de apps, música y películas, en renovaciones de suscripciones y en compras de la familia, y puede agrupar varias compras en un cargo.

**Sin contrastar (marcar como provisionales en el JSON con un campo `"verified": false`):** el formato de Amazon en España, los organismos de recaudación del IBI y otros impuestos, las abreviaturas de comunidades de propietarios, aseguradoras, gimnasios, compañías de agua, Digi, Simyo y Finetwork. Se irán afinando con extractos reales aportados por la comunidad.

### 10.4 Uso

- **Autocompletar en el formulario:** al elegir un elemento, se rellenan tipo, intervalo, modo, meses activos, preaviso, color e icono. El usuario solo pone importe y fecha.
- **Poner nombre, tipo, color e icono** a las series detectadas al importar (apartado 9.6).
- **Valores por defecto** en `POST /api/expenses` cuando llega `catalogId`.

## 11. Copia de seguridad, Wallos, calendario y demo

### 11.1 Copia de seguridad (JSON propio)

```json
{
  "format": "subscription-manager-backup",
  "version": 1,
  "exportedAt": "2026-11-20T18:00:00Z",
  "settings": { },
  "expenses": [ { "…todos los campos del gasto…": "", "charges": [ ], "priceHistory": [ ] } ],
  "importProfiles": [ ]
}
```

- **Exporta todo:** ajustes, gastos con sus cobros e historial, y perfiles de importación. Excluye los datos `IsDemo`. El nombre del fichero lleva la fecha: `subscription-manager-2026-11-20.json`.
- **Restaurar:**
  1. Valida `format` y `version`.
  2. Copia el fichero `.db` a `backups/`.
  3. En una transacción, borra todo y carga la copia.
  4. Si algo falla, no se cambia nada y se devuelve el error.
- La interfaz pide confirmación antes de restaurar: "Se sustituirán todos tus datos actuales".

### 11.2 Importar desde Wallos

**Entrada:** el JSON que exporta Wallos desde sus ajustes. El formato ha cambiado entre versiones, así que acepta:

- **Formato A**, con claves legibles: `Name`, `Payment Cycle`, `Next Payment`, `Price`, `Category`, `Notes`, `URL`, `State`, `Active`, `Cancellation Date`. Puede venir como array o como objeto con `subscriptions`.
- **Formato B**, filas de la base de datos: `name`, `price`, `currency`, `next_payment`, `cycle`, `frequency`, `inactive`, `notes`, `url`, `category`, `cancellation_date`. En este formato, `cycle` y `frequency` ya vienen traducidos a texto.

Claves sin distinguir mayúsculas. **Estas claves están sacadas de herramientas de terceros que importan desde Wallos.** Antes de dar la fase por terminada, verifica el mapeo con un fichero exportado de verdad desde una instancia de Wallos y guárdalo, anonimizado, como fichero de prueba.

**Mapeo:**

- **Nombre:** `Name` / `name`.
- **Importe:** se extrae el número de `Price` / `price`.
  - Si la moneda (símbolo o código en el texto, o el campo `currency`) **no es EUR**, se importa el número tal cual y se añade el aviso "Importe en otra moneda (USD): revísalo".
- **Ciclo**, de `Payment Cycle` o de `cycle` + `frequency`:
  - Daily, Weekly, Monthly, Yearly / Annually → 1 de su unidad;
  - "Every N Days/Weeks/Months/Years" → N de esa unidad;
  - Biweekly → 2 semanas; Bimonthly → 2 meses; Quarterly → 3 meses; Semi-annual → 6 meses;
  - si no se reconoce: 1 mes y el aviso "Ciclo no reconocido: revísalo".
- **Fecha de referencia:** `Next Payment` / `next_payment`.
- **Estado:** `Cancelled` si `State` no es "Enabled", si `Active` no es "Yes" o si `inactive` es verdadero. En cualquier otro caso, `Active`.
- **Descripción:** notas y URL unidas.
- **Tipo:** si el nombre coincide con un elemento del catálogo, el suyo, con su color e icono. Si no, se deduce de la categoría de Wallos: "insurance" o "seguro" → Seguro; "utilities" o "suministro" → Suministro; "tax" → Impuesto. En cualquier otro caso, Suscripción.

**Flujo:** vista previa con avisos por elemento, el usuario elige cuáles importar, y commit en transacción con `Source = WallosImport` en lo que aplique.

### 11.3 Calendario ICS

Generador propio (son pocos tipos de evento), con tests de formato:

- **Cabecera:**
  - `BEGIN:VCALENDAR`, `VERSION:2.0`, `PRODID:-//Subscription Manager//ES`;
  - `CALSCALE:GREGORIAN`, `METHOD:PUBLISH`, `X-WR-CALNAME:Subscription Manager`.
- **Ventana:** de hoy a hoy + 12 meses.
- **Eventos de día completo:** `DTSTART;VALUE=DATE:yyyyMMdd` y `DTEND;VALUE=DATE` al día siguiente. Así no hay problemas de zona horaria.
- **Cobros:** `SUMMARY:Cobro: Netflix · 14,99 €`, con `≈` delante del importe si es estimado.
- **Fechas límite:**
  - `SUMMARY:Fecha límite de baja: Seguro de hogar`;
  - `DESCRIPTION:Si no quieres renovar, avisa antes de esta fecha. Renovación: 15/01/2027.`
- **Fines de promoción, prueba y permanencia:** `SUMMARY:Fin de la promoción: Fibra y móvil`, y lo mismo con "Fin de la prueba gratuita" y "Fin de la permanencia".
- **`UID` estable:** `{expenseId}-{tipo}-{yyyyMMdd}@subscription-manager`. Ayuda a las apps que actualizan eventos al reimportar; no todas lo hacen.
- `DTSTAMP` con la hora actual en UTC.
- **Avisos (`VALARM` con `ACTION:DISPLAY`):**
  - en cobros si `RemindBeforeCharge`, y en los demás eventos si `RemindBeforeDeadline`;
  - antelación según `ReminderDaysBefore` y `ReminderHour`. El disparador es relativo al inicio del día del evento: aviso a las 9:00 de 7 días antes → `TRIGGER:-P6DT15H`.
- **Formato:** líneas terminadas en CRLF y plegadas a 75 octetos sin partir caracteres UTF-8. Se escapan `\`, `;`, `,` y los saltos de línea.
- **Respuesta:** `Content-Type: text/calendar; charset=utf-8` y `Content-Disposition: attachment; filename="subscription-manager.ics"`.
- **El archivo no se actualiza solo.** La interfaz dice "Si cambias algún gasto, vuelve a generarlo".

### 11.4 Modo demo

- `POST /api/demo` solo funciona si no hay ningún gasto.
- Carga el juego de datos del apartado 6 con `IsDemo = true`, **desplazando todas las fechas** (referencia, promoción y cobros) por la diferencia entre hoy y el 20 de noviembre de 2026. Así la demo siempre parece actual y conserva las mismas cifras.
- Pone `AppSettings.IsDemo = true`, nómina el día 28 y 14 pagas.
- Mientras dure la demo, todo lo que cree el usuario se marca también `IsDemo`.
- `DELETE /api/demo` borra todo lo `IsDemo` y devuelve los ajustes a sus valores por defecto.
- La copia de seguridad no incluye datos demo.

## 12. Frontend (después del backend)

### 12.1 Pantallas (según el prototipo)

1. **Bienvenida** (`V1Bienvenida`). Se muestra si no hay gastos y no está la demo activa.
   - Dos opciones:
     - "Importar el extracto de tu banco" (recomendada);
     - "Traer tus datos de otra herramienta" (Wallos o copia de seguridad).
   - El enlace "Prefiero verlo primero con datos de ejemplo".
   - Lista de confianza: "Todo se guarda en tu ordenador", "Sin cuenta y sin conectar tu banco", "Código abierto (AGPL-3.0), sin telemetría", "Exporta todo cuando quieras".
2. **Importar** (`V1Importar`). Pasos Fichero → Revisar → Listo.
   - Tras subir el fichero, enseña las filas de muestra con las columnas elegidas y permite corregirlas. **Este paso no está dibujado en el prototipo**: hazlo sencillo, con una tabla de 5 filas y un desplegable por dato.
   - Lista de cobros detectados con casillas, confianza y patrón; lista de "probablemente no son gastos fijos"; anuales sugeridos (sin importe, con el texto "importe pendiente" al marcarlos).
   - Total mensual de lo marcado y botón para añadir.
3. **Tu foto** (`V1TuFoto`).
   - Total anual animado y barra por tipo.
   - Tres datos: próximos 30 días, próxima fecha límite de baja y cuánto apartar cada mes.
   - Botón "Añadir los avisos a mi calendario", que descarga el ICS.
4. **Resumen** (`V1Resumen`).
   - Interruptor Al mes / Al año y total.
   - Datos: gastos activos, próximos 30 días y a apartar al mes.
   - "Lo que vence pronto", gráfico de dos anillos por tipo y cartera de tarjetas.
5. **Detalle** (`V1Detalle`).
   - Tarjeta y datos del gasto.
   - Botones "Editar" y "Marcar como dado de baja" (o "Reactivar" si no está activo).
   - Panel de fecha límite (solo si hay preaviso), con "Añadir el límite a mi calendario".
   - Panel de historial de precio (solo si hay cobros o historial), con "+X %" y "Llevas pagados".
6. **Previsión** (`V1Prevision`). Hucha con "deberías llevar apartados", "antes de la próxima nómina" y línea de 90 días con meses y nóminas.
7. **Nuevo gasto / Editar** (`V1NuevoGasto`).
   - Buscador de catálogo y chips de tipo.
   - Modo de importe Fijo / Variable / Aproximado.
   - Frecuencia "Cada N + unidad", fecha del próximo cobro y "Solo en algunos meses".
   - Sección Contrato con interruptores (permanencia, precio promocional, prueba gratuita, preaviso), todos apagados por defecto.
   - Avisos: casillas "N días antes de cada cobro" y "N días antes de la fecha límite de baja".
   - Vista previa "Así se verá" con `POST /api/expenses/preview`.

### 12.2 Lo mínimo que no está en el prototipo

Hace falta, pero sin diseño propio. Usa el mismo estilo:

- **Menú de ajustes** en la cabecera con:
  - Nómina (día y 12 o 14 pagas);
  - Descargar calendario;
  - Importar extracto;
  - Exportar copia;
  - Restaurar copia;
  - Importar desde Wallos;
  - "Salir de la demo" (solo en demo);
  - "Acerca de": versión, licencia AGPL-3.0 y enlace "Código fuente" al repositorio (apartado 16.4).
- **Gastos pausados y dados de baja:** sección plegable al final del Resumen, "Pausados y dados de baja (n)", con enlace a su detalle.
- **Gastos incompletos:** aviso en el Resumen, "Tienes 2 gastos sin completar", con enlace para completarlos.
- **Nómina sin configurar:** en Previsión, en lugar del bloque de nómina, "¿Qué día cobras la nómina?" con el día y 12 o 14 pagas.
- **Banda de demo:** "Estás viendo datos de ejemplo · Salir de la demo".

### 12.3 Cambios respecto a `subscription-manager-dashboard-spec.md`

- "Suscripción" pasa a "Gasto" y el botón a "Nuevo gasto".
- La navegación es Resumen y Previsión. **No hay** pantallas "Suscripciones" ni "Categorías".
- El gráfico "Por categoría" pasa a **"Por tipo de gasto"**: anillo interior por tipo y exterior por gasto. Una porción por tipo; las agrupaciones del prototipo ("Cuotas y educación", "Impuestos y comunidad") eran de ejemplo.
- El grabado de la tarjeta va **por tipo**:

  | Tipo | Grabado |
  |---|---|
  | Suscripción | círculos |
  | Suministro | arcos |
  | Seguro | rayos |
  | Cuota | rayos |
  | Impuesto | rombos |
  | Préstamo | rombos |
  | Otro | arcos |

- La etiqueta superior de la tarjeta es el tipo en mayúsculas. A la derecha, el ciclo: "mensual", "cada 2 meses", "trimestral", "anual", "sep–jun"…
- El anillo de días usa `cycleProgress` y `daysUntilNextCharge` de la API (ciclo real entre cobros), no 30 o 365 fijos.
- El reverso de la tarjeta muestra `chargesPerYear`: "10 cargos de 60 €", "6 cargos de ≈ 42 €", "1 cargo · vie 15 ene".
- La tarjeta lleva insignia si hay evento próximo: "Baja hasta 15 dic", "Promo hasta 1 ene", "Prueba hasta 27 nov".
- Los importes estimados llevan `≈` y la nota "los importes variables son estimados".
- Formato de números: `Intl.NumberFormat('de-DE', …)` o un formateador propio. `es-ES` no pone el punto de miles en números de cuatro cifras.

## 13. Plan de trabajo por fases

Cada fase:

- empieza leyendo el código afectado;
- termina con `dotnet test` en verde y sus criterios de aceptación cumplidos;
- se sube en uno o varios commits con Conventional Commits en inglés.

Sigue la disciplina de ramas del repositorio. Si no hay ninguna, usa una rama por fase.

### Fase 0 · Base

- Ubicación de la base de datos y copia automática antes de migrar (apartados 4.8 y 8).
- Escucha en `127.0.0.1`, comprobación de `Host`, CORS de desarrollo y límite de subida.
- `TimeProvider` inyectado.
- `ProblemDetails` con mensajes en español.
- Licencia y contribuciones (apartado 16): `LICENSE`, `CLA.md` y `CONTRIBUTING.md` en la raíz, sección de licencia del README, cabecera SPDX en todos los ficheros de código existentes y `THIRD-PARTY-NOTICES.md`.
- **Tests:**
  - petición con `Host: evil.example` → 400;
  - con migraciones pendientes se crea una copia en `backups/` y se conservan solo 10.

### Fase 1 · Modelo y migración (puntos 1 a 4)

- Entidades y enums del apartado 4, y migración del apartado 4.8.
- CRUD de gastos, cambio de estado, cobros manuales y `PriceHistory` al cambiar `Amount`.
- Validaciones del apartado 4.3.
- **Tests:**
  - la migración convierte un `Subscription` mensual y uno anual con categoría al modelo nuevo, con su descripción;
  - validaciones: `AmountMax < AmountMin`, `ActiveMonths` con unidad `Year`, preaviso sin unidad e icono inválido → 400;
  - cambiar `Amount` crea `PriceHistory`;
  - un gasto incompleto se guarda y sale con `isIncomplete` y `missingFields`.

### Fase 2 · Motor de calendario y cálculos (puntos 5 a 8)

- Servicio de cálculo puro (apartado 5).
- Campos calculados en los gastos, `GET /api/summary`, `GET /api/forecast`, ajustes y `POST /api/expenses/preview`.
- **Tests:**
  - todos los resultados del apartado 6 con `FakeTimeProvider` en 2026-11-20;
  - fin de mes (31 de enero → 28 de febrero → 31 de marzo) y 29 de febrero anual;
  - prueba gratuita: excluida de totales e incluida en la previsión desde su fin, con el evento `TrialEnd`;
  - fecha límite ya pasada → la del ciclo siguiente;
  - nómina el día 31 en febrero → último día del mes;
  - hoy es día de nómina → cuenta la siguiente;
  - los gastos pausados y dados de baja no cuentan.

### Fase 3 · Catálogo (punto 10)

- JSON embebido con los 33 elementos y `kindHints`, búsqueda, y valores por defecto al crear con `catalogId`.
- **Tests:**
  - el catálogo valida: ids únicos, tipos, colores `#RRGGBB` e iconos válidos;
  - "iber" encuentra Luz por el alias o patrón de Iberdrola;
  - la búsqueda ignora tildes;
  - "MOVISTAR PLUS" gana a "MOVISTAR";
  - "GOOGLE *GOOGLE STORAGE" va a Google One, "GOOGLE *GOOGLE" a YouTube Premium y "GOOGLE *ADS" a `google`;
  - "XFERA MOVILES SAU" y "ORANGE ESPAGNE SAU" van a Fibra y móvil; "ENERGIA XXI" va a Luz;
  - "APPLE.COM/BILL" va a `apple`, nunca a `icloud`;
  - `DIR` no coincide dentro de "DIRECCION" ni `AXA` dentro de "TAXA".

### Fase 4 · Importador (punto 9)

- Lectura, cabecera, asignación por contenido, normalización, detección, sesiones, commit y perfiles.
- **Ficheros de prueba sintéticos** en `tests/Fixtures/imports/`. Genera los Excel en los tests con **ClosedXML**, solo en el proyecto de tests. Variantes:
  1. Excel con 3 filas de datos de cuenta antes de la cabecera, "Fecha operación / Fecha valor / Concepto / Importe / Saldo" e importes negativos.
  2. CSV con `;`, en Windows-1252, con tildes en los conceptos e importes "1.234,56".
  3. Columnas "Cargo" y "Abono" separadas.
  4. Cabeceras en inglés (Date, Description, Amount, Balance).
  5. Cabeceras en catalán (Data, Concepte, Import, Saldo).
  6. Sin cabecera.
  7. Fechas como número de serie de Excel e importes como texto.
  8. Importe siempre positivo con columna D/H.
  9. Dos ficheros solapados: deduplicación.
  10. El extracto de junio a noviembre de 2026 del prototipo. Debe detectar 11 recurrentes (luz, gas, agua bimestral, fibra, comunidad trimestral, IBI, gimnasio, extraescolares, Netflix, Spotify y Apple, que el usuario renombrará a iCloud+), mandar Mercadona y "Bizum a Laura" a "probablemente no son gastos fijos" y sugerir los anuales del catálogo que falten.
- Cuando Manuel aporte extractos reales anonimizados, añádelos como pruebas.
- **Tests además:**
  - el saldo se identifica por la relación con el importe, aunque se llame "Columna5";
  - un perfil guardado se reutiliza;
  - la sesión caduca;
  - el commit no duplica cobros al reimportar;
  - `priceChangeCandidates` aparece si cambia el importe de un fijo;
  - la luz y el gas de la misma compañía con el mismo concepto salen como dos series, una de luz y otra de gas si el concepto lo indica;
  - dos suscripciones de Apple con importes distintos salen como dos series;
  - un concepto que cambia de texto a mitad de periodo y coincide con el mismo elemento del catálogo se une en una sola serie.

### Fase 5 · Calendario ICS (punto 11)

- **Tests:**
  - estructura válida;
  - plegado a 75 octetos con caracteres multibyte;
  - escapes;
  - `UID` estable entre dos generaciones;
  - `TRIGGER:-P6DT15H` con 7 días y las 9:00;
  - sin `VALARM` si los avisos están desactivados;
  - los incompletos y no activos no aparecen.

### Fase 6 · Copia de seguridad y Wallos (punto 12)

- **Tests:**
  - exportar y restaurar da los mismos datos;
  - una restauración que falla a medias no cambia nada;
  - se crea la copia del `.db` antes de restaurar;
  - Wallos en formato A y B;
  - moneda distinta de EUR → aviso;
  - ciclos "Every 3 Months" y "Quarterly" → 3 meses;
  - inactivo → dado de baja.

### Fase 7 · Demo (punto 13)

- **Tests:**
  - con gastos → 409;
  - las cifras de la demo coinciden con el apartado 6 aplicando el desplazamiento de fechas;
  - salir borra todo lo `IsDemo` y deja intactos los gastos no demo;
  - la copia de seguridad no incluye demo.

### Fase 8 · Frontend

Pantallas del apartado 12, por este orden:

1. Resumen
2. Detalle
3. Nuevo gasto / Editar
4. Previsión
5. Importar
6. Tu foto
7. Bienvenida
8. Lo mínimo del apartado 12.2

Respeta `prefers-reduced-motion` y la accesibilidad descrita en la especificación del Dashboard.

## 14. Criterios de aceptación globales de la V1

- [ ] Los 13 puntos del apartado 2.1 funcionan de punta a punta y nada del apartado 2.2 está implementado.
- [ ] Todos los resultados del apartado 6 se cumplen en tests automáticos.
- [ ] La app no hace ninguna petición de red saliente (compruébalo con un test o revisando las dependencias).
- [ ] Ningún log contiene conceptos, importes ni contenido de ficheros importados.
- [ ] La API solo responde en `127.0.0.1` y rechaza otros `Host`.
- [ ] Ningún texto de la interfaz cita leyes ni da instrucciones legales.
- [ ] Antes de cada migración y de cada restauración se guarda una copia del `.db`.
- [ ] `LICENSE`, `CLA.md` y `CONTRIBUTING.md` están en la raíz; todos los ficheros de código llevan la cabecera SPDX; todas las dependencias tienen licencia compatible y están en `THIRD-PARTY-NOTICES.md`.
- [ ] La app enseña en "Acerca de" la licencia y el enlace al código fuente.

## 15. Pendiente de Manuel (no bloquea)

- Poner su nombre completo y su correo en `CLA.md`, en el README y en la cabecera SPDX (sustituir `[NOMBRE Y APELLIDOS]`, `[FULL NAME]` y `[EMAIL]`).
- Al publicar el repositorio: subir `CLA.md` a un Gist público y enlazarlo con el repositorio en cla-assistant.io.
- Aportar extractos reales anonimizados de varios bancos para las pruebas del importador.
- Aportar un JSON exportado de Wallos para verificar el mapeo.
- Revisar y afinar los `bankPatterns` del catálogo con cargos reales.

## 16. Licencia y contribuciones

### 16.1 Decisión

- Licencia: **GNU Affero General Public License v3.0 o posterior** (identificador SPDX `AGPL-3.0-or-later`).
- Acuerdo de contribución (**CLA**): quien contribuye conserva su autoría y da permiso al mantenedor para distribuir su aportación bajo la AGPL y también bajo otras licencias, incluidas comerciales. A cambio, el mantenedor se compromete a mantener las aportaciones disponibles como código abierto.
- El titular del copyright es Manuel (`[NOMBRE Y APELLIDOS]` hasta que lo sustituya).

### 16.2 Ficheros en la raíz del repositorio

- `LICENSE`: texto oficial de la AGPL-3.0, sin modificar. Te lo pasa Manuel; no lo reescribas.
- `CLA.md` y `CONTRIBUTING.md`: te los pasa Manuel. Cópialos tal cual.
- `THIRD-PARTY-NOTICES.md`: lista de dependencias (backend y frontend) con su versión y licencia. Mantenla al añadir o quitar dependencias. Las fuentes tipográficas (Fraunces y Geist, licencia OFL) también van aquí.

### 16.3 README y cabeceras

Añade al README esta sección:

```markdown
## Licencia

Subscription Manager es software libre: puedes usarlo, estudiarlo, modificarlo y redistribuirlo bajo los términos de la GNU Affero General Public License v3.0 o posterior (AGPL-3.0-or-later). Consulta el fichero [LICENSE](LICENSE).

Si modificas la aplicación y la ofreces a otras personas a través de una red, la AGPL te obliga a poner a su disposición el código fuente de tu versión.

Las contribuciones requieren aceptar el [acuerdo de contribución](CLA.md). Consulta [CONTRIBUTING.md](CONTRIBUTING.md).

Si necesitas usar Subscription Manager en condiciones distintas de la AGPL, escribe a [EMAIL].

Copyright (C) 2026 [NOMBRE Y APELLIDOS]
```

Cada fichero de código (C#, TypeScript, CSS…) empieza con esta cabecera, adaptando el formato de comentario al lenguaje:

```
// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 [NOMBRE Y APELLIDOS]
```

Los JSON no admiten comentarios: el catálogo queda cubierto por `LICENSE`.

### 16.4 Enlace al código fuente (AGPL, sección 13)

La AGPL exige que, si alguien modifica la app y la ofrece por red, los usuarios puedan obtener el código. Para que las versiones derivadas lo cumplan fácilmente:

- El menú "Acerca de" muestra la versión, "Licencia: AGPL-3.0-or-later" y un enlace "Código fuente" a la URL del repositorio.
- La URL va en una constante de configuración (`SourceCodeUrl`) que quien haga una versión derivada debe cambiar por la suya.
- Es solo un enlace que abre el navegador: no hace ninguna petición desde la app.

### 16.5 Dependencias

- **Permitidas:** MIT, BSD, Apache-2.0, ISC, MPL-2.0, LGPL, GPL-3.0 o AGPL-3.0, y OFL para fuentes.
- **Prohibidas:** GPL-2.0-only, SSPL, Commons Clause, licencias no comerciales o de código visible (PolyForm, BSL, FSL…) y software privativo. Por ejemplo, **EPPlus** desde la versión 5.
- Las previstas en esta especificación son compatibles:
  - ExcelDataReader (MIT);
  - CsvHelper (bajo Apache-2.0);
  - ClosedXML (MIT, solo en tests);
  - React, Vite, Tailwind CSS y Framer Motion (MIT).

---

## Decisiones ya resueltas con Manuel (2026-11-21)

- **Unicidad de `RecurringExpense.Name`**: sin restricción (ver apartado 4.3).
- **Ficheros de licencia y URL del repositorio**: Manuel los aporta antes de empezar la Fase 0.
