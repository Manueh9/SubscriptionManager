# 🎨 Diseño — Dashboard

> Solo en español por ahora: es la especificación técnica de implementación de una pantalla concreta, no documentación de referencia bilingüe. Si en el futuro hace falta traducirla, se traduce entonces.

Diseño elegido de entre las propuestas generadas en Claude Design (canvas con 13 artboards: Bloques, Calma, Cartera, Cristal, Cronógrafo, Editorial, Guilloché en 5 variantes, Holo, Partitura, Perlas, Recibo, Tablero, Titanio). La dirección elegida es **"Guilloché · Color por servicio"** (artboard `GuillocheColor.dc.html`), con el tema **"medianoche"** (oscuro, platino/azul) como variante por defecto.

Enlace al canvas original: https://claude.ai/artifact/Jz7UJUZkw34n74dbPEjRYv#play-3d7f0468cb2d (el artboard `GuillocheColor.dc.html` dentro de `project/`). El código de ese artboard está escrito en el runtime propio de la herramienta de diseño (`<x-dc>`, `sc-for`, ...) — **no es código reutilizable**; esta especificación es la traducción a lo que hay que construir en React, y es la que manda si hay alguna duda al implementar.

Ver [Roadmap](../Roadmap/Roadmap.es.md) para en qué fase se construye esto y qué decisiones de backend/frontend ya se tomaron al respecto.

---

## 1. Concepto

Dashboard oscuro y premium. A la izquierda, las cifras globales y un gráfico circular por categoría. A la derecha, una **cartera de tarjetas apiladas**, una por suscripción activa, ordenadas por fecha de próximo cobro (la más próxima arriba).

Tres ideas que definen la pantalla:

1. Un interruptor **Al mes / Al año** cambia todas las cifras a la vez y gira todas las tarjetas.
2. Cada tarjeta tiene el **color de acento de su servicio** y un **icono genérico** según el tipo de servicio. No se usan logotipos de marcas.
3. Al **pulsar** una tarjeta se da la vuelta; al sacar el ratón de ella, vuelve sola.

## 2. Tokens de diseño

### Color (tema "medianoche")

| Token | Valor | Uso |
|---|---|---|
| `page` | `#070B14` | Fondo de página |
| `page-glow` | `rgba(180,195,220,0.08)` | Resplandor radial 820×720 px centrado en 72 % / 48 % |
| `text` | `#E9EDF4` | Texto principal |
| `muted` | `#8F99AA` | Texto secundario y etiquetas |
| `line` | `#1B2333` | Bordes y separadores |
| `accent` | `#C9D3E2` | Acento platino (símbolo €, unidad, subtítulos del gráfico) |
| `pill` | `linear-gradient(180deg, #F6F8FB, #B7C2D3)` | Botón primario y píldora del interruptor |
| `pill-ink` | `#070B14` | Texto sobre `pill` |
| `card-base` | `linear-gradient(135deg, #191A20, #0D0E12)` | Base de todas las tarjetas |

Colores del gráfico, por orden de categoría de mayor a menor gasto: `rgb(238,242,248)`, `rgb(201,211,226)`, `rgb(148,162,184)`, `rgb(95,110,134)`.

> El artboard real soporta además otros 3 temas intercambiables (`esmeralda`, `burdeos`, `marfil` — este último claro, los demás oscuros con otro tono de acento). Para la v1 construimos solo "medianoche"; los tokens de los otros tres quedan documentados aquí por si se añade un selector de tema más adelante — ver sección 9.

### Tipografía

- **Fraunces** (serif, Google Fonts, pesos 300–600 e itálica): cifras, títulos de sección, nombre de la app, nombres en las tarjetas.
- **Geist** (sans, pesos 400/500/600): navegación, etiquetas, texto auxiliar.
- Cifras siempre con `font-variant-numeric: tabular-nums`.
- Etiquetas en mayúsculas: 11–12 px, peso 500–600, `letter-spacing` 0.22–0.24em, color `muted`.

### Formato de datos

- Importes con formato español: punto de miles, coma decimal, dos decimales y ` €` (`1.945,22 €`). `toLocaleString('es-ES')` no pone punto de miles en números de cuatro cifras, así que conviene usar `Intl.NumberFormat('de-DE', …)` o un formateador propio.
- Fechas cortas: `mié 18 nov`.

## 3. Layout

- Contenedor centrado, `max-width: 1220px`, padding lateral `clamp(20px, 4vw, 64px)`, 28 px arriba y 72 px abajo. Separación vertical de 44 px entre cabecera y contenido.
- Contenido en una fila flex con `flex-wrap` y `gap: 56px 72px`:
  - **Columna izquierda** (`flex: 1 1 380px`): interruptor, total, tres datos, gráfico por categoría. Separación de 34 px entre bloques.
  - **Columna derecha** (`flex: 1 1 420px`): cartera, centrada, ancho `min(100%, 410px)`.
- En pantallas estrechas las columnas se apilan (primero la izquierda).

## 4. Componentes

### 4.1 Cabecera

- Izquierda: "Subscription Manager" en Fraunces itálica 22 px.
- Derecha: enlaces `Resumen` (activo, color `text`), `Suscripciones`, `Categorías` (color `muted`), 14 px, y botón **Nueva suscripción** (44 px de alto, radio 22 px, fondo `pill`, texto `pill-ink`, peso 600).

### 4.2 Interruptor Al mes / Al año

- Control segmentado de 260 px, borde 1 px `line`, radio completo, padding 4 px. Dos botones de 40 px de alto con `aria-pressed`.
- Una píldora (`pill`) se desliza bajo la opción activa: `translateX(0 | 100%)`, 0.55 s, `cubic-bezier(.3,1.4,.45,1)` (pequeño rebote).
- Texto de la opción activa en `pill-ink`; la inactiva en `muted`.
- Es **estado global de la pantalla** (`period: 'month' | 'year'`).

### 4.3 Total

- Etiqueta `GASTO TOTAL`.
- Cifra en Fraunces peso 300, `clamp(66px, 9vw, 118px)`, `line-height: 0.95`, `letter-spacing: -0.035em`. Símbolo `€` aparte, más pequeño, en `accent`.
- Debajo, "al mes" o "al año" en Fraunces itálica 22 px, color `accent`.
- **Contador animado**: de 0 al valor al cargar (1,7 s) y entre el valor mensual y el anual al cambiar de periodo (1,1 s), con easing cúbico in-out.

### 4.4 Tres datos

Rejilla de 3 columnas con borde superior `line` y 22 px de padding superior. Cifra en Fraunces 300 de 28 px y etiqueta de 13 px en `muted`:

- número de suscripciones activas → "activas"
- `mensuales · anuales` (por ejemplo `7 · 2`)
- importe total de los próximos 30 días → "próximos 30 días"

### 4.5 Gráfico por categoría (dos anillos)

- Título "Por categoría" (Fraunces 22 px) y nota "anillo exterior: cada suscripción".
- Gráfico de 236×236 px con dos anillos concéntricos que empiezan a las 12 y giran en sentido horario:
  - **Interior** (del 44 % al 69 % del radio): una porción por categoría, proporcional a su gasto mensual equivalente.
  - **Exterior** (del 72 % al 100 % del radio): cada categoría se subdivide en sus suscripciones, alineadas con la porción interior. Cada suscripción usa el color de su categoría aclarado progresivamente.
  - Pequeña separación entre porciones (≈0,8° interior, ≈0,6° exterior).
- **Centro**: etiqueta (10 px, mayúsculas), importe (Fraunces 19 px) y subtítulo (11 px, `accent`). Por defecto: `TOTAL`, el total y "al mes"/"al año".
- **Interacción**: al pasar el cursor por una porción, el resto baja a ≈20 % de opacidad y el centro muestra su nombre, importe y porcentaje (si es del anillo exterior, también su categoría). Al salir, vuelve al total.
- **Leyenda** a la derecha: una fila por categoría (punto de color, nombre, importe), mínimo 44 px de alto; al pasar el cursor resalta esa categoría en el gráfico.
- **Entrada**: las porciones crecen desde 0° en 1,6 s (ease-out cúbico) mientras el gráfico rota de −70° a 0° y escala de 0,82 a 1.
- Los importes del gráfico y la leyenda siguen al interruptor de periodo.
- Implementación recomendada: **SVG** (arcos con `path` o `stroke-dasharray`). En el prototipo está hecho con `conic-gradient` y máscaras, pero SVG da mejor detección de cursor y accesibilidad.
- **Pendiente de decidir** (no resuelto por el diseño): con más de 4 categorías, la paleta de 4 colores no basta. Decidir al construir este componente: ciclar la paleta, generar tonos intermedios, o agrupar el resto en una porción "Otras".

### 4.6 Cartera

- Cabecera: "Tu cartera" (Fraunces 22 px) y, a la derecha, "pulsa una tarjeta para darle la vuelta" (13 px, `muted`).
- Las tarjetas están apiladas con posición absoluta: la tarjeta `i` se desplaza `i × 60 px` hacia abajo, y la primera queda encima (`z-index` decreciente). De cada tarjeta tapada se ve una franja superior de 60 px.
- Alto del contenedor: `256 + (n − 1) × 60` px.
- **Hover**: al pasar por una tarjeta que no es la primera, ella y todas las de debajo bajan 150 px más, de modo que la tarjeta señalada asoma casi entera. El contenedor crece 150 px. Transición 0,65 s `cubic-bezier(.3,1.35,.45,1)`. Al salir de la cartera, todo vuelve.
- Muestra **todas** las suscripciones activas, no solo las de los próximos 30 días — incluidas las anuales que pueden estar a varios meses de su próximo cobro.

### 4.7 Tarjeta de suscripción

Tamaño: ancho de la cartera × **256 px**, radio **18 px**. Es un `<button>` con dos caras (3D).

**Fondo** (ambas caras), de arriba abajo en capas:

1. Grabado fino en el color de acento según la categoría (opacidad 10–16 %):
   - Streaming: círculos concéntricos desde la esquina superior derecha.
   - Software: trama de rombos (líneas a 45° y −45° cada 9 px).
   - Fitness: rayos desde la esquina inferior derecha.
   - Hogar: arcos elípticos desde la esquina inferior izquierda.
   - (Estos 4 grabados son genéricos, no ligados a esos nombres de categoría concretos — ver Roadmap, campo `Pattern` de `Category`.)
2. Oscurecido inferior: `linear-gradient(0deg, rgba(0,0,0,0.5), transparent 46%)`.
3. **Tres ondas** del color de acento que entran por la derecha, con borde nítido: elipses grandes centradas fuera del borde derecho con opacidades 0,5 / 0,32 / 0,2.
4. Velo horizontal del acento: de transparente al 30 % a 0,28 de opacidad en el borde derecho.
5. Base `card-base`.

**Borde**: mensuales con trazo interior blanco al 10 %; **anuales** con borde interior de 2 px en su color de acento al 90 %. Además, un marco interior fino a 10 px del borde (acento al 22 %).

**Cara frontal**:

- Arriba (a 22 px): categoría en mayúsculas a la izquierda; "mensual"/"anual" en Fraunces itálica 14 px a la derecha.
- Izquierda (a 70 px del borde superior): rosetón decorativo de 46 px, grabado radial en el color de acento.
- Derecha (a 64 px del borde superior): **anillo de días**, 98 px. Disco interior oscuro (`rgba(8,9,12,0.62)`), aro de ≈7 px: tramo en color de acento y resto en blanco al 16 %. Dentro, el número de días (Fraunces 34 px) y "días" (12 px).
  - Progreso del aro = `1 − días_restantes / periodo`, con periodo 30 para mensuales y 365 para anuales, mínimo 4 %. Empieza a las 12, sentido horario. Se dibuja al cargar.
- Abajo: ficha de icono de 42×42 px (radio 12 px, fondo negro al 50 %, borde blanco al 14 %, icono de 22 px en el color de acento), nombre del servicio (Fraunces 18 px, una línea con elipsis) y fecha del próximo cobro (12 px). A la derecha, el precio mensual equivalente (Fraunces 300, 24 px) y debajo `AL MES`; en las anuales, `AL MES · 289,00 €/AÑO`.

**Cara trasera** (rotada 180° en Y):

- Banda oscura horizontal de 40 px a 24 px del borde superior.
- `COSTE ANUAL` y debajo el detalle: "12 cargos de 13,99 €" (mensual) o "1 cargo · mié 9 dic" (anual).
- A la derecha, `×12` o `×1` en Fraunces 300 de 52 px.
- Abajo: misma ficha de icono y nombre, con "cobro mensual"/"cobro anual"; a la derecha el coste anual y `AL AÑO`.

**Iconos genéricos** (trazo de 1,8 px, estilo lineal): reproducir, nota musical, terminal, código, nube, mancuerna, wifi, paquete. Se elige por tipo de servicio, no por marca.

## 5. Interacciones y movimiento

| Evento | Efecto |
|---|---|
| Carga | Las tarjetas se reparten: cada una entra desde abajo a la derecha (`translate(150px, 80px) rotate(10deg)`, opacidad 0) con rebote, 0,9 s, empezando por la del fondo, 80 ms de desfase |
| Hover en tarjeta | Asoma (ver 4.6), se inclina hacia el cursor (`rotateX` ±4,5°, `rotateY` ±6,5° según la posición del ratón, `perspective: 1100px`), sube 6 px, aparece un brillo radial blanco al 16 % que sigue al ratón y la sombra gana un resplandor del color de acento |
| **Click en tarjeta** | Gira 180° en Y y muestra la otra cara. 0,85 s, `cubic-bezier(.3,1.25,.45,1)`, sin retardo |
| **Salir de una tarjeta girada** | Vuelve a su cara normal. Un segundo click también la devuelve |
| Cambiar periodo | Todas las tarjetas giran en cascada (60 ms de desfase; de arriba abajo al pasar a "año", de abajo arriba al volver), el total cuenta hasta el nuevo valor y el gráfico actualiza importes. Se anula cualquier giro individual |
| Hover en gráfico | Ver 4.5 |

Regla de la cara visible: `girada = (periodo === 'year') XOR (tarjeta pulsada)`. Es decir, en "Al año" todas muestran el reverso y pulsar una enseña su cara mensual.

El click solo gira la tarjeta. La navegación al detalle de la suscripción debe ir por otro control (por ejemplo, un botón dentro del reverso), que no está en este diseño.

**Accesibilidad**:

- Respetar `prefers-reduced-motion`: sin reparto, sin contadores ni crecimiento (valores finales directos) y sin transiciones de giro.
- Tarjetas enfocables con teclado; foco equivale a hover, Enter/Espacio gira, perder el foco la devuelve. `aria-pressed` refleja si está girada y `aria-label` describe nombre, importe visible y próximo cobro.
- El gráfico necesita una alternativa textual (la leyenda ya lista las categorías).

## 6. Datos y relación con el backend

Datos que necesita la pantalla:

- **Resumen de gastos**: total mensual, total anual, número de activas.
- **Suscripciones activas** con: nombre, categoría, precio, `billingCycle`, fecha del próximo cobro y días restantes.
- **Próximas renovaciones a 30 días** para el dato "próximos 30 días" (suma de importes).

Cálculos:

- Mensual equivalente: `Monthly → price`, `Yearly → price / 12`. **Decidido**: el backend añade este valor ya calculado a la respuesta de `Subscription` (ver Roadmap) en vez de que el frontend reimplemente la fórmula.
- Anual: `Monthly → price × 12`, `Yearly → price`. Mismo criterio: calculado en backend.
- `billingCycle = Day`: **decidido**. Se reutiliza la misma fórmula que ya usa `GET /api/Expenses/summary` (`Day × 365` al año), para que el total de la cartera coincida siempre con el resumen global.
- Orden de la cartera: por fecha de próximo cobro ascendente.
- Gasto por categoría: suma del mensual equivalente de sus suscripciones activas.

**Campos que el diseño usa y el modelo actual no tiene** — ver el Roadmap para el estado de cada uno:

- `AccentColor` en `Subscription` (color de la tarjeta).
- `Icon` en `Subscription` (icono genérico).
- `Pattern` en `Category` (grabado de fondo).

Estados no cubiertos por este diseño y pendientes de definir cuando se construya: cartera vacía, suscripciones pausadas o canceladas (no aparecen en este diseño, hay que decidir si tienen su propia vista), y más de ~12 tarjetas (la pila crece 60 px por tarjeta, en algún punto puede necesitar scroll o paginación).

## 7. Catálogo de servicios por defecto

Este es el contenido real del catálogo "servicios conocidos" que decidimos en el [Roadmap](../Roadmap/Roadmap.es.md) (sección "Decisiones ya tomadas"): vive **solo en el frontend**, como una tabla estática (p. ej. `src/data/servicePresets.ts`), usada únicamente para **sugerir** color e icono al crear una suscripción cuyo nombre coincide con uno de la lista (comparación sin distinguir mayúsculas). El usuario siempre puede elegir otro color/icono en el formulario, y la lista se amplía con el tiempo sin tocar el backend ni desplegar nada — solo editar ese fichero.

Los colores están inspirados en la identidad visual real de cada servicio (para que resulten reconocibles), pero son **solo el color de acento liso** — nunca un logotipo ni una marca registrada, igual que el resto del diseño.

| Servicio | Icono | Color de acento |
|---|---|---|
| Netflix | play | `#E5383B` |
| Disney+ | play | `#1138C4` |
| HBO Max | play | `#7B2FF7` |
| Amazon Prime Video | play | `#1FA7DC` |
| YouTube Premium | play | `#FF4D3D` |
| Spotify | music | `#1DB954` |
| Apple Music | music | `#FA2D63` |
| Tidal | music | `#00D9C0` |
| GitHub Copilot | terminal | `#8957E5` |
| ChatGPT Plus | terminal | `#74AA9C` |
| Claude Pro | terminal | `#D97757` |
| JetBrains All Products | code | `#F5B400` |
| Microsoft 365 | code | `#1F9E89` |
| Adobe Creative Cloud | code | `#D6246B` |
| iCloud+ | cloud | `#5AB9FF` |
| Google One | cloud | `#22B8CF` |
| Dropbox | cloud | `#3D5AFE` |
| Gimnasio | gym | `#FF8229` |
| Apple Fitness+ | gym | `#FF375F` |
| Fibra + móvil | wifi | `#E146AA` |
| Amazon Prime | box | `#23B9D7` |
| HelloFresh | box | `#91C11E` |
| PlayStation Plus | box | `#0070CC` |

> Nota: las suscripciones de videojuegos (PlayStation Plus, Xbox Game Pass, ...) no tienen un icono genérico que les quede realmente bien dentro de los 8 actuales (play/music/terminal/cloud/gym/wifi/code/box) — usar `box` es un ajuste aceptable pero no ideal. Si en el futuro hay varias suscripciones de este tipo, valorar añadir un noveno icono (p. ej. un mando de videojuegos).

## 8. Notas de implementación

- **Giro 3D**: contenedor con `perspective`, elemento interior con `transform-style: preserve-3d` y dos caras con `backface-visibility: hidden`; la trasera con `rotateY(180deg)`. Separar en capas anidadas la animación de entrada, la inclinación por ratón y el giro, para que no se pisen las transformaciones.
- **Framer Motion**: `animate={{ rotateY }}` para el giro, `useMotionValue` + `useSpring` para la inclinación, `animate()` sobre un motion value para los contadores y `staggerChildren` para el reparto y el giro en cascada.
- **Rendimiento**: animar solo `transform` y `opacity`. El brillo que sigue al ratón debe ir con variables CSS (`--mx`, `--my`) actualizadas directamente vía `ref`, no con estado de React, para no re-renderizar en cada movimiento del ratón — el prototipo original sí usa estado por simplicidad de su propio runtime, pero en React eso sería un cuello de botella de rendimiento innecesario.
- **Tailwind**: definir los tokens del apartado 2 en el tema. Los fondos de tarjeta dependen del color de acento de cada suscripción, así que van como variables CSS por tarjeta (`--accent: 225 45 55`), no en el tema global de Tailwind.
- **Componentes sugeridos**: `PeriodToggle`, `AnimatedAmount`, `SummaryStats`, `CategorySunburst`, `Wallet`, `SubscriptionCard` (con `CardFront`, `CardBack`, `DaysRing`, `ServiceIcon`).

## 9. Otros temas de color (no usados en la v1)

Tokens de los otros 3 temas del artboard original, por si se añade un selector de tema más adelante:

**Esmeralda** — `page: #07120E`, `text: #EFE9DA`, `muted: #97A39A`, `line: #1F2C26`, `accent: #D4B26E` (dorado sobre verde oscuro).

**Burdeos** — `page: #120709`, `text: #F4E8E2`, `muted: #AC9792`, `line: #2F1B1D`, `accent: #E3A587` (salmón sobre granate oscuro).

**Marfil** (claro, único tema no oscuro) — `page: #EFEAE0`, `text: #1B1A17`, `muted: #655F56`, `line: #D6CEC0`, `accent: #1D2B45` (azul marino sobre crudo claro).
