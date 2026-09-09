# Checklist maestro — Demo de portfolio Studio 01

**Línea base:** 2026-09-08  
**Regla de cierre:** una tarea pasa a `[x]` solo con test, captura, evento o enlace verificable.  
**Leyenda:** `[x]` cumplida · `[~]` parcial · `[ ]` pendiente · `[!]` bloqueo/riesgo.

## A. Mensaje y límites

- [x] Demo descrito como ingeniería con datos sintéticos.
- [x] Sin trading en vivo, órdenes, credenciales ni claims de rendimiento.
- [x] README: una decisión no es una orden.
- [x] Macro D1, Swing H1 y Execution M5 están representados en escenarios y en el gate de dominio.
- [~] Riesgo/ejecución están documentados como puertos, no implementados.
- [x] URL canónica: `https://labs.fernantezdev.pro/multi-timeframe-decision-platform`.
- [ ] Enlazar Studio 01 directamente con el demo.
- [ ] Registrar versión de despliegue y fuente de tráfico.

## B. Dominio e invariantes

- [x] Eventos de mercado inmutables.
- [x] Rechazo `LOOKAHEAD_REJECTED`.
- [x] Rechazo `DUPLICATE_EVENT_REJECTED`.
- [x] Fuera de orden se rechaza antes de registrar el ID como procesado.
- [x] Contratos explícitos para evidencia, decisión, transición y reason code.
- [x] Expiración reconcilia la fase con la evidencia activa requerida.
- [x] Invalidación limpia evidencia activa; sin posición emite `Wait`, no `Exit`.
- [x] H1 → M5 → M5 llega a `EntryReady` con timeline.
- [x] Macro D1 actúa como gate explícito para el contexto Swing H1.
- [x] `PositionClosed` llega a `Cooldown` y tiene prueba de contrato.
- [ ] Definir/prueba de reintento fuera de orden.
- [x] Expiración macro, swing, pullback y confirmación están definida y cubierta por pruebas con timestamps.
- [x] Gate macro D1 implementado y cubierto por prueba de contrato.
- [x] `SetupInvalidated` sin posición limpia el setup y emite `Wait`; el caso está probado.

## C. Pruebas y trazabilidad

- [x] Unit tests: secuencia feliz, duplicado y look-ahead.
- [x] Test de fuera de orden con timestamps.
- [x] Tests de TTL macro, swing, pullback y confirmación; además invalidación y cooldown.
- [x] Test macro D1 y documentación alineada.
- [x] Paridad batch/replay cubierta para una secuencia ordenada; dataset dorado sigue siendo una mejora futura.
- [ ] Prueba E2E API → UI.
- [ ] Evidencia de navegación por teclado y lector de pantalla.

## D. UX de demo

- [x] Selección y carga de escenarios sintéticos.
- [x] Fase, intención, evidencia activa y timeline visibles.
- [x] Límite de no trading y no claims visible.
- [~] Progreso por evento, sin anterior/reinicio explícitos.
- [x] Tarjetas de escenario con objetivo y resultado esperado.
- [x] Evento visible: timestamp, timeframe, lado, fuente y versión de reglas.
- [~] Fase, evidencia activa y explicación humana del reason code visibles; falta visualización explícita de evidencia expirada.
- [x] Diagrama de autoridad y límite de riesgo/ejecución visible.
- [x] Estados de carga, error y finalización implementados.
- [ ] Foco visible, headings semánticos y `aria-live`.
- [ ] Revisión WCAG 2.2 de contraste y objetivos táctiles.

## E. CTAs y embudo

- [x] Studio 01 tiene `Discuss a similar project`.
- [x] CTA primario `Open the interactive architecture demo` en Studio 01.
- [ ] CTA secundario a decisiones/arquitectura.
- [x] CTA de activación `Run the guided replay`.
- [x] CTA post-replay `Discuss a similar decision system`.
- [x] Back to Study Case 01 abre en misma pestaña hacia https://fernantezdev.pro/case-studies/multi-timeframe-algorithmic-decision-platform.
- [~] URLs centralizadas en el frontend del demo; falta configuración por entorno para despliegue.
- [ ] Copy final sin promesas financieras.
- [ ] Recorrido probado: Studio 01 → demo → contacto → retorno.

## F. Medición y privacidad

- [!] No existe telemetría: aún no se validan CTAs.
- [ ] Acordar plataforma, consentimiento, propietario y retención.
- [ ] Registrar `demo_view` y versión.
- [ ] Registrar `scenario_loaded` y `replay_completed`.
- [ ] Registrar `cta_clicked` sin datos personales.
- [ ] Registrar `contact_started`.
- [ ] Marcar `generate_lead` solo ante contacto válido.
- [ ] Verificar eventos en prueba/producción.
- [ ] Revisar embudo a 14 días y documentar decisión.

## G. Operación y entrega

- [x] Build backend/frontend correcto en línea base.
- [x] Auditorías NuGet/npm sin vulnerabilidades en línea base.
- [x] Docker Compose para API y web.
- [~] CORS amplio; aceptable localmente, no para producción pública.
- [~] Lockfile existe, pero `package.json` usa `latest`.
- [~] Existe CI; falta verificar build UI, auditorías y E2E.
- [!] Árbol Git sin rastrear: no hay línea base de commits verificable.
- [ ] Restringir CORS al dominio publicado.
- [ ] Fijar versiones frontend.
- [ ] Verificar/ampliar CI y smoke check.
- [ ] Inicializar/confirmar commits antes de publicación.

## Siguiente iteración

**Recomendada:** desplegar una versión de prueba y ejecutar el recorrido E2E Studio 01 → demo → contacto → retorno; después incorporar analítica con consentimiento.

**Evidencia mínima para publicar CTAs:** despliegue accesible, recorrido E2E correcto y revisión de accesibilidad manual; analítica solo tras consentimiento.









