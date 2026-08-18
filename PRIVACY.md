# Icon Replacer Privacy Policy

**Effective date:** August 17, 2026  
**Publisher:** To be completed with the verified Microsoft Store publisher name before submission

Icon Replacer is a local-first Windows utility for changing and restoring folder and shortcut icons. It includes a native management application, a local icon library, restore history, a command host, and packaged File Explorer context-menu integrations.

This policy explains what Icon Replacer accesses, what it stores, and when it uses the network.

## 1. Files and shell information Icon Replacer may access

Icon Replacer may access the following local information when the user opens the application or invokes an Explorer command:

- folders explicitly selected by the user;
- `.lnk` shortcut files explicitly selected by the user;
- local `.ico` files selected or imported by the user;
- folder and shortcut paths;
- shortcut target and icon metadata needed to preview, change, or restore an icon;
- `desktop.ini` content and file/folder attributes required by Windows folder-icon behavior;
- Explorer shell notifications and refresh operations required to display a changed icon;
- packaged COM activation and context-menu selection data supplied by Windows;
- operation results and errors needed to explain permission, format, trust, or refresh failures.

The Explorer extension is designed to keep menu-time work small and bounded. Heavier work is delegated to the application or command host.

## 2. Local icon library

Icon Replacer maintains a reusable icon library under the user's profile:

```text
%USERPROFILE%\.icons
```

The library may contain:

- `.ico` files imported by the user;
- one-level collections created by the user;
- catalog metadata derived from local file names, sizes, paths, and icon contents;
- application-created state needed to browse the library.

Icon Replacer does not claim ownership of imported icons. Users are responsible for having the right to use and redistribute icon files they import.

## 3. Restore history

Before changing a supported target, Icon Replacer records information needed to restore the previous icon configuration. Restore records may contain:

- target type and local path;
- previous icon path/index or folder-icon configuration;
- the new icon reference;
- timestamps and operation state;
- information needed to determine whether restoration is still possible.

Restore history is stored locally. It may reveal folder names, shortcut names, local paths, and icon-library paths. Review and redact it before sharing diagnostics publicly.

## 4. Changes made to Windows files

For folders, Icon Replacer may update `desktop.ini` and the file attributes Windows requires for custom folder icons. For shortcuts, it may update `.lnk` icon metadata through Windows shell interfaces.

Icon Replacer is designed to preserve unrelated `desktop.ini` content, create a restore record before mutation, notify Explorer after a successful change, and fail safely when a path is protected or untrusted.

The application does not automatically elevate for normal use and does not attempt to bypass Windows trust or security policy for untrusted `desktop.ini` files.

## 5. Network activity

### Microsoft Store build

The Microsoft Store build is compiled with Store-managed updates. Its update service returns a Store-managed status without sending a request to GitHub Releases. Microsoft may provide package delivery, licensing, crash reporting, and updates under Microsoft's own terms and privacy practices.

### Direct/GitHub build

The direct build may perform a bounded HTTPS request to the public GitHub Releases API when the user checks for an update. The request includes the Icon Replacer version as a user agent and standard network metadata visible to GitHub.

The update request retrieves public release metadata only. It is not designed to upload selected paths, `.ico` files, shortcut contents, `desktop.ini`, library contents, restore history, or user credentials.

The application may open project, documentation, release, support, sponsorship, or website links when the user explicitly selects them.

## 6. Information Icon Replacer does not intentionally collect

Icon Replacer is not designed to collect or transmit:

- account passwords or authentication tokens;
- prompts, responses, conversations, emails, or customer documents;
- the contents of unrelated local files;
- contact lists or browsing history;
- advertising identifiers;
- imported icon files for advertising, analytics, or resale;
- folder/shortcut paths to GVASTETHECREATOR;
- Explorer selections unrelated to an Icon Replacer command.

## 7. Sharing and sale of data

GVASTETHECREATOR does not sell Icon Replacer data.

Icon Replacer does not intentionally share the local icon library, selected paths, shortcut metadata, `desktop.ini` content, or restore history with GVASTETHECREATOR or third parties.

## 8. Logs, screenshots, and support

Users may voluntarily share logs, screenshots, restore records, library files, or other diagnostics when requesting support. Those materials can contain:

- usernames and home-directory paths;
- folder or shortcut names;
- local file-system structure;
- imported icon artwork;
- restore history;
- error details from protected or unavailable paths.

Review and redact all support material before sharing it publicly. Do not attach confidential paths, proprietary icons, customer names, or private shortcut targets to a public issue.

## 9. Data retention and deletion

The local icon library and restore history are intended to survive application updates. Current release policy preserves `%USERPROFILE%\.icons` by default so applied icon references and restore records are not broken unexpectedly.

Uninstall behavior must be verified for each release. Removing the package should unregister Explorer integrations. It must not delete original folders, shortcuts, or source `.ico` files. Users can delete the local icon library and restore history separately when they no longer need them, after considering whether existing folder or shortcut icons still reference those files.

## 10. Security

Icon Replacer validates local icon files and target paths and includes protections for reparse points, untrusted `desktop.ini` targets, protected locations, and bounded Explorer work. No software can guarantee absolute security.

Report security issues privately according to [`SECURITY.md`](SECURITY.md), without publishing sensitive paths, malicious fixtures, private icons, or exploit details in a public issue.

## 11. Children's privacy

Icon Replacer is a desktop customization utility and is not directed to children. It does not knowingly collect personal information from children.

## 12. Changes to this policy

This policy may be updated if Icon Replacer changes its shell integration, storage, supported formats, update channel, telemetry, or network behavior. Material changes will update this document and its effective date.

## 13. Contact

Privacy and support questions can be submitted through the public project channels without including credentials, confidential paths, private shortcut targets, or proprietary icon files.

Repository: `gvastethecreator/icon-replacer`

---

# Política de Privacidad de Icon Replacer

**Fecha de vigencia:** 17 de agosto de 2026  
**Publicador:** debe completarse con el nombre verificado de Microsoft Store antes de la submission

Icon Replacer es una utilidad local para Windows que permite cambiar y restaurar iconos de carpetas y accesos directos. Incluye una aplicación de gestión, biblioteca local de iconos, historial de restauración, command host e integraciones empaquetadas para el menú contextual de Explorer.

Esta política explica qué consulta Icon Replacer, qué guarda y cuándo utiliza la red.

## 1. Archivos e información del shell

Icon Replacer puede consultar la siguiente información local cuando el usuario abre la aplicación o invoca un comando de Explorer:

- carpetas seleccionadas explícitamente;
- accesos directos `.lnk` seleccionados explícitamente;
- archivos `.ico` locales elegidos o importados;
- rutas de carpetas y accesos directos;
- target y metadata de icono necesaria para preview, cambio o restauración;
- contenido de `desktop.ini` y atributos requeridos por Windows para iconos de carpeta;
- notificaciones y refresco de Explorer;
- datos de selección entregados por Windows a la activación COM y al menú contextual;
- resultados y errores necesarios para explicar fallos de permiso, formato, confianza o refresco.

La extensión de Explorer está diseñada para mantener el trabajo del menú pequeño y acotado. Las operaciones más pesadas se delegan a la aplicación o command host.

## 2. Biblioteca local

Icon Replacer mantiene una biblioteca reutilizable en:

```text
%USERPROFILE%\.icons
```

Puede contener:

- archivos `.ico` importados;
- colecciones de un nivel creadas por el usuario;
- metadata derivada de nombres, tamaños, rutas y contenido de iconos;
- estado necesario para navegar la biblioteca.

Icon Replacer no reclama propiedad sobre iconos importados. El usuario es responsable de tener derechos para utilizarlos o redistribuirlos.

## 3. Historial de restauración

Antes de cambiar un target compatible, Icon Replacer registra la información necesaria para restaurar su configuración anterior. Puede incluir:

- tipo y ruta local del target;
- ruta/índice de icono anterior o configuración de carpeta;
- referencia al icono nuevo;
- timestamps y estado de operación;
- información necesaria para determinar si todavía es posible restaurar.

El historial permanece local y puede revelar nombres de carpetas, accesos directos y rutas. Debe revisarse antes de compartir diagnósticos.

## 4. Cambios en archivos de Windows

Para carpetas, Icon Replacer puede actualizar `desktop.ini` y atributos necesarios para iconos personalizados. Para accesos directos, puede modificar metadata de icono mediante interfaces del shell.

Está diseñado para preservar contenido no relacionado de `desktop.ini`, crear un registro antes de modificar, notificar Explorer después del éxito y fallar de forma segura ante rutas protegidas o no confiables.

No eleva automáticamente para uso normal ni intenta evadir políticas de confianza de Windows.

## 5. Actividad de red

### Versión de Microsoft Store

La versión de Store se compila con updates administrados por Microsoft Store. El servicio de updates devuelve ese estado sin consultar GitHub Releases. Microsoft puede proporcionar distribución, licencias, reportes de fallos y updates según sus propias condiciones.

### Versión directa/GitHub

La versión directa puede realizar una consulta HTTPS acotada a la API pública de GitHub Releases cuando el usuario comprueba updates. La solicitud incluye la versión como user agent y metadata de red estándar visible para GitHub.

La consulta obtiene únicamente metadata pública. No está diseñada para subir rutas, `.ico`, accesos directos, `desktop.ini`, biblioteca, historial ni credenciales.

La aplicación puede abrir enlaces de proyecto, documentación, releases, soporte, sponsorship o sitio web cuando el usuario los selecciona.

## 6. Información que no recopila intencionalmente

Icon Replacer no está diseñado para recopilar o transmitir:

- contraseñas o tokens;
- prompts, respuestas, conversaciones, correos o documentos de clientes;
- contenido de archivos locales no relacionados;
- contactos o historial de navegación;
- identificadores publicitarios;
- iconos importados para publicidad, analytics o reventa;
- rutas a GVASTETHECREATOR;
- selecciones de Explorer no relacionadas con un comando de Icon Replacer.

## 7. Cesión o venta

GVASTETHECREATOR no vende datos de Icon Replacer.

La aplicación no comparte intencionalmente biblioteca, rutas, metadata de accesos directos, `desktop.ini` ni historial con GVASTETHECREATOR o terceros.

## 8. Logs, capturas y soporte

El usuario puede compartir voluntariamente logs, capturas, historial, archivos de biblioteca u otros diagnósticos. Pueden contener:

- nombres de usuario y rutas de perfil;
- nombres de carpetas o accesos directos;
- estructura del sistema de archivos;
- artwork importado;
- historial de restauración;
- errores de rutas protegidas o ausentes.

Debe revisarse y redactarse antes de publicarlo. No se deben adjuntar rutas confidenciales, iconos propietarios, nombres de clientes ni targets privados a un issue público.

## 9. Conservación y eliminación

La biblioteca y el historial están pensados para sobrevivir updates. La política actual preserva `%USERPROFILE%\.icons` para no romper referencias e historial.

La desinstalación debe verificarse en cada release. Remover el paquete debe desregistrar las integraciones de Explorer y no debe eliminar carpetas, accesos directos o `.ico` originales. El usuario puede borrar la biblioteca e historial cuando ya no los necesita, considerando primero si hay iconos aplicados que todavía referencian esos archivos.

## 10. Seguridad

Icon Replacer valida iconos y rutas e incluye protecciones para reparse points, `desktop.ini` no confiables, ubicaciones protegidas y trabajo acotado en Explorer. Ningún software puede garantizar seguridad absoluta.

Los problemas deben informarse de forma privada según [`SECURITY.md`](SECURITY.md), sin publicar rutas sensibles, fixtures maliciosos, iconos privados o detalles de explotación.

## 11. Privacidad de menores

Icon Replacer es una utilidad de personalización y no está dirigida a menores. No recopila conscientemente información personal de menores.

## 12. Cambios

Esta política puede actualizarse si cambian la integración del shell, almacenamiento, formatos, canal de updates, telemetría o red. Los cambios materiales actualizarán el documento y la fecha de vigencia.

## 13. Contacto

Las consultas pueden enviarse por los canales públicos sin incluir credenciales, rutas confidenciales, targets privados ni iconos propietarios.

Repositorio: `gvastethecreator/icon-replacer`
