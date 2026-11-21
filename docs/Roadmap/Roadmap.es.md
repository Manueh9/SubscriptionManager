# 🗺️ Hoja de Ruta

[🇬🇧 English](./Roadmap.en.md) | 🇪🇸 **Español**

---

> Este plan sustituye al anterior (identidad visual de tarjetas, esqueleto de frontend sobre el modelo de `Subscription`/`Category`). Desde el 2026-11-21, la referencia es la [Especificación de la V1](../Specification/SpecificationV1.es.md), que transforma la app de gestor de suscripciones a gestor de todos los gastos periódicos del hogar. Las fases de abajo son las del apartado 13 de ese documento.

## 📋 Índice

1. [Estado general](#-estado-general)
2. [Fase 0 — Base](#-fase-0--base)
3. [Fase 1 — Modelo y migración](#-fase-1--modelo-y-migración)
4. [Fase 2 — Motor de calendario y cálculos](#-fase-2--motor-de-calendario-y-cálculos)
5. [Fase 3 — Catálogo](#-fase-3--catálogo)
6. [Fase 4 — Importador](#-fase-4--importador)
7. [Fase 5 — Calendario ICS](#-fase-5--calendario-ics)
8. [Fase 6 — Copia de seguridad y Wallos](#-fase-6--copia-de-seguridad-y-wallos)
9. [Fase 7 — Demo](#-fase-7--demo)
10. [Fase 8 — Frontend](#-fase-8--frontend)
11. [Decisiones ya tomadas](#-decisiones-ya-tomadas)

---

## 🚦 Estado general

| Fase | Qué | Estado |
|---|---|---|
| 0 | Base: licencia/CLA, cabeceras SPDX, ubicación de la BD, seguridad de arranque, `TimeProvider`, mensajes en español | 🟡 En curso |
| 1 | Modelo y migración: `RecurringExpense`, `ChargeRecord`, enums nuevos, fin de `Category` | 🔲 Sin empezar |
| 2 | Motor de calendario y cálculos: resumen, previsión, nómina, hucha, fecha límite de baja | 🔲 Sin empezar |
| 3 | Catálogo de ~33 servicios españoles | 🔲 Sin empezar |
| 4 | Importador de extractos bancarios (Excel/CSV) | 🔲 Sin empezar |
| 5 | Calendario ICS | 🔲 Sin empezar |
| 6 | Copia de seguridad (JSON propio) e importación desde Wallos | 🔲 Sin empezar |
| 7 | Modo demo | 🔲 Sin empezar |
| 8 | Frontend (7 pantallas del prototipo + lo mínimo sin diseño propio) | 🔲 Sin empezar |

---

## 🧱 Fase 0 — Base

- [x] `LICENSE` (AGPL-3.0), `CLA.md`, `CONTRIBUTING.md` en la raíz; sección de licencia en el README; `THIRD-PARTY-NOTICES.md`.
- [x] Cabecera SPDX (`SPDX-License-Identifier` + `Copyright`) en los 42 ficheros `.cs` existentes.
- [x] Ubicación configurable de la base de datos (`%AppData%/SubscriptionManager/data.db`) + copia automática antes de cada migración (conservando las 10 más recientes). Las migraciones pendientes se aplican solas al arrancar.
- [ ] Escuchar solo en `127.0.0.1`, validar la cabecera `Host`, CORS solo en desarrollo, límite de subida de 10 MB.
- [ ] `TimeProvider` inyectado en todo lo que dependa de "hoy"; mensajes de `ProblemDetails` en español.

## 🧩 Fase 1 — Modelo y migración

Sin empezar. Cubre los puntos 1 a 4 del apartado 2.1 de la especificación:

- [ ] Entidades y enums nuevos: `RecurringExpense` (sustituye a `Subscription`), `ChargeRecord`, `ImportProfile`, `AppSettings`; enums `ExpenseKind`, `ExpenseStatus`, `IntervalUnit`, `AmountMode`, `NoticeUnit`, `ChargeSource`.
- [ ] Eliminación de `Category` (sustituida por `ExpenseKind`), sin restricción de unicidad en `RecurringExpense.Name`.
- [ ] Migración de EF Core con copia de seguridad previa del `.db`, conversión de los datos existentes y colores/iconos por defecto según el tipo.
- [ ] CRUD de gastos, cambio de estado, cobros manuales y `PriceHistory` al cambiar el importe.

## 🧮 Fase 2 — Motor de calendario y cálculos

Sin empezar. Cubre los puntos 5 a 8: fechas de cobro, importe estimado, coste anual/mensual, fecha límite de baja, resumen, previsión a 90 días, nómina y hucha.

## 📚 Fase 3 — Catálogo

Sin empezar. Catálogo embebido de ~33 servicios españoles (apartado 10), con patrones de reconocimiento bancario y valores por defecto.

## 🏦 Fase 4 — Importador

Sin empezar. Lectura de Excel/CSV, detección de cabecera y columnas, normalización, detección de cobros recurrentes, sesiones de importación (apartado 9).

## 📅 Fase 5 — Calendario ICS

Sin empezar. Generador de `.ics` con cobros, fechas límite y avisos (apartado 11.3).

## 💾 Fase 6 — Copia de seguridad y Wallos

Sin empezar. Exportar/restaurar copia JSON propia e importar desde Wallos (apartados 11.1 y 11.2).

## 🎭 Fase 7 — Demo

Sin empezar. Modo demo con el juego de datos de prueba, fechas desplazadas a "hoy" (apartado 11.4).

## 🖥 Fase 8 — Frontend

Sin empezar. Las 7 pantallas del prototipo (Bienvenida, Importar, Tu foto, Resumen, Detalle, Previsión, Nuevo gasto) más lo que no está dibujado (menú de ajustes, gastos pausados/incompletos, banda de demo). El [diseño del Dashboard](../Design/Dashboard.es.md) se adapta aquí con los cambios del apartado 12.3 (Gasto en vez de Suscripción, grabado por tipo en vez de por categoría, etc.) — no se descarta.

---

## ✅ Decisiones ya tomadas

- **Rama por fase**: cada fase (`fase-0-base`, `fase-1-modelo`, ...) se fusiona a `main` al cerrarse, para proteger el tag `v0.1.0` mientras dura la reescritura.
- **Sin unicidad de nombre en `RecurringExpense`**: se permite más de un gasto con el mismo nombre.
- **El prototipo de 7 pantallas** se consulta al llegar a la Fase 8, no antes.
- **Extractos bancarios y JSON de Wallos reales**: se añaden como fixtures cuando Manuel los aporte; las Fases 4 y 6 arrancan con fixtures sintéticos.
- **La doc de referencia se mantiene factual**: `Api.md`/`Database.md` solo describen lo implementado; este fichero documenta el plan.

---

<p align="center"><sub>Última actualización: 2026-11-21</sub></p>
