# GitOps Pipeline — gates que realmente fallan el build, no reportes que nadie hace cumplir

Prototipo que documenta una modernización real de un pipeline CI/CD: de reportes
"informativos" (cobertura que se genera pero no se exige, sync waves que existen solo en
la intención) a gates que **bloquean de verdad** cuando se violan — y un cierre real del
lazo entre CI y CD, sin un paso manual.

## El problema

*"¿Cómo hacés que 'cobertura obligatoria' y 'sync waves' sean garantías reales que fallan
el build/el despliegue cuando se violan, en vez de reportes que nadie hace cumplir — y cómo
cerrás el lazo entre CI y CD sin un paso manual?"*

## Qué encontré corriendo de verdad (vs. lo que el sistema real dice hacer)

Investigué el Jenkinsfile real de un sistema de microservicios con despliegue vía ArgoCD,
y los manifiestos de Kustomize del repo de CD asociado. Varios hallazgos, algunos
sorprendentes:

- **El gate de CHANGELOG es real y bloquea el publish.** El script de validación corre
  dentro del stage de publish, exige un heading `## [version]` con al menos una línea de
  contenido no vacía debajo, y si falla, no se buildea ni se pushea ninguna imagen. Esta
  parte del sistema real funciona exactamente como se documenta.
- **La cobertura se colecta pero no se hace cumplir.** coverlet + ReportGenerator generan
  un HTML en cada build — pero no hay ningún umbral numérico que falle el build. Solo falla
  si falta el archivo de resultados o si hay tests rotos. Un módulo puede caer al 10% de
  cobertura sin que el pipeline se entere.
- **Los "sync waves" son aspiracionales.** Cero anotaciones
  `argocd.argoproj.io/sync-wave` existen en los manifiestos reales de CD. El orden de
  despliegue depende de que ArgoCD aplique los recursos en el orden en que Kubernetes los
  acepte, sin ninguna garantía explícita de que el namespace/config exista antes que el
  Deployment que lo necesita.
- **Jenkins y ArgoCD están completamente desacoplados.** Ningún Jenkinsfile actualiza el
  tag de imagen en el repo de CD ni lo commitea. Jenkins termina en `docker push`; alguien
  edita a mano el YAML del repo de CD para que ArgoCD reconcilie el nuevo tag.
- **Un `.runsettings` de Testcontainers documenta una decisión que el script real
  contradice.** El archivo de configuración de tests de integración deshabilita el reaper
  Ryuk con un comentario explicando por qué — pero el script que realmente exporta la
  variable de entorno lo hace con el valor contrario. La documentación quedó desincronizada
  del comportamiento real.

**Decisión:** en vez de replicar el sistema tal cual (que tiene gates reales conviviendo con
gates aspiracionales), construí las piezas que faltan o que no se hacen cumplir — un umbral
de cobertura que de verdad rompe el build, sync waves reales y verificables, y el cierre del
lazo CI→CD — todo corriendo en un pipeline público de GitHub Actions (sustituye a Jenkins
por motivos prácticos de portfolio, pero las etapas replican las del pipeline real: tests
unitarios e integración en paralelo, gate de cobertura, gate de CHANGELOG, publish).

## Qué construí

### `ValidateChangelog` — mismo criterio que el gate real

Busca `## [version]` en `CHANGELOG.md` y exige al menos una línea de contenido no vacía
antes del próximo heading. Probado contra 4 casos (archivo inexistente, versión ausente,
heading vacío, heading con contenido) — incluyendo correrlo contra el propio
[`CHANGELOG.md`](CHANGELOG.md) de este repo.

### `ComputeSyncOrder` — sync waves reales, no ilustrativas

A diferencia del sistema real (cero anotaciones de sync-wave en ningún lado), los
manifiestos en [`cd/dev/`](cd/dev/) tienen anotaciones `argocd.argoproj.io/sync-wave`
genuinas: namespace/config en wave 0, Redis en wave 1, la API de cache en wave 2.
`ComputeSyncOrder` lee `kustomization.yml`, agrupa los recursos por su wave, y devuelve el
orden exacto en que ArgoCD los aplicaría. El test lo verifica contra el YAML real del
repo, no contra un fixture aislado.

### El gate de cobertura que realmente rompe el build

La mejora central: en vez de generar un reporte que nadie exige, `Cache.UnitTests` usa
`coverlet.msbuild` con un umbral de 80% de línea (`-p:Threshold=80`) — si la cobertura cae
por debajo, `dotnet test` sale con código de error y el job de CI falla.

**`CoverageGate.IntegrationTests`** prueba que ese mecanismo funciona de verdad, no que
"debería": shellea `dotnet test` real contra dos fixtures gemelas
([`HighCoverageSample`](tests/CoverageGate.IntegrationTests/fixtures/HighCoverageSample) y
[`LowCoverageSample`](tests/CoverageGate.IntegrationTests/fixtures/LowCoverageSample)) — el
mismo `Calculator` de 4 métodos, el mismo umbral de 80%. La bien testeada (los 4 métodos
cubiertos) sale con código 0; la deliberadamente sub-testeada (solo 1 de 4 métodos, 25% de
cobertura) sale con código distinto de cero. El test lee `process.ExitCode` directamente —
no confía en ningún reporte de texto, confía en que el proceso realmente falló.

### `BumpImageTag` — cierra el lazo CI→CD

En el sistema real, nada conecta el `docker push` de Jenkins con el `newTag` del repo de
CD — se edita a mano. `BumpImageTag` hace ese paso mediante reemplazo de texto quirúrgico
(no un parseo+reserializado completo de YAML, para no reformatear el resto del archivo) y
el workflow de GitHub Actions lo ejecuta al final de un run exitoso en `main`, commiteando
el nuevo tag directamente a `cd/dev/kustomization.yml`.

## El pipeline real: [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml)

Seis jobs, corriendo en el propio repo público:

1. **`unit-tests`** — `ValidateChangelog`, `ComputeSyncOrder`, `BumpImageTag` (unitarios).
2. **`integration-tests`** — `Cache.IntegrationTests` contra Redis real vía Testcontainers.
3. **`coverage-gate`** — aplica el umbral de 80% sobre `Cache`, y corre
   `CoverageGate.IntegrationTests` (que a su vez shellea `dotnet test` dos veces más).
4. **`sync-order`** — corre los tests de `ComputeSyncOrder` e imprime el orden resuelto de
   `cd/dev/` para que quede visible en el log del run.
5. **`changelog-gate`** — extrae la versión vigente del propio `CHANGELOG.md` y valida esa
   entrada con `ValidateChangelog`.
6. **`bump-image-tag`** — solo en push a `main` y solo si los cinco jobs anteriores
   pasaron: bump del tag de imagen al SHA del commit, commit y push a `cd/dev/` con las
   credenciales del propio workflow.

El link al run verde es la prueba de que esto corre de verdad:
**[github.com/jose-oran/gitops-pipeline/actions](https://github.com/jose-oran/gitops-pipeline/actions)**

## Cómo correrlo localmente

```bash
# Todo lo que no necesita Docker
dotnet test tests/ValidateChangelog.UnitTests/ValidateChangelog.UnitTests.csproj
dotnet test tests/ComputeSyncOrder.UnitTests/ComputeSyncOrder.UnitTests.csproj
dotnet test tests/BumpImageTag.UnitTests/BumpImageTag.UnitTests.csproj

# Requiere Docker (Testcontainers levanta Redis real)
dotnet test tests/Cache.IntegrationTests/Cache.IntegrationTests.csproj

# El gate de cobertura real sobre Cache
dotnet test tests/Cache.UnitTests/Cache.UnitTests.csproj \
  -p:CollectCoverage=true -p:Threshold=80 -p:ThresholdType=line -p:ThresholdStat=total

# La prueba de que el gate funciona (shellea dotnet test dos veces más, por eso tarda)
dotnet test tests/CoverageGate.IntegrationTests/CoverageGate.IntegrationTests.csproj
```

## Qué haría distinto a escala

- **Entornos de preview conectados a servicios reales.** El sistema real tiene un
  `ApplicationSet` con generador de Pull Request funcionando, pero no conectado a los
  servicios específicos que más lo necesitarían — quedó documentado como posible, no
  implementado. Acá no lo reproduzco porque un solo servicio de ejemplo no justifica un
  generador de PR completo; a escala, sería el siguiente paso natural sobre esta misma base.
- **Umbral de cobertura por módulo, no global.** Un único 80% total esconde módulos
  críticos con 40% si otros compensan con 100% — a escala, ThresholdStat por módulo
  (`ThresholdStat=modules`) sería más honesto sobre qué exactamente está protegido.
- **El commit de `bump-image-tag` no dispara un segundo ArgoCD sync explícito** — asume
  que ArgoCD está en modo de sync automático observando el repo. Para un entorno con sync
  manual, el workflow necesitaría un paso adicional llamando a la API de ArgoCD.

## Arquitectura

```
src/
  Cache/                    wrapper mínimo sobre Redis (la "app" bajo prueba)
  ValidateChangelog/        puerto del gate real de CHANGELOG
  ComputeSyncOrder/         sync waves reales, no ilustrativas
  BumpImageTag/             cierra el lazo CI -> CD
tests/
  Cache.UnitTests/          coverlet.msbuild, umbral real de 80%
  Cache.IntegrationTests/   Redis real vía Testcontainers
  ValidateChangelog.UnitTests/
  ComputeSyncOrder.UnitTests/     contra el YAML real de cd/dev/
  BumpImageTag.UnitTests/         round-trip: bump -> releer -> mismo valor
  CoverageGate.IntegrationTests/  shellea dotnet test real, dos fixtures gemelas
cd/dev/                     manifiestos Kustomize reales con sync-wave genuinas
.github/workflows/ci-cd.yml el pipeline real
```

Stack: .NET 10, xUnit, coverlet (msbuild + collector), Testcontainers.Redis, YamlDotNet,
GitHub Actions.
