# ✨ Qué hace la app, en cristiano

[🇬🇧 English](./Features.en.md) | 🇪🇸 **Español**

> Este documento no habla de endpoints, códigos de estado ni tablas de base de datos — eso ya está en el [Changelog](../../CHANGELOG.es.md) y la [Referencia de la API](../Api/Api.es.md), para quien construya la app. Esto es **lo que la app sabe hacer hoy, contado como se lo contarías a alguien que simplemente va a usarla.**

---

## ⚠️ Antes de nada: cómo se usa esto hoy

Todo lo de aquí abajo **ya funciona**, pero todavía no hay ninguna pantalla en la que hacer clic — no hay botones, ni formularios, ni nada visual. Hoy por hoy, todo esto solo se puede probar a través de peticiones HTTP directas o de los tests automatizados; una persona normal todavía no puede abrir la app y usarla. Eso es exactamente lo que falta — ver la [Hoja de Ruta](../Roadmap/Roadmap.es.md) para el plan de la interfaz.

Piénsalo así: el motor del coche ya está montado y probado, pero todavía no hay carrocería ni volante.

---

## 📂 Organizar tus suscripciones en categorías

Puedes crear tus propias categorías para agrupar lo que pagas — por ejemplo "Streaming", "Software", "Gimnasio", "Hogar" — con el nombre que tú quieras, no hay una lista cerrada de categorías predefinidas.

- No puedes crear dos categorías con el mismo nombre (ni siquiera si cambias mayúsculas por minúsculas) — así evitas acabar con "Streaming" y "streaming" como dos cosas distintas por error.
- Puedes renombrar una categoría más adelante sin perder las suscripciones que tiene dentro.
- No puedes borrar una categoría mientras todavía tenga suscripciones asignadas — primero tienes que moverlas o borrarlas, así nunca te quedas con una suscripción "huérfana" sin categoría.

## 📝 Llevar el registro de cada suscripción

Por cada suscripción que apuntas, la app guarda:

- **El nombre** (Netflix, Spotify, el gimnasio, lo que sea).
- **Una descripción libre**, si quieres anotar algo.
- **El precio** que pagas.
- **Cada cuánto se te cobra**: a diario, al mes, o al año.
- **Desde cuándo** la tienes, y, si la has dado de baja, **hasta cuándo** estuvo activa.
- **Su categoría**.
- **Su estado**: activa, pausada, o cancelada (ver más abajo).

El mismo nombre de suscripción no se puede repetir dos veces dentro de la misma categoría (para que no acabes con dos "Netflix" por error dentro de "Streaming"), pero sí puedes tener, por ejemplo, una suscripción llamada igual en dos categorías distintas si por algún motivo te hiciera falta.

## 💶 Cambiar el precio y ver cómo ha evolucionado

Cuando a una suscripción le suben (o te bajan) el precio, lo actualizas en la app — y automáticamente queda **guardado para siempre** cuándo pasó, cuál era el precio antes y cuál es el nuevo. Así, con el tiempo, puedes ver el historial completo de cuánto ha ido subiendo (o bajando) cada servicio.

No puedes "cambiar" el precio al mismo precio que ya tenía, ni ponerlo en negativo — la app te avisa si lo intentas.

## ⏸️ Pausar, cancelar o reactivar sin perder el historial

Una suscripción puede estar:

- **Activa** — la estás pagando ahora mismo.
- **Pausada** — por lo que sea, de momento no se está cobrando, pero no la has borrado del todo.
- **Cancelada** — la has dado de baja.

Puedes cambiar entre estos estados cuando quieras (incluso volver a activar una que estaba pausada o cancelada). Nada de esto borra ni el historial de precios ni los datos de la suscripción — solo cambia su estado.

## 📅 Ver qué se te va a cobrar pronto

La app sabe calcular, para cada suscripción activa, cuándo le toca el siguiente cobro — ya sea diario, mensual o anual — y te puede mostrar solo las que vencen en los próximos días que tú decidas (por ejemplo, "qué se me va a cobrar en los próximos 30 días"), ordenadas de la más próxima a la más lejana.

Esto funciona bien incluso con casos raros de calendario — por ejemplo, una suscripción mensual que empezó un día 31 sigue calculando bien su próximo cobro aunque el mes no tenga 31 días.

Las suscripciones pausadas o canceladas nunca aparecen aquí, porque no se te va a cobrar nada por ellas.

## 📊 Ver cuánto estás gastando en total

De un vistazo, la app te puede decir:

- Cuánto te está costando **al mes**, en total, sumando todas tus suscripciones activas (convirtiendo las que son anuales o diarias a su equivalente mensual).
- Cuánto te está costando **al año**.
- Cuántas suscripciones activas tienes en total.

Las pausadas y las canceladas no cuentan para este total — solo lo que de verdad estás pagando ahora.

## 🔒 Todo se queda en tu ordenador

No hay cuentas, no hay login, no hay ninguna nube de por medio. Todo lo que guardas vive en un único fichero en tu propio ordenador. Nadie más puede ver tus datos, y la app funciona sin conexión a internet (salvo para lo que tú mismo quieras consultar).

---

## 🚫 Lo que la app NO hace (todavía, o nunca)

Para que quede claro y no haya sorpresas:

- **No hay pantalla ni botones todavía** — ver la advertencia del principio. Esto es lo próximo que se va a construir.
- **No te avisa ni te manda notificaciones** de que algo está por cobrarte — puedes *consultar* qué se te va a cobrar pronto, pero la app no te manda un aviso por su cuenta.
- **No se conecta a tu banco ni a ningún proveedor de pago** — tú introduces los datos a mano, la app no sabe nada de tu cuenta bancaria real.
- **No es multiusuario** — está pensada para que la use una sola persona, no para compartir con tu pareja o tu familia desde cuentas distintas.
- **No hay copia de seguridad en la nube** — si quieres respaldar tus datos, tendrás que copiar tú mismo el fichero de la base de datos.
- **No hay app de móvil** — es una aplicación web que corre en tu ordenador.

---

<p align="center"><sub>Para el detalle técnico de todo esto (endpoints, validaciones, códigos de error), ver el <a href="../../CHANGELOG.es.md">Changelog</a> y la <a href="../Api/Api.es.md">Referencia de la API</a>.</sub></p>
