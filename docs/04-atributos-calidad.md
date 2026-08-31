# 5. Atributos de calidad críticos

**Variante 6 · Órdenes de trabajo — "Taller y soporte técnico"**  
Roberto Angel Ayala Lecoña

El caso sugiere **mantenibilidad** y **fiabilidad**. Los **ratifico**, pero no porque los sugiera el enunciado: abajo está el argumento, el escenario que los hace medibles y qué decisiones de arquitectura compro con cada uno. También digo explícitamente qué atributo **sacrifico** y por qué, porque un atributo que no compite con nada no es una decisión de arquitectura.

---

## 1. Mantenibilidad (modificabilidad) — CRÍTICO

### El argumento
Las reglas de asignación y priorización del taller cambian por decisión operativa, no por decisión técnica: hoy se asigna por especialidad, mañana por menor carga, en temporada alta por antigüedad del ticket, y cuando entra un cliente corporativo se asigna por contrato. **La regla que más cambia no puede vivir en el corazón del sistema.** Si `OrdenDeTrabajo` sabe cómo se elige un técnico, cada cambio de política obliga a tocar la clase que sostiene el ciclo de vida y a volver a probar los seis estados. Por eso corto M2 fuera de M1 y aíslo la decisión detrás de una interfaz: **una regla nueva debe ser una clase nueva, no una modificación**.

### Escenario de calidad (medible)
> **Fuente:** el jefe de taller. **Estímulo:** pide una nueva regla de asignación ("primero el ticket más viejo por encima de la especialidad"). **Artefacto:** módulo M2. **Entorno:** operación normal. **Respuesta:** se implementa una clase nueva que cumple `EstrategiaDeAsignacion` y se registra en la configuración. **Medida:** en producción en **≤ 1 día**, **0 archivos modificados en M1**, y las pruebas del ciclo de vida de la orden siguen pasando sin cambios.

### Qué compro con esto
- M2 separado de M1, con la decisión de asignación detrás de una interfaz (**Strategy** en H3).
- Los permisos por rol como política, no como `if` repartidos en las pantallas de Angular.
- Módulos Angular por *feature* con carga diferida: un cambio en `asignacion` no recompila ni re-despliega `ordenes`.

### Cómo se verifica
Prueba concreta para la defensa: agregar una tercera estrategia de asignación en vivo y mostrar que el diff no toca `OrdenDeTrabajo`.

---

## 2. Fiabilidad — CRÍTICO

### El argumento
El sistema entero existe para responder una pregunta: **¿en qué estado está el equipo?** El cliente cruza la ciudad porque le avisamos "lista"; el jefe reasigna porque el tablero dice "en reparación". Si el estado que se muestra no es el estado real, el sistema no es "impreciso": es **peor que no tenerlo**, porque la gente toma decisiones creyéndole. El daño no es técnico, lo paga una persona que vino en vano. Además, un aviso que se pierde en silencio (RF5) es indistinguible de un aviso nunca enviado, y termina en un cliente enojado y un técnico acusado sin evidencia.

### Escenario de calidad (medible)
> **Fuente:** un técnico. **Estímulo:** intenta pasar una orden de `RECIBIDA` directo a `LISTA` (o dos usuarios cambian la misma orden a la vez). **Artefacto:** M1. **Entorno:** operación normal. **Respuesta:** la transición inválida se rechaza en el dominio (no en la pantalla), se registra el intento y el estado persistido no cambia. **Medida:** **0 estados inválidos** en la base; **100 %** de los cambios de estado con `Avance` que registra autor y fecha; un aviso fallido queda en `FALLIDA` con `intentos > 0` y es visible en el listado, nunca perdido.

### Qué compro con esto
- Transiciones validadas en el dominio: no existe `setEstado()` público (invariante 1 del diagrama de clases).
- `Avance` como bitácora inmutable con autor y fecha: es lo que hace **auditable** el estado.
- `Notificacion` con `estadoEnvio` e `intentos`: el fallo se ve, se reintenta y no desaparece.
- `ENTREGADA` y `CANCELADA` como estados terminales: una orden cerrada no se reabre por accidente.

### Sobre la trazabilidad
No la declaro como un tercer atributo separado: **la trazabilidad es el mecanismo con el que consigo fiabilidad**, no un objetivo aparte. La bitácora `Avance` y el historial de `Asignacion` existen para que el estado mostrado sea verificable; si los pusiera como atributo independiente estaría contando dos veces la misma decisión.

---

## 3. Lo que NO priorizo (y por qué)

### Eficiencia — importante, no crítica
El enunciado pregunta qué pasa con la cola cuando entran 50 órdenes juntas. Mi respuesta: **50 órdenes simultáneas no es un problema de arquitectura, es un problema de índices.** Se resuelve con índices por `estado`, `tecnico` y `fechaRecepcion`, paginación en el listado y carga diferida en Angular. El cuello de botella real del taller es el **tiempo del técnico** (horas por reparación), no el tiempo de una consulta (milisegundos): optimizar la consulta no hace que las 50 órdenes salgan antes.

Y hay un costo si me equivoco de prioridad: las optimizaciones típicas (cachés de listado, desnormalizar el estado en varias tablas, precalcular la carga por técnico) **endurecen justo lo que más cambia** —las reglas de asignación— y además abren la puerta a que el estado cacheado no coincida con el real, que es exactamente el riesgo que la fiabilidad quiere cerrar. **Eficiencia y fiabilidad chocan acá, y elijo fiabilidad.**

**Umbral declarado:** el listado de órdenes responde en menos de 2 segundos con hasta 5.000 órdenes activas. Si el taller crece más allá de eso, se reabre la discusión con un ADR nuevo, no antes.

### Usabilidad — importante, no crítica
Importa (la recepcionista registra con el cliente parado enfrente), pero se corrige con cambios de pantalla, que son baratos. Un error de fiabilidad, en cambio, se paga con la confianza del cliente y no se corrige con un rediseño de formulario.

### Seguridad — presente como higiene, no como atributo dominante
Hay roles y permisos (RF4, M0), pero no manejo dinero ni enlaces de pago manipulables como la Variante 2. El riesgo real de mi dominio es el **dato falso**, no el acceso indebido.

---

## Resumen de la decisión

| Atributo | Nivel | Razón en una línea |
|---|---|---|
| **Mantenibilidad** | 🔴 Crítico | Las reglas de asignación cambian seguido: el cambio debe costar una clase nueva, no un refactor. |
| **Fiabilidad** | 🔴 Crítico | El estado que se muestra debe ser el real: la gente decide creyéndole al sistema. |
| Usabilidad | 🟡 Importante | Se arregla con pantallas; el costo de equivocarse es bajo y reversible. |
| Eficiencia | 🟡 Importante | 50 órdenes se resuelven con índices; el cuello de botella es humano. |
| Seguridad | 🟢 Higiene | Roles y permisos sí; no hay dinero en juego. |
| Portabilidad | 🟢 No prioritaria | Un taller, un despliegue. Optimizar por esto sería costo sin beneficio. |
