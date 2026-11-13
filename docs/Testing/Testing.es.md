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

<p align="center"><sub>Última actualización: 2026-11-13</sub></p>
