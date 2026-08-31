# Trazabilidad: RF → módulo → clases

**Variante 6 · Órdenes de trabajo — "Taller y soporte técnico"**  
Roberto Angel Ayala Lecoña

Este documento no es un punto obligatorio del H1: lo agrego para demostrar la **coherencia entre módulos y clases** que pide la rúbrica. Cada requerimiento funcional aterriza en un módulo dueño y en clases concretas del diagrama.

---

## RF1 · Registrar la entidad principal
**Aterrizaje:** registrar la orden con diagnóstico inicial, cliente y equipo.

- **Módulo dueño:** M1 · Recepción y Órdenes (apoyado por M3 para cliente y equipo).
- **Clases:** `OrdenDeTrabajo`, `Cliente`, `Equipo`.
- **Operación:** `Recepcionista.registrarOrden(cliente, equipo, diagnostico)`.
- **Validaciones:** código de orden único, cliente con documento y al menos un canal de contacto, equipo con tipo y número de serie (o marcado como "sin serie" con justificación), diagnóstico inicial no vacío, fecha prometida ≥ fecha de recepción.
- **Estado inicial:** `RECIBIDA`.
- **En Angular:** formulario reactivo con validadores síncronos y un validador asíncrono de número de serie duplicado en taller.

## RF2 · Listar y buscar con filtro útil
**Aterrizaje:** buscar órdenes por técnico, estado o cliente.

- **Módulo dueño:** M1 (consulta), con M2 para el filtro por técnico.
- **Clases:** `OrdenDeTrabajo`, `Asignacion`, `EstadoOrden`.
- **Filtros:** código de orden, documento o nombre del cliente, estado, técnico asignado, rango de fechas, prioridad, y el filtro que más usa el jefe: **"vencidas"** (`estaVencida()`).
- **En Angular:** listado paginado del lado del servidor con los filtros en la query string, para que un filtro sea compartible por link.

## RF3 · Flujo de estados
**Aterrizaje:** recibida → diagnosticada → en reparación → lista → entregada (más `CANCELADA`, decisión propia).

- **Módulo dueño:** M1, en exclusiva.
- **Clases:** `OrdenDeTrabajo`, `EstadoOrden`, `Avance`.
- **Operación:** `cambiarEstado(destino, autor)` validado por `puedeTransicionarA(destino)`.
- **Regla dura:** cada transición genera un `Avance` con autor y fecha; no hay cambio de estado sin rastro.
- Tabla completa de transiciones en [03-diagrama-clases.md](03-diagrama-clases.md).

## RF4 · Roles con permisos distintos
**Aterrizaje:** el técnico actualiza avances; el jefe de taller asigna y prioriza.

- **Módulo dueño:** M0 · Acceso y Roles (transversal), aplicado sobre M2.
- **Clases:** `Usuario` (abstracta), `Recepcionista`, `Tecnico`, `JefeDeTaller`.
- **Diferencia de permisos:** solo `JefeDeTaller` puede `asignar()`, `reasignar()` y `repriorizar()`; el `Tecnico` solo ve y mueve sus propias órdenes.
- **En Angular:** `rolGuard` en las rutas + directiva `*siPuede` para ocultar botones. La autorización real se revalida en el servidor: la pantalla oculta, no protege.
- Matriz completa de permisos en [01-actores.md](01-actores.md).

## RF5 · Notificar
**Aterrizaje:** aviso al cliente cuando su orden pasa a `LISTA`.

- **Módulo dueño:** M5 · Notificaciones.
- **Clases:** `Notificacion`, `Canal`, `EstadoEnvio`.
- **Disparador:** el evento `OrdenLista` que publica M1 al ejecutar la transición `EN_REPARACION → LISTA`. **M1 no conoce a M5** (candidato a **Observer** en H3).
- **Canal:** el de `Cliente.canalPreferido()`; si falla, queda `FALLIDA` con `intentos` y se reintenta. Siempre hay respaldo en `REGISTRO_INTERNO` para que el aviso quede registrado aunque el proveedor externo esté caído.
- **Otros avisos previstos:** `OrdenAsignada` al técnico y aviso interno cuando un repuesto queda bajo el mínimo.

## RF6 · Reportar
**Aterrizaje:** carga de trabajo por técnico y tiempos de resolución.

- **Módulo dueño:** M6 · Reportes e Indicadores.
- **Clases:** `ReporteOperativo`, con `Asignacion` y `OrdenDeTrabajo` como fuentes de solo lectura.
- **Salidas:**  
  - **Carga por técnico:** órdenes activas por técnico, comparadas con su `capacidadMaxima`.
  - **Tiempos de resolución:** promedio y máximo de `tiempoDeResolucion()` por período, excluyendo las `CANCELADA` (por eso ese estado importa: si no existiera, ensuciaría este promedio).
  - **Órdenes por estado** en el período, y cuántas vencieron su fecha prometida.
- **Regla dura:** M6 nunca escribe. Un reporte no modifica una orden.

---

## Matriz resumen

| RF | Módulo dueño | Módulos de apoyo | Clases protagonistas | Atributo de calidad que tensiona |
|---|---|---|---|---|
| RF1 Registrar | M1 | M3 | `OrdenDeTrabajo`, `Cliente`, `Equipo` | Usabilidad (registro rápido) |
| RF2 Buscar | M1 | M2 | `OrdenDeTrabajo`, `Asignacion` | Eficiencia (listado paginado) |
| RF3 Estados | M1 | — | `OrdenDeTrabajo`, `EstadoOrden`, `Avance` | **Fiabilidad** |
| RF4 Roles | M0 | M2 | `Usuario` y sus tres subclases | **Mantenibilidad** (permisos como política) |
| RF5 Notificar | M5 | M1 (evento) | `Notificacion`, `Canal` | **Fiabilidad** (aviso que no se pierde) |
| RF6 Reportar | M6 | M1, M2 | `ReporteOperativo`, `Asignacion` | Eficiencia (agregados) |

## Patrones que ya se ven venir (para H3)

| Patrón | Dónde | Qué problema resuelve |
|---|---|---|
| **Strategy** | M2, `EstrategiaDeAsignacion` | Las reglas de asignación cambian seguido: agregar una regla sin modificar la orden. |
| **Observer** | M1 → M5 | La orden publica `OrdenLista` sin conocer quién notifica ni por qué canal. |
| **State** | M1, transiciones | Reemplazar el `switch` de `puedeTransicionarA()` que crece con cada estado nuevo. |
| **Adapter** | M5 | Integrar proveedores de correo/mensajería intercambiables sin tocar el dominio. |
| **Factory** | M1 | Crear la orden según su tipo (garantía, reparación, mantenimiento preventivo) con reglas distintas. |

En H3 elijo **dos** y los desarrollo en serio; el resto queda como camino explorado y descartado, con su razón.
