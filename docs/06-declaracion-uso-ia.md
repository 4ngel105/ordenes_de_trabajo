# Declaración de uso de IA

**Variante 6 · Órdenes de trabajo — "Taller y soporte técnico"**  
Roberto Angel Ayala Lecoña · H1 · 30-ago-2026

La consigna permite usar IA como copiloto **declarándolo**: qué le pedí, qué acepté y qué corregí. Acá está.

---

## Herramienta

**Claude (Claude Code)**, usada como copiloto de redacción y diagramación del expediente.

---

## H1 — qué le pedí

- Estructurar el expediente del H1 con los 5 puntos que exige el documento del caso, para la Variante 6 y con Angular como tecnología del prototipo.
- Redactar el borrador de los actores, el inventario de módulos, el primer diagrama de clases en Mermaid y el argumento de los dos atributos de calidad.
- Escribir la sintaxis Mermaid del diagrama de clases y del diagrama de estados.

## Qué acepté del borrador

- La estructura del repositorio (README + `docs/`) y la separación de un documento por punto.
- La sintaxis Mermaid de los diagramas: es trabajo mecánico, no una decisión de arquitectura.
- El formato de las tablas de permisos, transiciones y trazabilidad RF → módulo → clase.

## Qué es decisión mía y defiendo como propia

Estas son las decisiones que **no** delegué, y son las que me van a preguntar en la defensa:

1. **La elección de la variante y de Angular**, por mi rubro real y por el tipo de uso del sistema (panel interno de jornada completa).
2. **El corte de los módulos**: separar M2 (Asignación) de M1 (Órdenes) porque tienen **distinto ritmo de cambio**. Este corte es la razón de ser de mi atributo crítico de mantenibilidad.
3. **Agregar el estado `CANCELADA`**, que no está en el enunciado, porque en el taller real la orden muere antes de repararse y sin ese estado terminal el reporte de tiempos del RF6 queda contaminado.
4. **Modelar `Asignacion` como clase con historial** en vez de un campo `tecnicoId` en la orden, porque la reasignación es cotidiana y el RF6 necesita saber quién tenía qué y cuándo.
5. **Ratificar mantenibilidad y fiabilidad** como críticos y **declarar explícitamente que sacrifico eficiencia**, con el umbral en el que esa decisión se reabre (5.000 órdenes activas).
6. **Tratar la trazabilidad como mecanismo de la fiabilidad y no como un tercer atributo**, para no contar dos veces la misma decisión.

## Qué corregí del borrador

<!--
COMPLETAR AL REVISAR: anotar acá lo que cambiaste del borrador con tus palabras.
Ejemplos de lo que corresponde anotar:
- términos que en tu taller se dicen distinto,
- roles o permisos que ajustaste a cómo funciona en la realidad,
- atributos de clase que agregaste o sacaste,
- reglas de transición que cambiaste.
Esta sección es la evidencia más fuerte de autoría en la defensa: sin ella, el
borrador es de la herramienta; con ella, el expediente es tuyo.
-->

- _(pendiente de completar tras la revisión personal del borrador)_

---

## Compromiso

Todo lo que está en este expediente lo puedo explicar y defender. Si en la defensa no puedo justificar una decisión, esa decisión no es mía y corresponde que se me observe.
