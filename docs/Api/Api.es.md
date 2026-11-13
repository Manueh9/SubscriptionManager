# 🌐 Referencia de la API

[🇬🇧 English](./Api.en.md) | 🇪🇸 **Español**

> Endpoints HTTP que expone el backend (API Web de ASP.NET Core), qué espera recibir cada uno y qué devuelve, incluyendo los códigos de estado. Ver [Base de Datos](../Database/Database.es.md) para el esquema subyacente.

---

## 📋 Índice

1. [Notas generales](#known-gap)
2. [Categories](#-categories)
3. [Subscriptions](#-subscriptions)
4. [Price Histories](#-price-histories)
5. [Referencia de códigos de estado](#-referencia-de-códigos-de-estado)

---

## ℹ️ Notas generales <a id="known-gap"></a>

- URL base (desarrollo local): `http://localhost:<puerto>/api` (el puerto sale de `Properties/launchSettings.json`).
- Todas las peticiones/respuestas usan `Content-Type: application/json`.
- Actualmente **no hay autenticación** — la app es local-first y mono-usuario (ver [Alcance del Proyecto](../ProjectScope/ProjectScope.es.md)).
- ⚠️ **Carencia conocida:** todavía no hay middleware global de manejo de excepciones ni atributos de validación en los DTOs. Las validaciones del dominio (p. ej. un `name` vacío) lanzan un `ArgumentException` normal, que no se captura y hoy se traduce en `500 Internal Server Error` en vez de un `400 Bad Request` correcto. Se indica en cada endpoint afectado más abajo, y debería corregirse más adelante (atributos de validación + un middleware de excepciones que devuelva `ProblemDetails`).
- ✅ Los conflictos de nombre duplicado (`409`) ya están manejados de forma segura, incluso ante peticiones concurrentes: el controller comprueba primero si ya existe un duplicado, y la restricción única de la base de datos actúa como red de seguridad si dos peticiones llegan a la vez y superan esa comprobación — en cualquiera de los dos casos se devuelve `409`, nunca un `500` sin control.

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

**Respuesta `500 Internal Server Error`** — si `name` está vacío/en blanco (ver la [carencia conocida](#known-gap) de arriba; debería ser `400 Bad Request`).

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

**Respuesta `500 Internal Server Error`** — si `name` está vacío/en blanco (ver la [carencia conocida](#known-gap) de arriba).

---

### `DELETE /api/Categories/{id}`

Elimina una categoría.

**Respuesta `204 No Content`** — borrado correcto, sin cuerpo.

**Respuesta `404 Not Found`** — no existe ninguna categoría con ese `id`.

**Respuesta `500 Internal Server Error`** — si la categoría todavía tiene suscripciones asignadas. La base de datos aplica `ON DELETE RESTRICT` en `Subscriptions.CategoryId` (ver [Base de Datos](../Database/Database.es.md#-relaciones)), y el controller todavía no captura ese caso — se traduce en una `DbUpdateException` sin manejar en vez de un `409 Conflict` limpio.

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

**Respuesta `400 Bad Request`** — el `categoryId` no corresponde a ninguna categoría existente.

**Respuesta `409 Conflict`** — ya existe una suscripción con ese `name` en esa categoría (sin distinguir mayúsculas/minúsculas).

**Respuesta `500 Internal Server Error`** — si `name` está vacío/en blanco o `price` es negativo (ver la [carencia conocida](#known-gap) de arriba).

---

### `PUT /api/Subscriptions/{id}`

Actualiza los datos de una suscripción. **El `price` no es editable aquí a propósito** — solo debería cambiar a través de un flujo dedicado de cambio de precio (todavía sin implementar), para que cada cambio quede registrado en [Price Histories](#-price-histories).

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

**Respuesta `400 Bad Request`** — el `categoryId` no corresponde a ninguna categoría existente.

**Respuesta `409 Conflict`** — ya existe otra suscripción con ese `name` en esa categoría.

**Respuesta `500 Internal Server Error`** — si `name` está vacío/en blanco (ver la [carencia conocida](#known-gap) de arriba).

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

**Respuesta `500 Internal Server Error`** — si `newPrice` es negativo o igual al precio actual (ver la [carencia conocida](#known-gap) de arriba; debería ser `400 Bad Request`).

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
| `400 Bad Request` | Referencia inválida en la petición | `POST`/`PUT` en `Subscriptions` con un `categoryId` que no existe. |
| `404 Not Found` | El recurso no existe | `GET`/`PUT`/`DELETE` con un `id` que no está en la base de datos. |
| `409 Conflict` | Recurso duplicado | `POST`/`PUT` en `Categories`/`Subscriptions` con un nombre ya en uso (sin distinguir mayúsculas/minúsculas) dentro de ese ámbito. |
| `500 Internal Server Error` | Excepción sin manejar | Hoy también cubre la validación de nombre vacío/precio negativo y el fallo de `RESTRICT` al borrar una categoría con suscripciones — ver la [carencia conocida](#known-gap). |

---

<p align="center"><sub>Última actualización: 2026-11-04</sub></p>
