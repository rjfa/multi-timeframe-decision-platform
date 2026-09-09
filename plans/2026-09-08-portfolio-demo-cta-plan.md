# Plan de conversión para la demo del portfolio — Studio 01

**Línea base:** 2026-09-08  
**Ámbito:** `multi-timeframe-decision-platform` y su invocación desde `rf-software-consulting`.  
**Objetivo:** convertir la demo técnica en una prueba guiada y medible de Studio 01, sin claims financieros ni expansión hacia trading real.

## Hipótesis falsable

Si un visitante llega desde Studio 01, entiende el invariante demostrado, completa un replay guiado y recibe un CTA contextual hacia contacto, aumentará la proporción de sesiones con interacción cualificada y leads frente a la línea base. La línea base no está instrumentada ni tiene commit/branch verificable: antes de comparar resultados, registrar versión de despliegue y medir durante 14 días visitas, replays y CTAs.

## Decisiones confirmadas

| Decisión | Valor | Estado |
|---|---|---|
| URL canónica del demo | `https://labs.fernantezdev.pro/multi-timeframe-decision-platform` | Confirmada |
| Destino de contacto | `https://fernantezdev.pro/#contact` | Confirmado |
| Alcance del host `labs` | Se publicarán más proyectos/demos | Confirmado |
| URL canónica de Studio 01 | https://fernantezdev.pro/case-studies/multi-timeframe-algorithmic-decision-platform | Confirmada |
| Apertura de enlaces | Misma pestaña; retorno visible `Back to Study Case 01` | Confirmada |
| Atribución/analítica | Pendiente de consentimiento y proveedor | Pendiente |
## Límites obligatorios

- `Intent.Enter` es intención semántica, no una orden, recomendación ni señal ejecutable.
- El demo usa eventos sintéticos; no debe prometer rendimiento ni trading en vivo.
- Los cambios de conversión no deben ocultar invariantes incumplidos: primero se corrige la verdad de dominio y después se mejora el relato.
- Analítica solo tras aprobar consentimiento, retención y política de privacidad.

## Fase 0 — Contrato de conversión

**Resultado:** una ruta y una medida inequívocas antes de crear CTAs.

1. Elegir URL pública canónica del demo. Recomendación: `demo.fernantezdev.pro/multi-timeframe-decision-platform`.
2. Configurar, por entorno y fuera del dominio, `portfolioCaseUrl`, `contactUrl` y, si se aprueba, `architectureUrl`.
3. Definir conversión de negocio: contacto enviado válido, no clic.
4. Definir propietario del dato, consentimiento, retención y proveedor analítico.

**Aceptación:** URLs, conversión y política quedan registradas; se puede asociar cada despliegue a una versión.

## Fase 1 — Corregir los invariantes publicados

**Resultado:** el demo no afirma comportamientos que el núcleo pueda violar.

1. Reconciliar expiración de evidencia y fase.
   - Hipótesis: tras expirar el swing en `T + 8h`, un pullback no puede avanzar sin reconstruir contexto.
   - Prueba: evento H1 en `T`, pullback después de TTL y resultado `WaitContext` o rechazo explícito.
2. Definir la semántica de fuera de orden.
   - Elegir entre rechazo permanente con cursor o reintento posterior válido.
   - No marcar el ID como procesado antes de aplicar la regla elegida.
3. Hacer D1/macro demostrable con una regla mínima de gate o retirar esa promesa de copy/documentación.
4. Aclarar si `Invalidated` es historial o evidencia activa y qué intención corresponde cuando no hay posición.
5. Añadir pruebas de expiración, orden, invalidación, cooldown, macro y paridad batch/replay.

**Aceptación:** tests con timestamps, reason codes y transiciones esperadas; UI, docs y código usan la misma semántica.

## Fase 2 — Replay guiado y accesible

**Resultado:** una persona no técnica comprende el caso sin leer código.

1. Sustituir slugs por tarjetas: nombre, objetivo arquitectónico, pasos y resultado esperado.
2. Antes del primer clic, explicar problema, regla de vela cerrada, TTL/evidencia, límite de no-ejecución y resultado observable.
3. Por evento mostrar timestamp, timeframe, lado, fase anterior/siguiente, evidencia añadida/expirada, reason code e intención.
4. Añadir diagrama: evidencia → roadmap → intención → riesgo futuro → ejecución futura.
5. Incorporar `Run guided replay`, anterior, siguiente, reiniciar, carga, error y finalización.
6. Reemplazar `label` no asociados por semántica adecuada; añadir foco visible y `aria-live` para el resultado dinámico.

**Aceptación:** una prueba de usuario puede explicar qué evita la arquitectura y no interpreta la pantalla como consejo financiero.

## Fase 3 — Embudo de CTA

| Ubicación | CTA | Intención | Destino |
|---|---|---|---|
| Studio 01 | `Open the interactive architecture demo` | Demostrar capacidad | Demo público |
| Studio 01 | `Read the architecture decisions` | Profundizar | Arquitectura/ADR público |
| Cabecera del demo | `Run the guided replay` | Activación | Escenario recomendado |
| Replay finalizado | `Discuss a similar decision system` | Lead cualificado | Contacto del portfolio |
| Demo persistente | Back to Study Case 01 | Continuidad | https://fernantezdev.pro/case-studies/multi-timeframe-algorithmic-decision-platform |

1. Añadir el CTA de demo en `CaseStudyPage` del portfolio externo.
2. Presentar el CTA comercial tras un replay, sin competir con la activación inicial.
3. Usar lenguaje de arquitectura: `decision system`, `evidence`, `authority boundaries`, `explainable behavior`.
4. Atribuir sin datos personales: `source=studio-01-demo`, `scenario=<id>`, `completion=<true|false>`.

**Aceptación:** Studio 01 → demo → replay → contacto → Studio 01 funciona sin URL manual ni pérdida de contexto.

## Fase 4 — Medición y decisión

1. Registrar `demo_view`, `scenario_loaded`, `replay_completed`, `cta_clicked`, `contact_started` y `generate_lead` al confirmar un contacto válido.
2. Incluir versión de despliegue, escenario, ubicación del CTA y estado de finalización; no datos personales.
3. Verificar eventos en entorno de prueba y con una sesión controlada en producción.
4. A los 14 días revisar visitas → inicio → finalización → CTA → contacto → lead.
5. Modificar solo una variable por experimento: copy, ubicación o contexto.

**Aceptación:** existe un informe por fuente y versión que fundamenta conservar, iterar o retirar un CTA.

## Fase 5 — Operación pública

1. Restringir CORS al dominio publicado.
2. Fijar versiones frontend en `package.json`; conservar lockfile.
3. Verificar CI: test backend, build frontend y auditorías.
4. Añadir E2E API → UI y smoke/health check de despliegue.
5. Confirmar que no se exponen adapters, credenciales, datos privados ni métricas de rendimiento.

## Orden recomendado y evidencia mínima

1. Fase 0: URL, consentimiento y conversión definidos.
2. Fase 1: tests de invariantes en verde.
3. Fase 2: prueba E2E y navegación por teclado.
4. Fase 3: recorrido real de CTA desde Studio 01.
5. Fases 4–5: evento recibido, embudo de 14 días y controles operativos.




