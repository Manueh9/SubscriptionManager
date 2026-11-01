# 🧪 Guía de Pruebas Manuales de la API

[🇬🇧 English](./Testing.en.md) | 🇪🇸 **Español**

> Cómo probar a mano los endpoints descritos en la [Referencia de la API](../Api/Api.es.md), ya sea con el fichero `.http` incluido o con una herramienta como Bruno/Postman/Insomnia.

---

## 📋 Índice

1. [Arrancar la API](#-arrancar-la-api)
2. [Opción A — El fichero `.http`](#-opción-a--el-fichero-http)
3. [Opción B — Bruno / Postman / Insomnia](#-opción-b--bruno--postman--insomnia)
4. [Secuencia de pruebas sugerida](#-secuencia-de-pruebas-sugerida)

---

## ▶️ Arrancar la API

Desde `backend/src/SubscriptionManager.API`:

```
dotnet run
```

La consola imprime la URL en la que está escuchando, p. ej. `http://localhost:5166`. Deja esta terminal abierta — el servidor sigue corriendo hasta que lo paras (`Ctrl+C`). Todas las peticiones de abajo asumen esta URL base.

---

## 📄 Opción A — El fichero `.http`

El proyecto ya tiene `backend/src/SubscriptionManager.API/SubscriptionManager.API.http` con peticiones ya montadas para los endpoints de `Categories` (listar, crear, obtener, actualizar, borrar, un caso de 404 y el caso conocido de 500 con nombre vacío).

1. Ábrelo en **Rider** o **VS Code** (con la extensión *REST Client*).
2. Pulsa "Send Request" (o la ▶️ que aparece encima de cada bloque) en cada petición separada por `###`, de arriba a abajo.
3. La petición `createCategory` tiene nombre (`# @name createCategory`), y las de abajo reutilizan automáticamente el `id` que devuelve (`{{createCategory.response.body.$.id}}`) — así que ejecutándolas en orden se encadenan crear → obtener → actualizar → borrar sin que tengas que copiar ningún id a mano.

Este fichero por ahora solo cubre `Categories`. Puedes añadir tus propios bloques para `Subscriptions` y `PriceHistories` siguiendo el mismo patrón — mira la [Referencia de la API](../Api/Api.es.md) para la forma exacta de cada petición/respuesta.

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

## ✅ Secuencia de pruebas sugerida

Reproduce el flujo que ya verificamos mientras construíamos la API — un buen orden para ir probando a mano, copiando cada `id` de una respuesta a la siguiente petición:

1. `POST /api/Categories` → crea "Streaming". Esperado `201`.
2. `POST /api/Categories` otra vez con el **mismo nombre** (prueba también con mayúsculas distintas, p. ej. "streaming"). Esperado `409`.
3. `POST /api/Subscriptions` → crea "Netflix" usando el id de categoría del paso 1. Esperado `201`.
4. `POST /api/Subscriptions` otra vez con el mismo nombre **y** la misma categoría. Esperado `409`.
5. `POST /api/Subscriptions` con el mismo nombre pero una categoría **distinta** (crea una segunda categoría primero). Esperado `201` — permitido, porque la unicidad es por categoría.
6. `GET /api/Subscriptions/{id}` con el id del paso 3. Esperado `200`.
7. `PUT /api/Subscriptions/{id}` → cambia el nombre/fechas (el precio no es editable aquí a propósito). Esperado `204`.
8. `GET /api/PriceHistories?subscriptionId={id}` → esperado `200` con una lista vacía (todavía no existe ningún cambio de precio — esa funcionalidad no está implementada).
9. `DELETE /api/Subscriptions/{id}`. Esperado `204`.
10. `DELETE /api/Categories/{id}` (la del paso 1, ya sin suscripciones). Esperado `204`.
11. `GET /api/Categories/999999` (un id que no existe). Esperado `404`.
12. `POST /api/Categories` con `"name": ""`. Esperado `500` — es la [carencia conocida](../Api/Api.es.md#known-gap) ya documentada, no un bug que tengas que arreglar.

---

<p align="center"><sub>Última actualización: 2026-11-01</sub></p>
