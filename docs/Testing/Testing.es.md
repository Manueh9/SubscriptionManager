# 🧪 Guía de Pruebas Manuales de la API

[🇬🇧 English](./Testing.en.md) | 🇪🇸 **Español**

> Cómo probar a mano los endpoints descritos en la [Referencia de la API](../Api/Api.es.md), ya sea con el fichero `.http` incluido o con una herramienta como Bruno/Postman/Insomnia.

---

## 📋 Índice

1. [Arrancar la API](#-arrancar-la-api)
2. [Opción A — El fichero `.http`](#-opción-a--el-fichero-http)
3. [Opción B — Bruno / Postman / Insomnia](#-opción-b--bruno--postman--insomnia)
4. [Opción C — Tests automatizados](#-opción-c--tests-automatizados)
5. [Secuencia de pruebas sugerida](#-secuencia-de-pruebas-sugerida)

---

## ▶️ Arrancar la API

Desde `backend/src/SubscriptionManager.API`:

```
dotnet run
```

La consola imprime la URL en la que está escuchando, p. ej. `http://localhost:5166`. Deja esta terminal abierta — el servidor sigue corriendo hasta que lo paras (`Ctrl+C`). Todas las peticiones de abajo asumen esta URL base.

---

## 📄 Opción A — El fichero `.http`

El proyecto ya tiene `backend/src/SubscriptionManager.API/SubscriptionManager.API.http`, organizado en secciones, con peticiones ya montadas para `Categories`, `Subscriptions` (incluidos los endpoints de cambio de precio, de estado y de próximas renovaciones), el `PriceHistories` de solo lectura, y el resumen de `Expenses` de solo lectura — cubriendo el camino feliz más los casos documentados de `404`/`400`/`409`.

Hay un segundo fichero, `SubscriptionManager.API.UpcomingRenewals.http`, separado por ser largo y muy específico: ejercita a fondo `GET /api/Subscriptions/upcoming` — una fecha de inicio en el futuro, el ciclo `Day`, una suscripción `Monthly` que empieza el día 31 y otra `Yearly` que empieza el 29 de febrero (justo los casos límite para los que se arregló el cálculo de la fecha de renovación), excluir suscripciones pausadas, excluir las que ya tienen `EndDate` pasado, y el límite de `withinDays`.

1. Ábrelo en **Rider** o **VS Code** (con la extensión *REST Client*).
2. Pulsa "Send Request" (o la ▶️ que aparece encima de cada bloque) en cada petición separada por `###`, de arriba a abajo.
3. Las peticiones que crean algo tienen nombre (`# @name createCategory`, `# @name createSubscription`, ...), y las de abajo reutilizan automáticamente el `id` que devuelven (p. ej. `{{createCategory.response.body.$.id}}`) — así que ejecutando una sección en orden se encadenan crear → obtener → actualizar → borrar sin que tengas que copiar ningún id a mano.

---

## 📮 Opción B — Bruno / Postman / Insomnia

Todavía no hay una colección exportada, así que las peticiones se montan a mano:

1. Crea una petición nueva.
2. Pon el método (`GET`/`POST`/`PUT`/`DELETE`) y la URL, p. ej. `http://localhost:5166/api/Categories`.
3. Para `POST`/`PUT`, añade una cabecera `Content-Type: application/json` y pega el cuerpo JSON (ver ejemplos en la [Referencia de la API](../Api/Api.es.md)).
4. Envía, y comprueba el código de estado contra lo documentado ahí.

Ejemplo — crear una categoría:
```
POST http://localhost:5166/api/Categories
Content-Type: application/json

{ "name": "Streaming" }
```

---

## 🤖 Opción C — Tests automatizados

`backend/tests/SubscriptionManager.API.Tests` es un proyecto xUnit que ejercita los mismos endpoints a través de `WebApplicationFactory`, contra una base de datos SQLite en memoria aislada (no hace falta arrancar el servidor a mano). Desde `backend/tests/SubscriptionManager.API.Tests`:

```
dotnet test
```

Hoy cubre el camino feliz de `Categories`, `Subscriptions`, `PriceHistories` y `Expenses`, más los casos límite `400`/`404`/`409` documentados en la [Referencia de la API](../Api/Api.es.md) — incluidas comprobaciones de regresión de los dos manejadores globales de excepciones (`ArgumentExceptionHandler` para las validaciones de nombre vacío/precio negativo/mismo precio/mismo estado, y `DbUpdateExceptionHandler` para borrar una categoría que todavía tiene suscripciones), la conversión de coste diario/mensual/anual que usa el resumen de gastos, el cálculo de fecha de renovación (límites de la ventana, suscripciones pausadas, suscripciones terminadas) que usa `/api/Subscriptions/upcoming`, y la validación `[EnumDataType]` que rechaza un `billingCycle`/`newStatus` no definido.

Algunos ficheros de test se saltan HTTP por completo, porque lo que cubren se testea mejor como entrada/salida simple sobre una pieza de lógica aislada que dando toda la vuelta:

- `SubscriptionRenewalDateTests` llama directamente a `Subscription.GetNextRenewalDate` — sin servidor, sin base de datos — cubriendo cada rama (estado, fecha de inicio en el futuro, cada ciclo de facturación, el caso límite del recorte del día 31/29 de febrero, el tope de `EndDate`) de forma mucho más exhaustiva de lo que sería práctico por HTTP.
- `DbUpdateExceptionHandlerTests` llama directamente al manejador con un `DbUpdateException` construido a propósito, confirmando que reclama una violación real de `SQLITE_CONSTRAINT` (`409`) pero deja sin capturar un error transitorio como una base de datos bloqueada (así que sigue dando `500`) — una distinción que no es práctica de provocar con una sola petición HTTP manual.
- `TrySaveChangesTests` ejercita el helper de guardado compartido de los controllers contra una base de datos SQLite en memoria real (sin HTTP), así que además verifica la premisa en la que se apoya el helper: que SQLite realmente informa de un nombre duplicado, un duplicado sin distinguir mayúsculas, y un borrado con `RESTRICT` como violaciones de restricción. Usa a propósito un `DbContext` nuevo por operación, imitando la vida real por petición — compartir un solo contexto dejaría que el *change tracker* de EF se adelantara al `RESTRICT` con su propia excepción antes de que SQLite llegue a ver el borrado.
- `DatabaseMigrationServiceTests` deja una base de datos real (en una carpeta temporal) congelada en la primera migración, para confirmar que arrancar con una migración pendiente hace copia de seguridad antes de migrar y conserva solo las 10 copias más recientes; y que si no hay ninguna migración pendiente, no toca nada.
- `StartupSecurityTests` llama directamente a `StartupSecurity.EnsureLoopbackOnly` con distintas URLs, confirmando que acepta `localhost`/`127.0.0.1`/`::1` y rechaza cualquier otra cosa (`0.0.0.0`, una IP de red, `*`, `+`).

`UpcomingRenewalsEndpointsTests` conserva un test de regresión a nivel HTTP para el caso del día 31, confirmando que el controller conecta correctamente de principio a fin con el cálculo de dominio (ya testeado a fondo por separado).

`HostFilteringTests` sí es a nivel HTTP a propósito: comprueba que una petición de verdad con `Host: evil.example` recibe `400` antes de llegar a ningún controller, y que una con `Host: localhost` sigue funcionando con normalidad.

`ExpensesEndpointsTests` y `UpcomingRenewalsEndpointsTests` le dan a cada test su propia `WebApiFactory` (y por tanto su propia base de datos vacía) en vez de compartir una vía `IClassFixture`, porque ambos endpoints agregan todas las suscripciones de la base de datos y de lo contrario verían datos dejados por tests hermanos.

---

## ✅ Secuencia de pruebas sugerida

Reproduce el flujo que ya verificamos mientras construíamos la API — un buen orden para ir probando a mano, copiando cada `id` de una respuesta a la siguiente petición:

1. `POST /api/Categories` → crea "Streaming". Esperado `201`.
2. `POST /api/Categories` otra vez con el **mismo nombre** (prueba también con mayúsculas distintas, p. ej. "streaming"). Esperado `409`.
3. `POST /api/Subscriptions` → crea "Netflix" usando el id de categoría del paso 1. Esperado `201`.
4. `POST /api/Subscriptions` otra vez con el mismo nombre **y** la misma categoría. Esperado `409`.
5. `POST /api/Subscriptions` con el mismo nombre pero una categoría **distinta** (crea una segunda categoría primero). Esperado `201` — permitido, porque la unicidad es por categoría.
6. `POST /api/Subscriptions` con `"billingCycle": 999` (no es un valor definido). Esperado `400`.
7. `GET /api/Subscriptions/{id}` con el id del paso 3. Esperado `200`.
8. `GET /api/Subscriptions/upcoming` → esperado `200`; esta suscripción debería aparecer (está `Active`, y su `startDate` debería estar dentro de la ventana por defecto de 30 días, salvo que hayas puesto una más lejana).
9. `PUT /api/Subscriptions/{id}` → cambia el nombre/fechas (el precio no es editable aquí a propósito). Esperado `204`.
10. `PUT /api/Subscriptions/{id}/price` con `{ "newPrice": 14.99 }` → esperado `200` con la suscripción actualizada.
11. `PUT /api/Subscriptions/{id}/price` otra vez con el **mismo** precio → esperado `400`.
12. `GET /api/PriceHistories?subscriptionId={id}` → esperado `200` con **un** registro mostrando el precio viejo/nuevo del paso 10.
13. `GET /api/Expenses/summary` → esperado `200`; el total debería incluir los `14.99`/mes de esta suscripción.
14. `PUT /api/Subscriptions/{id}/status` con `{ "newStatus": 1 }` (Paused) → esperado `200` con la suscripción actualizada.
15. `PUT /api/Subscriptions/{id}/status` otra vez con el **mismo** estado → esperado `400`.
16. `PUT /api/Subscriptions/{id}/status` con `{ "newStatus": 999 }` (no es un valor definido) → esperado `400`.
17. `GET /api/Expenses/summary` otra vez → esperado `200`; el total ya no debería incluir esta suscripción, porque ahora está `Paused`.
18. `GET /api/Subscriptions/upcoming` otra vez → esperado `200`; esta suscripción tampoco debería aparecer ya, por la misma razón.
19. `DELETE /api/Categories/{id}` usando el id del paso 1, mientras la suscripción del paso 3 todavía existe → esperado `409`.
20. `DELETE /api/Subscriptions/{id}`. Esperado `204`.
21. `DELETE /api/Categories/{id}` (la del paso 1, ya sin suscripciones). Esperado `204`.
22. `GET /api/Categories/999999` (un id que no existe). Esperado `404`.
23. `POST /api/Categories` con `"name": ""`. Esperado `400`.

---

<p align="center"><sub>Última actualización: 2026-11-21</sub></p>
