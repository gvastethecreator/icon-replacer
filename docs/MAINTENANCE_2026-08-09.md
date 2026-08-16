# Mantenimiento 2026-08-09

## Clasificación

Aplicación Windows .NET 10/WinUI con core, AppModel, CLI, command host, shell extension nativa y tests. Bun y pnpm no aplican: no existe frontend JavaScript ni manifest Node.

## Dependencias y migraciones

| Paquete | Antes | Ahora |
| --- | --- | --- |
| Microsoft.WindowsAppSDK | 2.2.0 | 2.3.1 |
| Microsoft.Windows.SDK.BuildTools | 10.0.28000.2270 | 10.0.28000.2526 |
| Microsoft.Windows.SDK.BuildTools.WinApp | 0.4.0 | 0.5.0 |
| Microsoft.NET.Test.Sdk | 17.14.1 | 18.8.1 |
| coverlet.collector | 6.0.4 | 10.0.1 |
| xunit | 2.9.3, legacy | xunit.v3 3.2.2 |
| xunit.runner.visualstudio | 3.1.4 | 3.1.5 |

CommunityToolkit.Mvvm 8.4.2 ya era latest. El SDK queda fijado en 10.0.302 con roll-forward de patch para reproducir net10. Los paquetes directos no tienen actualizaciones pendientes. Los componentes transitivos siguen las versiones compatibles fijadas por Windows App SDK y Microsoft.NET.Test.Sdk; forzarlos como referencias directas rompería el contrato de esos bundles.

Fuentes primarias revisadas:

- [Windows App SDK release channels](https://learn.microsoft.com/windows/apps/windows-app-sdk/release-channels)
- [Windows App SDK 2.3.1 release notes](https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=stable)
- [Migración de xUnit.net v2 a v3](https://xunit.net/docs/getting-started/v3/migration)
- [Microsoft Test Platform releases](https://github.com/microsoft/vstest/releases)
- [Coverlet releases](https://github.com/coverlet-coverage/coverlet/releases)

## Mantenimiento, arquitectura, rendimiento y UX

- Se activaron lockfiles para los seis proyectos y `restore --locked-mode`.
- xUnit v2 se migró a xUnit v3, incluidos metadatos de atributos derivados y tokens de cancelación exigidos por sus analizadores.
- La detección de herramientas nativas ahora vive en un único componente compartido por CLI y WinUI. Detecta CMake incluido en Visual Studio aunque no esté en `PATH`, y CMake es un requisito real del gate nativo.
- El build nativo descarta cachés CMake creadas desde otro checkout o unidad. Esto evita rutas absolutas residuales como `D:\DEV` y conserva builds incrementales en el checkout actual.
- El smoke COM crea una colección aislada, valida menús modernos y clásicos, previews, límites de comandos y presupuesto de consulta, y limpia el fixture. Ya no depende de datos previos en `%USERPROFILE%\.icons`.
- `.vscode/tasks.json` contiene tareas cortas para restore, build, test, coverage, native, outdated, audit y ci. La tarea CI detiene la cadena en el primer fallo.
- `.gitignore` cubre salidas .NET, CMake, tests, cachés y scratch sin ocultar las tareas compartidas.
- Se eliminó la ruta local `D:\DEV\icon-replacer` de la configuración de VS Code. El RC staged, sus assets y la evidencia de release se preservaron.

## Verificación

- Restore locked: pass, 6/6 proyectos.
- Dependencias directas latest: pass. Vulnerables: 0. Deprecated: 0.
- Tests: 290 passed, 0 failed, 0 skipped.
- Coverage: 4,835/6,088 líneas (79.41%) y 1,345/2,217 branches (60.66%). Es evidencia diagnóstica; el repositorio no declara un umbral obligatorio.
- Solution Release: pass, incluidos WinUI, CLI, command host y shell extension nativa, con 0 warnings y 0 errors.
- Smoke COM x64 Release: pass; fixture temporal eliminado; consulta clásica dentro del presupuesto de 250 ms.
- CLI: help, shell-manifest y tooling smoke pass. Diagnostics reconoce compiler, MSBuild y CMake. Los estados `WinUI templates missing`, package/install proof y Explorer no configurado siguen siendo gates ambientales/de release, no defectos de build.

## Diez loops de calidad

1. Clasificación: .NET/WinUI puro; Bun y pnpm no aplican.
2. Dependencias: directas latest y migraciones mayores completadas.
3. Reproducibilidad: SDK y NuGet fijados con lockfiles.
4. Compilación: solución completa Release en verde.
5. Tests: suite y analizadores en verde.
6. Seguridad: auditoría NuGet sin vulnerabilidades ni paquetes deprecated.
7. Arquitectura: detección nativa deduplicada y gate CMake corregido.
8. Rendimiento: build incremental preservado; caché ajena sólo se limpia al detectar otro checkout; smoke mantiene presupuesto de consulta.
9. UX de desarrollo: tareas consistentes, diagnóstico de tooling exacto y smoke hermético.
10. Documentación y residuos: índice, mantenimiento, ignores, ruta VS Code y mapa sincronizados; artefactos de RC se preservan por diseño.

## Autopsia adversarial

- Un build limpio podía fallar por un `CMakeCache.txt` con rutas de otra unidad. Corregido y reproducido.
- `CanBuildNativeExtension` ignoraba CMake aunque el script lo necesita. Corregido con regresión automatizada.
- CLI y WinUI detectaban tooling con implementaciones distintas. Sustituidas por una sola fuente.
- El smoke podía fallar en una máquina limpia por falta de iconos del usuario. Ahora usa un fixture autocontenido y reversible.
- Una cadena de tareas con `;` podía continuar después de un fallo. Reemplazada por una cadena fail-fast.
- No se declara release-ready: faltan pruebas manuales de Explorer, instalación/desinstalación, High Contrast y 200% DPI. No se tocó la instalación existente.

## Gate

El estado técnico para continuar el desarrollo queda verde. La publicación del RC sigue bloqueada por los gates manuales y de instalación documentados. El RC, sus assets staged y cualquier instalación Explorer se preservan sin publicar ni reemplazar.
