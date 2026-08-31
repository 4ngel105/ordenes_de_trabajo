# 2. Actores del sistema

**Variante 6 · Órdenes de trabajo — "Taller y soporte técnico"**  
Roberto Angel Ayala Lecoña

Un actor es alguien (persona o sistema) que **espera algo del sistema**. Los ordeno por qué tanto lo tocan: primero quienes lo usan a diario, después el que solo recibe su resultado y por último el sistema externo con el que conversamos.

---

## Actores primarios (usan el sistema todos los días)

### Recepcionista — rol *operador*
- **Qué quiere:** registrar la orden en el menor tiempo posible mientras el cliente está parado frente al mostrador: datos del cliente, equipo, accesorios que deja y diagnóstico inicial de lo que el cliente reporta.
- **También quiere:** encontrar una orden por código, por documento del cliente o por teléfono cuando el cliente llama a preguntar; y cerrar la entrega dejando constancia de quién retiró el equipo.
- **Qué NO puede hacer:** asignar técnicos, cambiar prioridades ni tocar precios de repuestos.

### Técnico — rol *operador*
- **Qué quiere:** abrir el sistema y ver **solo su cola de trabajo**, ordenada por prioridad, sin tener que preguntarle al jefe qué sigue.
- **También quiere:** cargar el avance de lo que hizo, registrar los repuestos que consumió y mover el estado de la orden cuando termina la reparación.
- **Qué NO puede hacer:** asignarse órdenes a sí mismo ni modificar la prioridad que fijó el jefe.

### Jefe de Taller — rol *supervisor*
- **Qué quiere:** una foto honesta de quién tiene qué: cuántas órdenes activas carga cada técnico y cuáles están por vencer la fecha prometida.
- **También quiere:** asignar y reasignar órdenes con un motivo registrado, subir la prioridad de un caso urgente y ver el reporte de tiempos de resolución para decidir contrataciones o turnos.
- **Es el único que puede:** asignar, reasignar y repriorizar.

### Encargado de Almacén — rol *operador* (secundario)
- **Qué quiere:** que las órdenes reserven repuestos contra un stock que refleje lo que hay en el estante, y enterarse cuando un repuesto cruza su stock mínimo.
- **Qué NO puede hacer:** cambiar el estado de una orden.

---

## Actor externo (recibe el resultado, no opera el sistema)

### Cliente
- **Qué quiere:** una sola cosa —**cuándo va a estar listo mi equipo**— y no tener que llamar tres veces para averiguarlo.
- **Cómo interactúa:** no tiene usuario en el sistema. Recibe un aviso automático cuando su orden pasa a **LISTA** (RF5) y puede consultar el estado con su código de orden.
- **Por qué importa para la arquitectura:** este actor es la razón de que la **fiabilidad** sea un atributo crítico. El cliente cruza la ciudad porque el sistema dijo "lista"; si el estado miente, el costo lo paga una persona real.

---

## Sistema externo

### Servicio de Notificaciones (correo / mensajería)
- **Qué espera del sistema:** recibir un mensaje con destinatario, canal y contenido.
- **Qué le pedimos:** una confirmación de entrega o un error, para poder reintentar.
- **Por qué lo declaro como actor:** es un tercero que puede fallar o cambiar de proveedor. Aislarlo detrás de una frontera propia (M5) es lo que después permite meter un **Adapter** en H3 sin tocar el dominio de la orden.

---

## Resumen de permisos por rol (RF4)

| Acción | Recepcionista | Técnico | Jefe de Taller | Almacén |
|---|:--:|:--:|:--:|:--:|
| Registrar orden (RF1) | ✅ | — | ✅ | — |
| Buscar órdenes (RF2) | ✅ (todas) | ✅ (solo las suyas) | ✅ (todas) | — |
| Cargar avance | — | ✅ | ✅ | — |
| Cambiar estado a EN_REPARACION / LISTA | — | ✅ | ✅ | — |
| Entregar orden | ✅ | — | ✅ | — |
| Asignar / reasignar / repriorizar | — | — | ✅ | — |
| Consumir repuesto en una orden | — | ✅ | ✅ | ✅ |
| Ajustar stock y precios de repuestos | — | — | — | ✅ |
| Ver reportes (RF6) | — | solo su carga | ✅ | solo stock |

> Esta tabla es la que en H2/H3 justifica sacar los permisos de los `if` regados por las pantallas y llevarlos a una política por rol (candidato a **Strategy**).
