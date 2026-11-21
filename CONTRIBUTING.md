# Cómo contribuir a Subscription Manager

Gracias por querer mejorar Subscription Manager. Esta guía explica qué necesitas saber antes de abrir un *pull request*.

## Antes de empezar

- Para cambios grandes, abre primero un *issue* y explica qué quieres hacer. Así evitamos trabajo que luego no encaje.
- Lee los principios del proyecto. Todo *pull request* tiene que respetarlos:
  - **Local primero.** Todo corre en el ordenador del usuario, sin cuentas ni nube.
  - **Sin red.** Nada de llamadas salientes, telemetría ni analítica.
  - **Privacidad.** No se guardan ni se registran en logs datos bancarios.

## Acuerdo de contribución (CLA)

Para aceptar tu primer *pull request* necesitamos que aceptes el acuerdo de contribución ([CLA.md](CLA.md)).

- El bot **CLA Assistant** te lo pedirá en un comentario del *pull request*. Se acepta con un clic y vale para todas tus aportaciones.
- **Por qué lo pedimos:** permite al mantenedor ofrecer el proyecto también con otras licencias (por ejemplo, a una empresa que no pueda usar la AGPL) sin tener que pedir permiso a cada persona que ha contribuido.
- **Lo que no cambia:** sigues siendo el autor de tu aportación, y esta seguirá disponible como código abierto en la versión pública del proyecto.
- Si contribuyes como parte de tu trabajo, asegúrate de que tu empresa te da permiso.

## Licencia

Subscription Manager se publica bajo la **GNU Affero General Public License v3.0 o posterior** (AGPL-3.0-or-later). Ver [LICENSE](LICENSE).

## Normas de código

- Identificadores y comentarios en **inglés**. Textos de la interfaz y mensajes al usuario en **español**.
- Commits con **Conventional Commits** en inglés (`feat: …`, `fix: …`).
- Los tests tienen que pasar: `dotnet test` en el backend y los tests del frontend.
- Los ficheros de código nuevos llevan esta cabecera:

  ```
  // SPDX-License-Identifier: AGPL-3.0-or-later
  // Copyright (C) 2026 Manuel Garcia Rodriguez
  ```

## Dependencias

Antes de añadir una dependencia, revisa su licencia.

- **Se aceptan:** MIT, BSD, Apache-2.0, ISC, MPL-2.0, LGPL, GPL-3.0 o AGPL-3.0, y OFL para fuentes tipográficas.
- **No se aceptan:**
  - GPL-2.0-only, que es incompatible con la AGPL-3.0;
  - SSPL o Commons Clause;
  - licencias no comerciales o de código visible (PolyForm, BSL, FSL…);
  - software privativo.

Añade la dependencia y su licencia a `THIRD-PARTY-NOTICES.md`.

## Contribuir al catálogo de servicios

El catálogo (`Catalog/catalog.es.json`) recoge los servicios típicos en España y cómo aparecen sus cargos en el banco. Es una de las partes donde más puedes ayudar.

- Para añadir o corregir un patrón bancario, indica cómo aparece el cargo y de qué banco es. Pon **solo el texto del concepto**, sin importes, números de cuenta ni datos personales.
- Nada de logotipos ni marcas gráficas: solo el nombre en texto, un color y un icono genérico.
- No añadas precios: cambian a menudo y se quedarían viejos.

## Extractos bancarios de prueba

Si aportas un extracto para las pruebas del importador:

- **Anonimízalo antes de subirlo:** cambia nombres, cuentas, referencias e importes reales.
- **Mantén el formato tal cual:** columnas, filas de cabecera, separadores, codificación y formato de fechas e importes. Es lo que se está probando.
- Indica de qué banco es y cómo lo descargaste (Excel o CSV, y desde qué pantalla).
