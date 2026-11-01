# 🌐 Referencia de la API

[🇬🇧 English](./Api.en.md) | 🇪🇸 **Español**

> Endpoints HTTP que expone el backend (API Web de ASP.NET Core), qué espera recibir cada uno y qué devuelve, incluyendo los códigos de estado. Ver [Base de Datos](../Database/Database.es.md) para el esquema subyacente.

---

## 📋 Índice

1. [Notas generales](#-notas-generales)
2. [Categories](#-categories)
3. [Subscriptions](#-subscriptions)
4. [Price Histories](#-price-histories)
5. [Referencia de códigos de estado](#-referencia-de-códigos-de-estado)

---

## ℹ️ Notas generales

- URL base (desarrollo local): `http://localhost:<puerto>/api` (el puerto sale de `Properties/launchSettings.json`).
- Todas las peticiones/respuestas usan `Content-Type: application/json`.
- Actualmente **no hay autenticación** — la app es local-first y mono-usuario (ver [Alcance del Proyecto](../ProjectScope/ProjectScope.es.md)).
- ⚠️ **Carencia conocida:** todavía no hay middleware global de manejo de excepciones ni atributos de validación en los DTOs. Las validaciones del dominio (p. ej. un `name` vacío) lanzan un `ArgumentException` normal, que no se captura y hoy se traduce en `500 Internal Server Error` en vez de un `400 Bad Request` correcto. Se indica en cada endpoint afectado más abajo, y debería corregirse más adelante (atributos de validación + un middleware de excepciones que devuelva `ProblemDetails`).

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

**Respuesta `500 Internal Server Error`** — si `name` está vacío/en blanco (ver la [carencia conocida](#-notas-generales) de arriba; debería ser `400 Bad Request`).

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

**Respuesta `500 Internal Server Error`** — si `name` está vacío/en blanco (ver la [carencia conocida](#-notas-generales) de arriba).

---

### `DELETE /api/Categories/{id}`

Elimina una categoría.

**Respuesta `204 No Content`** — borrado correcto, sin cuerpo.

**Respuesta `404 Not Found`** — no existe ninguna categoría con ese `id`.

**Respuesta `500 Internal Server Error`** — si la categoría todavía tiene suscripciones asignadas. La base de datos aplica `ON DELETE RESTRICT` en `Subscriptions.CategoryId` (ver [Base de Datos](../Database/Database.es.md#-relaciones)), y el controller todavía no captura ese caso — se traduce en una `DbUpdateException` sin manejar en vez de un `409 Conflict` limpio.

---

## 📁 Subscriptions

🚧 **Pendiente de implementar.** `SubscriptionsController` existe como fichero vacío — todavía no tiene endpoints.

---

## 📁 Price Histories

🚧 **Pendiente de implementar.** `PriceHistoriesController` existe como fichero vacío — todavía no tiene endpoints.

---

## 🔢 Referencia de códigos de estado

| Código | Significado | Cuándo se usa |
|---|---|---|
| `200 OK` | Éxito, con cuerpo de respuesta | Peticiones `GET` que encuentran datos. |
| `201 Created` | Recurso creado | `POST` exitoso. Incluye una cabecera `Location` apuntando al nuevo recurso. |
| `204 No Content` | Éxito, sin cuerpo | `PUT`/`DELETE` exitosos. |
| `404 Not Found` | El recurso no existe | `GET`/`PUT`/`DELETE` con un `id` que no está en la base de datos. |
| `500 Internal Server Error` | Excepción sin manejar | Hoy también cubre fallos de validación y de restricciones de clave foránea — ver la [carencia conocida](#-notas-generales). |

---

<p align="center"><sub>Última actualización: 2026-11-01</sub></p>
