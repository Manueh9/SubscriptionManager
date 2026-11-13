# 🌐 Referencia de la API

[🇬🇧 English](./Api.en.md) | 🇪🇸 **Español**

> Endpoints HTTP que expone el backend (API Web de ASP.NET Core), qué espera recibir cada uno y qué devuelve, incluyendo los códigos de estado. Ver [Base de Datos](../Database/Database.es.md) para el esquema subyacente.

---

## 📋 Índice

1. [Notas generales](#notas-generales)
2. [Categories](#-categories)
3. [Subscriptions](#-subscriptions)
4. [Price Histories](#-price-histories)
5. [Referencia de códigos de estado](#-referencia-de-códigos-de-estado)

---

## ℹ️ Notas generales <a id="notas-generales"></a>

- URL base (desarrollo local): `http://localhost:<puerto>/api` (el puerto sale de `Properties/launchSettings.json`).
- Todas las peticiones/respuestas usan `Content-Type: application/json`.
- Actualmente **no hay autenticación** — la app es local-first y mono-usuario (ver [Alcance del Proyecto](../ProjectScope/ProjectScope.es.md)).
- ✅ Dos manejadores globales de excepciones (registrados vía `app.UseExceptionHandler()`) convierten los fallos esperados en códigos de estado correctos en vez de un `500` sin control: `ArgumentExceptionHandler` traduce los fallos de las validaciones del dominio (`ArgumentException`/`ArgumentOutOfRangeException` — un `name` vacío, un `price` negativo, cambiar el precio al mismo valor) en `400 Bad Request`; `DbUpdateExceptionHandler` traduce las violaciones de restricciones de la base de datos (`DbUpdateException` — p. ej. borrar una categoría que todavía tiene suscripciones) en `409 Conflict`. Ambos devuelven un cuerpo `ProblemDetails`.
- ✅ Los conflictos de nombre duplicado (`409`) ya están manejados de forma segura, incluso ante peticiones concurrentes: el controller comprueba primero si ya existe un duplicado, y la restricción única de la base de datos (capturada por `DbUpdateExceptionHandler` si dos peticiones llegan a la vez y superan esa comprobación) actúa como red de seguridad — en cualquiera de los dos casos se devuelve `409`, nunca un `500` sin control.

---

## 📁 Categories

### `GET /api/Categories`

Devuelve todas las categorías.

**Respuesta `200 OK`**
```json
[
  {
    "id": 1,
    "name": "Streaming",
    "createdAt": "2026-11-01T10:00:00Z"
  }
]
```

---

### `GET /api/Categories/{id}`

Devuelve una categoría concreta por su id.

**Respuesta `200 OK`**
```json
{
  "id": 1,
  "name": "Streaming",
  "createdAt": "2026-11-01T10:00:00Z"
}
```

**Respuesta `404 Not Found`** — no existe ninguna categoría con ese `id` (cuerpo vacío).

---

### `POST /api/Categories`

Crea una categoría.

**Cuerpo de la petición**
```json
{
  "name": "Streaming"
}
```

**Respuesta `201 Created`**
Cabecera `Location: /api/Categories/{id}`
```json
{
  "id": 1,
  "name": "Streaming",
  "createdAt": "2026-11-01T10:00:00Z"
}
```

**Respuesta `409 Conflict`** — ya existe una categoría con ese `name` (sin distinguir mayúsculas/minúsculas).

**Respuesta `400 Bad Request`** — si `name` está vacío/en blanco.

---

### `PUT /api/Categories/{id}`

Renombra una categoría existente.

**Cuerpo de la petición**
```json
{
  "name": "Streaming & VOD"
}
```

**Respuesta `204 No Content`** — actualización correcta, sin cuerpo.

**Respuesta `404 Not Found`** — no existe ninguna categoría con ese `id`.

**Respuesta `409 Conflict`** — ya existe otra categoría con ese `name` (sin distinguir mayúsculas/minúsculas).

**Respuesta `400 Bad Request`** — si `name` está vacío/en blanco.

---

### `DELETE /api/Categories/{id}`

Elimina una categoría.

**Respuesta `204 No Content`** — borrado correcto, sin cuerpo.

**Respuesta `404 Not Found`** — no existe ninguna categoría con ese `id`.

**Respuesta `409 Conflict`** — si la categoría todavía tiene suscripciones asignadas. La base de datos aplica `ON DELETE RESTRICT` en `Subscriptions.CategoryId` (ver [Base de Datos](../Database/Database.es.md#-relaciones)); la `DbUpdateException` resultante la traduce a `409` `DbUpdateExceptionHandler` (ver [Notas generales](#notas-generales)).

---

## 📁 Subscriptions

### `GET /api/Subscriptions`

Devuelve todas las suscripciones.

**Respuesta `200 OK`**
```json
[
  {
    "id": 1,
    "name": "Netflix",
    "description": "Streaming service",
    "price": 12.99,
    "billingCycle": 1,
    "startDate": "2026-01-01T00:00:00Z",
    "endDate": null,
    "createdAt": "2026-11-01T10:00:00Z",
    "updatedAt": "2026-11-01T10:00:00Z",
    "status": 0,
    "categoryId": 1
  }
]
```
> `billingCycle` y `status` son enums serializados como números enteros — ver [Base de Datos: Referencia de enums](../Database/Database.es.md#-referencia-de-enums) para la equivalencia de valores.

---

### `GET /api/Subscriptions/{id}`

Devuelve una suscripción concreta por su id.

**Respuesta `200 OK`** — misma forma que arriba (un único objeto).

**Respuesta `404 Not Found`** — no existe ninguna suscripción con ese `id`.

---

### `POST /api/Subscriptions`

Crea una suscripción.

**Cuerpo de la petición**
```json
{
  "name": "Netflix",
  "description": "Streaming service",
  "price": 12.99,
  "billingCycle": 1,
  "startDate": "2026-01-01T00:00:00Z",
  "categoryId": 1
}
```

**Respuesta `201 Created`**
Cabecera `Location: /api/Subscriptions/{id}`
```json
{
  "id": 1,
  "name": "Netflix",
  "description": "Streaming service",
  "price": 12.99,
  "billingCycle": 1,
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": null,
  "createdAt": "2026-11-01T10:00:00Z",
  "updatedAt": "2026-11-01T10:00:00Z",
  "status": 0,
  "categoryId": 1
}
```

**Respuesta `400 Bad Request`** — el `categoryId` no corresponde a ninguna categoría existente, o `name` está vacío/en blanco, o `price` es negativo.

**Respuesta `409 Conflict`** — ya existe una suscripción con ese `name` en esa categoría (sin distinguir mayúsculas/minúsculas).

---

### `PUT /api/Subscriptions/{id}`

Actualiza los datos de una suscripción. **El `price` no es editable aquí a propósito** — solo debería cambiar a través de `PUT /api/Subscriptions/{id}/price` (más abajo), para que cada cambio quede registrado en [Price Histories](#-price-histories).

**Cuerpo de la petición**
```json
{
  "name": "Netflix Premium",
  "description": "Updated plan",
  "billingCycle": 1,
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": null,
  "categoryId": 1
}
```

**Respuesta `204 No Content`** — actualización correcta, sin cuerpo.

**Respuesta `404 Not Found`** — no existe ninguna suscripción con ese `id`.

**Respuesta `400 Bad Request`** — el `categoryId` no corresponde a ninguna categoría existente, o `name` está vacío/en blanco.

**Respuesta `409 Conflict`** — ya existe otra suscripción con ese `name` en esa categoría.

---

### `DELETE /api/Subscriptions/{id}`

Elimina una suscripción. Sus registros de `PriceHistory` se eliminan junto con ella (`ON DELETE CASCADE`, ver [Base de Datos](../Database/Database.es.md#-relaciones)).

**Respuesta `204 No Content`** — borrado correcto, sin cuerpo.

**Respuesta `404 Not Found`** — no existe ninguna suscripción con ese `id`.

---

### `PUT /api/Subscriptions/{id}/price`

Cambia el precio de una suscripción y registra el cambio como una nueva entrada en [Price Histories](#-price-histories). Esta es la **única** forma de cambiar el `price` — el `PUT /api/Subscriptions/{id}` general de arriba lo rechaza a propósito.

**Cuerpo de la petición**
```json
{
  "newPrice": 14.99
}
```

**Respuesta `200 OK`** — la suscripción con su `price` y `updatedAt` actualizados.
```json
{
  "id": 1,
  "name": "Netflix",
  "description": "Streaming service",
  "price": 14.99,
  "billingCycle": 1,
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": null,
  "createdAt": "2026-11-01T10:00:00Z",
  "updatedAt": "2026-11-04T10:00:00Z",
  "status": 0,
  "categoryId": 1
}
```

**Respuesta `404 Not Found`** — no existe ninguna suscripción con ese `id`.

**Respuesta `400 Bad Request`** — si `newPrice` es negativo o igual al precio actual.

---

## 📁 Price Histories

**Solo lectura desde el punto de vista de la API.** Los registros se crean automáticamente con `PUT /api/Subscriptions/{id}/price` de arriba — no hay `POST`/`PUT`/`DELETE` directo sobre este recurso.

### `GET /api/PriceHistories`

Devuelve todos los registros de historial de precios, opcionalmente filtrados por suscripción.

**Parámetros de consulta**

| Parámetro | Tipo | Descripción |
|---|---|---|
| `subscriptionId` | `int` (opcional) | Solo devuelve los registros de esta suscripción. |

**Respuesta `200 OK`**
```json
[
  {
    "id": 1,
    "subscriptionId": 1,
    "oldPrice": 9.99,
    "newPrice": 12.99,
    "changedAt": "2026-11-01T10:00:00Z"
  }
]
```

---

### `GET /api/PriceHistories/{id}`

Devuelve un registro de historial de precios concreto por su id.

**Respuesta `200 OK`** — misma forma que arriba (un único objeto).

**Respuesta `404 Not Found`** — no existe ningún registro con ese `id`.

---

## 🔢 Referencia de códigos de estado

| Código | Significado | Cuándo se usa |
|---|---|---|
| `200 OK` | Éxito, con cuerpo de respuesta | Peticiones `GET` que encuentran datos, y `PUT /api/Subscriptions/{id}/price` (devuelve la suscripción actualizada en vez de `204`, para que el cliente vea el nuevo precio sin un `GET` posterior). |
| `201 Created` | Recurso creado | `POST` exitoso. Incluye una cabecera `Location` apuntando al nuevo recurso. |
| `204 No Content` | Éxito, sin cuerpo | `PUT`/`DELETE` exitosos (excepto `PUT .../price`, ver arriba). |
| `400 Bad Request` | Referencia inválida o fallo de validación del dominio | `POST`/`PUT` en `Subscriptions` con un `categoryId` que no existe, un `name` vacío, un `price` negativo, o un cambio de precio al mismo valor/negativo. |
| `404 Not Found` | El recurso no existe | `GET`/`PUT`/`DELETE` con un `id` que no está en la base de datos. |
| `409 Conflict` | Recurso duplicado, o violación de una restricción de la base de datos | `POST`/`PUT` en `Categories`/`Subscriptions` con un nombre ya en uso (sin distinguir mayúsculas/minúsculas) dentro de ese ámbito, o `DELETE /api/Categories/{id}` sobre una categoría que todavía tiene suscripciones. |
| `500 Internal Server Error` | Excepción realmente sin manejar | No se espera en uso normal — todas las carencias conocidas anteriores ya están mapeadas a `400`/`409` por los manejadores de excepciones descritos en [Notas generales](#notas-generales). |

---

<p align="center"><sub>Última actualización: 2026-11-13</sub></p>
