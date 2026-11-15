# 🗺️ Hoja de Ruta

[🇬🇧 English](./Roadmap.en.md) | 🇪🇸 **Español**

> Qué está planeado a continuación, en orden, y las decisiones ya tomadas sobre cómo construirlo. Este es un **plan vivo**, no un changelog: en cuanto una fase se entrega, sus detalles pasan a la documentación permanente (Referencia de la API, Base de Datos, Arquitectura, Testing) y la entrada aquí se recorta a una línea con ✅. Actualiza este fichero cada vez que el plan cambie — que no se quede desactualizado.

---

## 📋 Índice

1. [Estado general](#-estado-general)
2. [Fase 0 — Backend (MVP)](#-fase-0--backend-mvp)
3. [Fase 1 — Backend: identidad visual de las tarjetas](#-fase-1--backend-identidad-visual-de-las-tarjetas)
4. [Fase 2 — Esqueleto del frontend](#-fase-2--esqueleto-del-frontend)
5. [Fase 3 — Dashboard estático](#-fase-3--dashboard-estático)
6. [Fase 4 — Capa de animación e interacción](#-fase-4--capa-de-animación-e-interacción)
7. [Fase 5 — Resto de pantallas](#-fase-5--resto-de-pantallas)
8. [Fase 6 — CORS](#-fase-6--cors)
9. [Decisiones ya tomadas](#-decisiones-ya-tomadas)
10. [Preguntas abiertas](#-preguntas-abiertas)

---

## 🚦 Estado general

| Fase | Qué | Estado |
|---|---|---|
| 0 | MVP de backend — CRUD de `Categories`/`Subscriptions`, cambios de precio con histórico, cambios de estado, vista de próximas renovaciones, resumen de gasto mensual/anual, manejo de errores centralizado, 56 tests automatizados (ver [Estado del Proyecto](../../README.es.md#-estado-del-proyecto) y la [Referencia de la API](../Api/Api.es.md)) | ✅ Hecho |
| 1 | Ampliaciones de backend para el diseño de tarjetas (`AccentColor`, `Icon`, `Pattern`, equivalentes calculados) | 🔲 Sin empezar |
| 2 | Esqueleto del proyecto frontend (Vite + React + TS + Tailwind + Framer Motion) | 🔲 Sin empezar |
| 3 | Dashboard con datos reales, todavía sin animación | 🔲 Sin empezar |
| 4 | Capa de animación e interacción sobre el dashboard | 🔲 Sin empezar |
| 5 | Listado de suscripciones, detalle/edición, alta, gestión de categorías | 🔲 Sin empezar |
| 6 | CORS, en cuanto se sepa el puerto real del dev server de Vite | 🔲 Sin empezar |

---

## 🧱 Fase 0 — Backend (MVP)

Todo lo de abajo ya está construido, testeado y documentado — lo mantengo aquí (en vez de recortarlo a la línea de resumen de la tabla de arriba) para que la hoja de ruta muestre el recorrido completo, no solo lo que falta.

**Dominio y endpoints** — 16 endpoints repartidos en 4 controllers:
- [x] `Categories` — listar, obtener por id, crear, renombrar, borrar.
- [x] `Subscriptions` — listar, obtener por id, crear, actualizar, borrar.
- [x] `PUT /api/Subscriptions/{id}/price` — cambia el precio y registra una entrada en `PriceHistory`; `PUT /api/Subscriptions/{id}` no puede tocar el precio a propósito.
- [x] `PUT /api/Subscriptions/{id}/status` — Active/Paused/Cancelled, rechazando un cambio al mismo valor.
- [x] `GET /api/Subscriptions/upcoming` — suscripciones activas cuya próxima renovación cae dentro de una ventana, calculada con matemática de fechas correcta (sin desviarse en meses cortos ni en años bisiestos).
- [x] `PriceHistories` — de solo lectura, listar (opcionalmente filtrado por suscripción) + obtener por id.
- [x] `GET /api/Expenses/summary` — totales mensual/anual y número de activas.
- [x] Nombres únicos sin distinguir mayúsculas: por categoría globalmente, por suscripción dentro de su categoría (colación `NOCASE` de SQLite).
- [x] `BillingCycle`/`SubscriptionStatus` validados contra sus valores de enum definidos (`[EnumDataType]`) — un entero no definido se rechaza antes de llegar a la capa de dominio.

**Robustez**:
- [x] Dos `IExceptionHandler` globales: los fallos de validación del dominio → `400`; una violación de restricción de la base de datos genuina → `409`. Un error transitorio de la BD (p. ej. un bloqueo) deliberadamente *no* se informa como conflicto — sale como un `500` real en vez de mentirle al cliente.
- [x] Esa misma comprobación de "violación de restricción" se comparte entre el manejador global y el manejo de carreras de nombre duplicado de los propios controllers (`TrySaveChangesAsync`), así que hay una sola definición de "esto es un conflicto real", no cuatro copias.
- [x] Las consultas de solo lectura usan `AsNoTracking()`.

**Persistencia**:
- [x] EF Core + SQLite, 2 migraciones aplicadas, esquema documentado al completo.

**Testing**:
- [x] 56 tests automatizados (xUnit) repartidos en 8 ficheros: tests de integración a nivel HTTP por controller, más tests unitarios puros para la matemática de fechas de renovación y los manejadores de excepciones.
- [x] Dos ficheros `.http` para pruebas manuales (el principal, más uno dedicado que cubre a fondo los casos límite de `/upcoming`).

**Documentación**: [Referencia de la API](../Api/Api.es.md), [Base de Datos](../Database/Database.es.md), [Guía de Pruebas](../Testing/Testing.es.md) y [Arquitectura y Ciclo de Vida de una Petición](../Architecture/Architecture.es.md) — todas bilingües, todas verificadas línea a línea contra el código real.

## 🧩 Fase 1 — Backend: identidad visual de las tarjetas

El diseño elegido para el dashboard (ver [Diseño: Dashboard](../Design/Dashboard.es.md)) necesita tres campos que el modelo de dominio actual no tiene, más dos valores que conviene calcular en el backend en vez de duplicar la fórmula en el frontend.

- [ ] `Subscription.AccentColor` (string hex, p. ej. `"#E12D37"`) — el color de acento de la tarjeta, elegido por el usuario.
- [ ] `Subscription.Icon` (nuevo enum de C#, igual que `BillingCycle`/`SubscriptionStatus`) — un conjunto cerrado y pequeño de iconos genéricos (`Play`, `Music`, `Terminal`, `Cloud`, `Gym`, `Wifi`, `Code`, `Box`, ...).
- [ ] `Category.Pattern` (nuevo enum de C#) — el grabado de fondo de la tarjeta. Asignado automáticamente de forma rotatoria al crear una categoría, editable después vía el `PUT /api/Categories/{id}` ya existente.
- [ ] Añadir `monthlyEquivalent` / `yearlyEquivalent` a `SubscriptionResponse` (y a `UpcomingRenewalResponse`), calculados con la misma fórmula exacta que ya usa `ExpensesController` — para que los totales de la cartera nunca puedan desviarse de `GET /api/Expenses/summary`.
- [ ] Nueva migración de EF Core para las dos columnas nuevas más la columna `Pattern`.
- [ ] Actualizar `SubscriptionCreateRequest`/`SubscriptionUpdateRequest` para aceptar `accentColor`/`icon`; actualizar `CategoryRequest` para aceptar un `pattern` opcional.
- [ ] Actualizar la [Referencia de la API](../Api/Api.es.md) y [Base de Datos](../Database/Database.es.md) cuando esto se despliegue — no antes (documentan lo que existe, no lo planeado).

No hace falta ningún endpoint de agregación nuevo: el gráfico por categoría y los totales por categoría del dashboard se calculan en el cliente a partir de `GET /api/Subscriptions` + `GET /api/Categories` — el volumen de datos (las suscripciones de una sola persona) hace que eso sea trivial.

## 🏗 Fase 2 — Esqueleto del frontend

- [ ] `npm create vite@latest frontend -- --template react-ts` dentro de la carpeta `frontend/` ya existente.
- [ ] Tailwind CSS + los tokens de diseño de [Diseño: Dashboard](../Design/Dashboard.es.md) §2 (el tema "medianoche").
- [ ] Framer Motion.
- [ ] Un cliente de API tipado para los endpoints del backend ya existentes.

## 🖥 Fase 3 — Dashboard estático

Construir el dashboard con datos reales de principio a fin, dejando la animación para después — cabecera, interruptor de periodo (sin la píldora deslizante todavía), total, la rejilla de 3 datos, el gráfico por categoría (estático, sin hover), la cartera apilada (estática, sin tilt/flip). El objetivo: un dashboard funcional y visualmente completo, solo que todavía no "vivo".

## 🎬 Fase 4 — Capa de animación e interacción

Se añade sobre la fase 3, aproximadamente en orden de impacto: reparto de entrada → deslizamiento del interruptor + contadores animados → giro de tarjeta al click → tilt/brillo al hover/expansión de la cartera → dimming del gráfico al hover + cambio de etiqueta central. Especificación completa de tiempos/easing en [Diseño: Dashboard](../Design/Dashboard.es.md) §5.

## 📱 Fase 5 — Resto de pantallas

- [ ] Listado de suscripciones — filtrable/ordenable por categoría y estado.
- [ ] Detalle/edición de suscripción — incluyendo su línea de tiempo de histórico de precios, cambio de estado (pausar/cancelar/reactivar) y cambio de precio.
- [ ] Flujo de alta de suscripción — con un catálogo de presets "servicios conocidos" solo en el frontend (nombre → color/icono por defecto, p. ej. Netflix → rojo/play) que se amplía con el tiempo sin tocar el backend; el usuario siempre puede sobreescribir la sugerencia.
- [ ] Gestión de categorías — listar/crear/renombrar/borrar, con el selector de patrón.

Ninguna de estas pantallas está diseñada todavía — cada una tendrá su propio fichero bajo `docs/Design/` cuando lo esté, igual que [Dashboard.es.md](../Design/Dashboard.es.md).

## 🌐 Fase 6 — CORS

`Program.cs` todavía no tiene `AddCors`/`UseCors`. Se añade en cuanto la fase 2 esté en marcha y se sepa el puerto real del dev server de Vite (por defecto `5173`, pero hay que confirmarlo).

---

## ✅ Decisiones ya tomadas

Decisiones de criterio tomadas al planificar esto, para que no se vuelvan a discutir más adelante:

- **Almacenamiento del color**: string hex (`"#E12D37"`), no los strings de triplete RGB del prototipo de diseño — hex es el estándar web y funciona directo con `<input type="color">`.
- **Icono**: un enum cerrado de C#, no un string libre — consistente con cómo ya están modelados `BillingCycle`/`SubscriptionStatus`, y validado de la misma forma.
- **Patrón de categoría**: un enum cerrado de C#, asignado automáticamente de forma rotatoria al crear la categoría — el usuario nunca tiene que pensarlo, pero puede cambiarlo.
- **Equivalentes mensual/anual**: calculados en el backend e incluidos en la respuesta de la suscripción, no duplicados como fórmula en el frontend — garantiza que el dashboard y `/api/Expenses/summary` nunca puedan discrepar.
- **`BillingCycle.Day`**: se trata con la misma conversión que ya usa `ExpensesController` (`× 365` al año), no una inventada para el dashboard.
- **Cartera "sin límite de ventana"**: se resuelve llamando a `GET /api/Subscriptions/upcoming` con un `withinDays` muy alto desde el frontend — sin cambios en el backend, la opción más simple por ahora.
- **Catálogo de presets "servicios conocidos"**: vive entero en el frontend como tabla estática — ya hay una lista inicial de ~20 servicios habituales, cada uno con icono y color de acento sugeridos, definida en [Diseño: Dashboard](../Design/Dashboard.es.md) §7. Ampliarlo con el tiempo nunca requiere un cambio de backend ni un despliegue.
- **Tema**: se construye solo "medianoche" (oscuro) para la v1. Los otros tres temas del canvas de diseño (`esmeralda`, `burdeos`, `marfil`) quedan documentados en [Diseño: Dashboard](../Design/Dashboard.es.md) §9 por si se añade un selector de tema más adelante, pero quedan fuera de alcance por ahora.
- **La doc de referencia se mantiene factual**: `Api.md`/`Database.md` solo describen lo implementado, nunca lo planeado — para eso está este fichero y `docs/Design/`.

## ❓ Preguntas abiertas

No bloquean la fase 1, pero necesitarán respuesta antes de construir el componente correspondiente:

- Esquema de color del gráfico por categoría para más de 4 categorías (¿ciclar la paleta? ¿generar tonos? ¿agrupar el resto en "Otras"?) — ver [Diseño: Dashboard](../Design/Dashboard.es.md) §4.5.
- Cómo se presentan las suscripciones pausadas/canceladas — el diseño actual del dashboard no las contempla en absoluto.
- Qué pasa cuando la cartera supera ~12 tarjetas (la pila ahora mismo sigue creciendo 60 px cada una).

---

<p align="center"><sub>Última actualización: 2026-11-15</sub></p>
