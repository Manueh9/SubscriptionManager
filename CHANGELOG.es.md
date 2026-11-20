# 📜 Registro de Cambios

[🇬🇧 English](./CHANGELOG.md) | 🇪🇸 **Español**

Todos los cambios relevantes de este proyecto se documentan aquí. El formato sigue [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), y el versionado sigue [Semantic Versioning](https://semver.org/lang/es/).

**Esquema de versionado de este proyecto**: mientras no haya un producto usable de principio a fin (sin frontend), las versiones se quedan en `0.x.y`. **La `1.0.0` queda reservada para la primera versión que una persona pueda usar de verdad** — ver la [Hoja de Ruta](docs/Roadmap/Roadmap.es.md) para lo que falta para llegar ahí. Dentro de `0.x`, un `0.x.0` nuevo marca un hito importante (p. ej. que una fase de la hoja de ruta se complete); los `0.x.y` de parche son para arreglos más pequeños.

---

## [Sin publicar]

Nada todavía — ver la [Hoja de Ruta](docs/Roadmap/Roadmap.es.md) para lo que viene.

## [0.1.0] — 2026-11-20 — MVP de Backend

Primera foto versionada. Esto no es "todo lo construido en esta fecha" — es el punto de partida desde el que este registro empieza a hacer seguimiento, cubriendo todo lo construido desde que arrancó el proyecto. **La app es solo backend en este punto: una API REST sin interfaz de usuario.** Se puede probar con los ficheros `.http`, la suite de tests automatizados, o cualquier cliente HTTP — todavía no por una persona a través de una interfaz.

### Añadido

**Categories** (`/api/Categories`)
- Crear, listar, obtener por id, renombrar, borrar.
- Los nombres son únicos, sin distinguir mayúsculas, en toda la app.
- Borrar una categoría que todavía tiene suscripciones se rechaza (`409`), no se permite en silencio ni revienta.

**Subscriptions** (`/api/Subscriptions`)
- Crear, listar, obtener por id, actualizar, borrar.
- Cada suscripción guarda: nombre, descripción, precio, ciclo de facturación (`Day` / `Monthly` / `Yearly`), fecha de inicio, fecha de fin opcional, estado (`Active` / `Paused` / `Cancelled`), y su categoría.
- Los nombres son únicos por categoría (sin distinguir mayúsculas), no globalmente — el mismo nombre de servicio se permite en dos categorías distintas.
- `price` y `status` **no** son editables a través de la actualización general — pasan por sus propios endpoints dedicados de abajo, así que cada cambio es deliberado y (en el caso del precio) queda registrado.
- `PUT /api/Subscriptions/{id}/price` — cambia el precio y registra automáticamente el cambio en el historial de precios. Rechaza un precio negativo o "cambiar" al mismo precio que ya tenía.
- `PUT /api/Subscriptions/{id}/status` — cambia el estado. Rechaza cambiar al estado en el que ya está.
- `GET /api/Subscriptions/upcoming` — suscripciones activas cuya próxima renovación cae dentro de una ventana configurable (`withinDays`, 30 días por defecto), ordenadas de más próxima a más lejana. Maneja correctamente casos límite como una suscripción que empieza el día 31 de un mes, o el 29 de febrero, sin que la fecha de renovación se vaya desviando con el tiempo.

**Price Histories** (`/api/PriceHistories`, de solo lectura)
- Listar (opcionalmente filtrado a una suscripción) y obtener por id.
- Los registros se crean automáticamente por el endpoint de cambio de precio de arriba — no hay forma directa de crear, editar o borrar uno.
- Se borran automáticamente cuando se borra su suscripción.

**Expenses** (`/api/Expenses/summary`, de solo lectura)
- Coste mensual total, coste anual total, y número de suscripciones activas, a través de todas las suscripciones activas.
- Las suscripciones pausadas y canceladas no cuentan.

**Integridad de datos y manejo de errores**
- Todo campo de tipo enum (`billingCycle`, `status`) rechaza valores no definidos con un `400`, antes de que la petición llegue siquiera a la lógica de dominio.
- Los fallos de validación del dominio (un nombre vacío, un precio negativo, un cambio de precio/estado redundante) devuelven siempre `400 Bad Request`.
- Un conflicto de datos genuino (un nombre duplicado, o borrar una categoría todavía en uso) devuelve siempre `409 Conflict` — incluso cuando solo aparece como una carrera a nivel de base de datos entre dos peticiones casi simultáneas, no solo en el caso común.
- Cualquier otro fallo del servidor realmente inesperado se queda como un `500` de verdad — nunca se disfraza de un `400`/`409` que no es.

**Por debajo**
- ASP.NET Core Web API (.NET 10) + Entity Framework Core + SQLite, enteramente local — sin dependencia de la nube, sin autenticación (a propósito, ver el [Alcance del Proyecto](docs/ProjectScope/ProjectScope.es.md)).
- 56 tests automatizados (xUnit) que cubren los casos límite documentados de cada endpoint, más las piezas más delicadas (matemática de fechas de renovación, los manejadores de excepciones) de forma aislada.

### Todavía no incluido

Para que el alcance de esta versión quede sin ambigüedad:

- Ningún frontend de ningún tipo — ver la [Hoja de Ruta](docs/Roadmap/Roadmap.es.md).
- Sin autenticación — intencional, no es un hueco (esto es una app local-first y mono-usuario).
- Sin configuración de CORS — no hace falta hasta que exista un frontend que llame a la API desde un navegador.

---

<p align="center"><sub>Formato: Keep a Changelog · Versionado: Semantic Versioning</sub></p>
