# 📦 Subscription Manager

[🇬🇧 English](./README.md) | 🇪🇸 **Español**

> Aplicación local-first para gestionar suscripciones: registrar pagos recurrentes, controlar cambios de precio y visualizar el gasto mensual y anual. Funciona enteramente en tu propia máquina — no necesita ninguna infraestructura en la nube.

[Funcionalidades de la Aplicación](docs/Features/Features.es.md) describe, en lenguaje llano, las capacidades actuales de la app.

---

## 🛠 Stack Tecnológico

| Capa | Tecnología |
|---|---|
| Backend / API | ASP.NET Core Web API (.NET 10), C# |
| Persistencia | Entity Framework Core + SQLite |
| Frontend | React, TypeScript, Vite, Tailwind CSS, Framer Motion |
| Testing | xUnit |

## 🚦 Estado del Proyecto

**Versión actual: `0.1.0`** — ver el [Registro de Cambios](CHANGELOG.es.md) para ver exactamente qué incluye. La `1.0.0` queda reservada para la primera versión usable por el usuario.

- ✅ **API backend** — CRUD de `Categories` y `Subscriptions`, cambios de precio con histórico (`PriceHistories`), cambios de estado de suscripción, una vista de próximas renovaciones, y un resumen de gasto mensual/anual. Ver la [Referencia de la API](docs/Api/Api.es.md) para la lista completa de endpoints.
- 🚧 **Frontend** — todavía sin empezar. Ver la [Hoja de Ruta](docs/Roadmap/Roadmap.es.md) para lo que está planeado, en orden, y el [diseño del dashboard](docs/Design/Dashboard.es.md) ya elegido.

## ▶️ Cómo Empezar

Requiere el [SDK de .NET 10](https://dotnet.microsoft.com/download).

**Arrancar la API** (desde `backend/src/SubscriptionManager.API`):
```
dotnet run
```
La consola imprime la URL en la que está escuchando (p. ej. `http://localhost:5166`).

**Ejecutar los tests automatizados** (desde `backend/tests/SubscriptionManager.API.Tests`):
```
dotnet test
```
Ver la [Guía de Pruebas](docs/Testing/Testing.es.md) para probar los endpoints a mano.

## 📚 Documentación

| Doc | 🇬🇧 English | 🇪🇸 Español |
|---|---|---|
| Funcionalidades de la Aplicación | [English](docs/Features/Features.en.md) | [Español](docs/Features/Features.es.md) |
| Registro de Cambios | [English](CHANGELOG.md) | [Español](CHANGELOG.es.md) |
| Alcance del Proyecto | [English](docs/ProjectScope/ProjectScope.en.md) | [Español](docs/ProjectScope/ProjectScope.es.md) |
| Hoja de Ruta | [English](docs/Roadmap/Roadmap.en.md) | [Español](docs/Roadmap/Roadmap.es.md) |
| Arquitectura y Ciclo de Vida de una Petición | [English](docs/Architecture/Architecture.en.md) | [Español](docs/Architecture/Architecture.es.md) |
| Base de Datos | [English](docs/Database/Database.en.md) | [Español](docs/Database/Database.es.md) |
| Referencia de la API | [English](docs/Api/Api.en.md) | [Español](docs/Api/Api.es.md) |
| Guía de Pruebas | [English](docs/Testing/Testing.en.md) | [Español](docs/Testing/Testing.es.md) |
| Diseño — Dashboard | — | [Español](docs/Design/Dashboard.es.md) |
