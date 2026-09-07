// H2 · SIGOT — EL DESPUÉS (los 5 principios trabajando juntos)
// El mismo taller, la misma orden de trabajo, la misma regla de negocio.
// Lo único que cambió es QUIÉN sabe qué y CÓMO consigue sus herramientas.
// Se lee en 4 actos: CONTRATOS → MODELO → PIEZAS → COORDINADORES.
// Este archivo es el espejo de h2/despues.drawio: cada interfaz de acá es una caja verde allá.

namespace Sigot.Despues;

// ============ ACTO 1 · CONTRATOS (los define el negocio: DIP) ============
// Ninguno menciona SQL, SMTP ni WhatsApp. Solo dicen QUÉ se necesita.

// [O] Open/Closed + [L] Liskov: cada estado de la orden sabe sola a dónde puede ir.
// Mañana entra un estado nuevo (EN_ESPERA_DE_REPUESTO) como una clase más, por esta puerta,
// sin abrir ninguna clase existente. Antes esto era un switch.
public interface IEstadoDeOrden
{
    string Nombre();
    bool PuedeTransicionarA(string destino);
}

// [O] La política de asignación del jefe de taller cambia por temporada.
// Cada política es una pieza; cambiar de política no toca el servicio que asigna.
public interface IEstrategiaDeAsignacion
{
    Tecnico ElegirTecnico(Orden orden, List<Tecnico> candidatos);
}

// [I] Interface Segregation: contrato chico y justo — UN método, enviar un aviso.
// Quien lo firme no queda obligado a nada que no use.
public interface ICanalDeEnvio
{
    void Enviar(string destino, string mensaje);
}

// [D] El dominio publica QUE algo pasó; no sabe quién escucha ni por dónde avisa.
// Esta es la interfaz de la que depende la orden: nunca de un notificador concreto.
public interface IPublicadorDeEventos
{
    void Publicar(string destino, string mensaje);
}

// [I] El reporte solo necesita LEER. Este contrato no tiene Guardar():
// que M6 sea de solo lectura deja de ser disciplina del programador y pasa a ser un tipo.
public interface IConsultaDeOrdenes
{
    List<Orden> Todas();
}

// El contrato de escritura es OTRO, y extiende al de lectura. Quien guarda también consulta;
// quien solo consulta jamás recibe Guardar().
public interface IRepositorioDeOrdenes : IConsultaDeOrdenes
{
    void Guardar(Orden orden);
}

// ============ ACTO 2 · MODELO ============
// Las clases que son el negocio. Saben de sí mismas y nada más.

public class Avance
{
    public string EstadoAnterior { get; }
    public string EstadoNuevo { get; }
    public string Autor { get; }

    public Avance(string estadoAnterior, string estadoNuevo, string autor)
    {
        EstadoAnterior = estadoAnterior;
        EstadoNuevo = estadoNuevo;
        Autor = autor;
    }
}

public class UsoDeRepuesto
{
    public string Descripcion { get; }
    public int Cantidad { get; }
    public decimal PrecioUnitario { get; }

    public UsoDeRepuesto(string descripcion, int cantidad, decimal precioUnitario)
    {
        Descripcion = descripcion;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
    }
}

// [S] Single Responsibility: la orden SABE de sí misma —su código, su estado, su bitácora—
// y nada más. Ya NO calcula costos, ya NO calcula tiempos y ya NO fabrica notificadores.
public class Orden
{
    public string Codigo { get; }
    public string ContactoCliente { get; }
    public string EspecialidadRequerida { get; }
    public DateTime FechaRecepcion { get; }
    public DateTime FechaPrometida { get; }
    public DateTime? FechaCierre { get; private set; }
    public IEstadoDeOrden Estado { get; private set; }          // ← guarda el CONTRATO, no un enum
    public List<Avance> Bitacora { get; } = new();
    public List<UsoDeRepuesto> Repuestos { get; } = new();

    public Orden(string codigo, string contactoCliente, string especialidadRequerida,
                 DateTime recepcion, DateTime prometida)
    {
        Codigo = codigo;
        ContactoCliente = contactoCliente;
        EspecialidadRequerida = especialidadRequerida;
        FechaRecepcion = recepcion;
        FechaPrometida = prometida;
        Estado = new EstadoRecibida();
    }

    // Mueve el estado y deja rastro. La validación de SI puede moverse la hace el estado;
    // el orden de los pasos lo dirige el coordinador. La orden solo ejecuta y anota.
    public void MoverA(IEstadoDeOrden destino, string autor)
    {
        Bitacora.Add(new Avance(Estado.Nombre(), destino.Nombre(), autor));
        Estado = destino;
        if (destino.Nombre() == "ENTREGADA") FechaCierre = new DateTime(2026, 9, 4);
    }

    public void ConsumirRepuesto(string descripcion, int cantidad, decimal precioUnitario)
        => Repuestos.Add(new UsoDeRepuesto(descripcion, cantidad, precioUnitario));
}

// [S] La identidad del técnico (M0): quién es y con qué se autentica.
public class Tecnico
{
    public string Nombre { get; }
    public string Usuario { get; }
    public string Especialidad { get; }
    public PerfilDeCapacidad Capacidad { get; }

    public Tecnico(string nombre, string usuario, string especialidad, PerfilDeCapacidad capacidad)
    {
        Nombre = nombre;
        Usuario = usuario;
        Especialidad = especialidad;
        Capacidad = capacidad;
    }

    public bool EsDe(string especialidad) => Especialidad == especialidad;
}

// [S] Su capacidad de trabajo (M2): cuánto aguanta y cuánto lleva encima.
// En el ANTES esto vivía dentro de Tecnico, mezclado con el email y la contraseña.
public class PerfilDeCapacidad
{
    public int CapacidadMaxima { get; }
    public int OrdenesActivas { get; private set; }

    public PerfilDeCapacidad(int capacidadMaxima, int ordenesActivas)
    {
        CapacidadMaxima = capacidadMaxima;
        OrdenesActivas = ordenesActivas;
    }

    public int CargaActual() => OrdenesActivas;
    public bool EstaDisponible() => OrdenesActivas < CapacidadMaxima;
    public void TomarUna() => OrdenesActivas++;
}

// ============ ACTO 3 · PIEZAS INTERCAMBIABLES ============
// Cada una firma un contrato del acto 1 y lo cumple sin sorpresas.

// --- Los estados: el switch del ANTES, partido en una clase por estado. [O][L] ---
// [L] Todas responden PuedeTransicionarA() sin efectos secundarios y sin lanzar excepciones:
// por eso el coordinador puede tratarlas a todas igual, sin preguntar cuál es cuál.
public class EstadoRecibida : IEstadoDeOrden
{
    public string Nombre() => "RECIBIDA";
    public bool PuedeTransicionarA(string destino) => destino == "DIAGNOSTICADA" || destino == "CANCELADA";
}

public class EstadoDiagnosticada : IEstadoDeOrden
{
    public string Nombre() => "DIAGNOSTICADA";
    public bool PuedeTransicionarA(string destino) => destino == "EN_REPARACION" || destino == "CANCELADA";
}

public class EstadoEnReparacion : IEstadoDeOrden
{
    public string Nombre() => "EN_REPARACION";
    public bool PuedeTransicionarA(string destino) => destino == "LISTA" || destino == "DIAGNOSTICADA";
}

public class EstadoLista : IEstadoDeOrden
{
    public string Nombre() => "LISTA";
    public bool PuedeTransicionarA(string destino) => destino == "ENTREGADA";
}

// Estados terminales: no se sale de acá. Y "no poder moverse" es una pieza más,
// no un caso especial escrito con un if en otro lado.
public class EstadoEntregada : IEstadoDeOrden
{
    public string Nombre() => "ENTREGADA";
    public bool PuedeTransicionarA(string destino) => false;
}

public class EstadoCancelada : IEstadoDeOrden
{
    public string Nombre() => "CANCELADA";
    public bool PuedeTransicionarA(string destino) => false;
}

// --- Las políticas de asignación del jefe de taller. [O] ---
public class AsignacionPorMenorCarga : IEstrategiaDeAsignacion
{
    public Tecnico ElegirTecnico(Orden orden, List<Tecnico> candidatos)
    {
        Tecnico elegido = null;
        foreach (var tecnico in candidatos)
        {
            if (!tecnico.Capacidad.EstaDisponible()) continue;
            if (elegido == null || tecnico.Capacidad.CargaActual() < elegido.Capacidad.CargaActual())
            {
                elegido = tecnico;
            }
        }
        return elegido;
    }
}

// La MISMA orden y los MISMOS candidatos dan otro resultado con esta política.
// Cambiar de regla es cambiar esta pieza: ServicioDeAsignacion no se toca.
public class AsignacionPorEspecialidad : IEstrategiaDeAsignacion
{
    public Tecnico ElegirTecnico(Orden orden, List<Tecnico> candidatos)
    {
        foreach (var tecnico in candidatos)
        {
            if (tecnico.EsDe(orden.EspecialidadRequerida) && tecnico.Capacidad.EstaDisponible())
            {
                return tecnico;
            }
        }
        return null;
    }
}

// --- Los canales de aviso. [I][D][L] Mismo contrato, tecnologías distintas. ---
public class CanalCorreoSmtp : ICanalDeEnvio
{
    public void Enviar(string destino, string mensaje)
        => Console.WriteLine($"[SMTP] Correo a {destino}: \"{mensaje}\"");
}

public class CanalWhatsAppApi : ICanalDeEnvio
{
    public void Enviar(string destino, string mensaje)
        => Console.WriteLine($"[WHATSAPP] {destino}: \"{mensaje}\"");
}

// Pieza de PRUEBA: cumple el mismo contrato pero no manda nada a nadie.
// Gracias a ella se puede probar todo el ciclo de vida sin un servidor de correo.
// Esto era literalmente imposible en el ANTES.
public class CanalRegistroInterno : ICanalDeEnvio
{
    public void Enviar(string destino, string mensaje)
        => Console.WriteLine($"[REGISTRO INTERNO] (no se envio nada) para {destino}: \"{mensaje}\"");
}

// --- Los cálculos que salieron de la orden. [S] ---
// A propósito NO tienen interfaz: hay una sola fórmula de costo y una sola de SLA.
// SRP justifica separarlas; OCP todavía no justifica abstraerlas. Una interfaz acá
// sería decorativa, y los principios decorativos son los que se caen en la defensa.
public class CalculadoraDeCostos
{
    public decimal CostoTotal(Orden orden)
    {
        decimal total = 0;
        foreach (var uso in orden.Repuestos)
        {
            total += uso.PrecioUnitario * uso.Cantidad;
        }
        return total;
    }
}

public class MetricasDeOrden
{
    public int DiasDeResolucion(Orden orden)
        => ((orden.FechaCierre ?? new DateTime(2026, 9, 4)) - orden.FechaRecepcion).Days;

    public bool EstaVencida(Orden orden)
        => orden.Estado.Nombre() != "ENTREGADA" && new DateTime(2026, 9, 4) > orden.FechaPrometida;
}

// --- El almacenamiento. Firma los dos contratos: sabe leer y sabe escribir. ---
public class RepositorioEnMemoria : IRepositorioDeOrdenes
{
    private readonly List<Orden> _ordenes = new();

    public void Guardar(Orden orden) => _ordenes.Add(orden);
    public List<Orden> Todas() => _ordenes;
}

// ============ ACTO 4 · COORDINADORES (alto nivel) ============

// [S][D] Una sola responsabilidad: el FLUJO de mover una orden por su ciclo de vida.
// No sabe validar transiciones (eso es del estado) ni avisar (eso es del publicador): DIRIGE.
public class ServicioDeOrdenes
{
    private readonly IPublicadorDeEventos _eventos;    // ← contrato, no notificador concreto

    // Inyección por constructor: la pieza entra ya construida, desde afuera.
    // En toda esta clase no hay ni un solo new de sus herramientas.
    public ServicioDeOrdenes(IPublicadorDeEventos eventos) => _eventos = eventos;

    public void CambiarEstado(Orden orden, IEstadoDeOrden destino, string autor)
    {
        // [O][L] Le pregunta al estado actual, sea cual sea. Ni un switch, ni un if por tipo.
        if (!orden.Estado.PuedeTransicionarA(destino.Nombre()))
        {
            Console.WriteLine($"[RECHAZADO] {orden.Codigo}: de {orden.Estado.Nombre()} no se puede pasar a {destino.Nombre()}");
            return;
        }

        Console.WriteLine($"[BITACORA] {orden.Codigo}: {orden.Estado.Nombre()} -> {destino.Nombre()} (por {autor})");
        orden.MoverA(destino, autor);

        // [D] Publica el evento del negocio contra un contrato. No sabe si atrás
        // hay un correo, un WhatsApp o nada: por eso agregar canales no la toca.
        if (destino.Nombre() == "LISTA")
        {
            _eventos.Publicar(orden.ContactoCliente, $"Su orden {orden.Codigo} está lista para retirar");
        }
    }
}

// [D] Del otro lado del contrato: este servicio ES un publicador, y a su vez
// depende del contrato ICanalDeEnvio. La inversión ocurre en los dos extremos.
public class ServicioDeNotificacion : IPublicadorDeEventos
{
    private readonly ICanalDeEnvio _canal;

    public ServicioDeNotificacion(ICanalDeEnvio canal) => _canal = canal;

    public void Publicar(string destino, string mensaje) => _canal.Enviar(destino, mensaje);
}

// [S][D] Coordina la asignación. La REGLA la pone la estrategia; él solo la aplica y registra.
public class ServicioDeAsignacion
{
    private readonly IEstrategiaDeAsignacion _estrategia;

    public ServicioDeAsignacion(IEstrategiaDeAsignacion estrategia) => _estrategia = estrategia;

    public Tecnico Asignar(Orden orden, List<Tecnico> candidatos)
    {
        var elegido = _estrategia.ElegirTecnico(orden, candidatos);
        if (elegido == null)
        {
            Console.WriteLine($"[ASIGNACION] {orden.Codigo}: no hay tecnico disponible");
            return null;
        }
        Console.WriteLine($"[ASIGNACION] {orden.Codigo} -> {elegido.Nombre} (carga {elegido.Capacidad.CargaActual()}/{elegido.Capacidad.CapacidadMaxima})");
        return elegido;
    }
}

// [I] Recibe IConsultaDeOrdenes, no el repositorio completo.
// Aunque quisiera, no puede escribir: Guardar() no existe en su contrato.
public class ReporteOperativo
{
    private readonly IConsultaDeOrdenes _consulta;

    public ReporteOperativo(IConsultaDeOrdenes consulta) => _consulta = consulta;

    public void OrdenesPorEstado()
    {
        string[] estados = { "RECIBIDA", "DIAGNOSTICADA", "EN_REPARACION", "LISTA", "ENTREGADA", "CANCELADA" };
        foreach (var estado in estados)
        {
            int cuantas = 0;
            foreach (var orden in _consulta.Todas())
            {
                if (orden.Estado.Nombre() == estado) cuantas++;
            }
            if (cuantas > 0) Console.WriteLine($"[REPORTE] {estado}: {cuantas} orden(es)");
        }
    }
}

// ============ DEMO ============

public static class Demo
{
    // Lleva una orden recién recibida hasta LISTA. Sirve para mostrar que el MISMO
    // servicio produce avisos distintos según la pieza que le hayan enchufado.
    private static void LlevarHastaLista(ServicioDeOrdenes servicio, Orden orden, string tecnico)
    {
        servicio.CambiarEstado(orden, new EstadoDiagnosticada(), "recepcion");
        servicio.CambiarEstado(orden, new EstadoEnReparacion(), "jefe");
        servicio.CambiarEstado(orden, new EstadoLista(), tecnico);
    }

    public static void Correr()
    {
        // Los new NO desaparecieron: se MUDARON acá, a quien ARMA el sistema.
        var ana = new Tecnico("Ana", "aquiroga", "electronica", new PerfilDeCapacidad(5, 3));
        var bruno = new Tecnico("Bruno", "bmamani", "mecanica", new PerfilDeCapacidad(4, 1));
        var candidatos = new List<Tecnico> { ana, bruno };

        var orden = new Orden("OT-002", "cliente@correo.com", "electronica",
                              new DateTime(2026, 9, 1), new DateTime(2026, 9, 3));

        // ---- [O] La misma orden y los mismos candidatos, con dos politicas distintas ----
        Console.WriteLine("-- politica del jefe: menor carga --");
        new ServicioDeAsignacion(new AsignacionPorMenorCarga()).Asignar(orden, candidatos);

        Console.WriteLine("-- el jefe cambio la politica: se cambia la PIEZA, no el servicio --");
        var tecnicoAsignado = new ServicioDeAsignacion(new AsignacionPorEspecialidad()).Asignar(orden, candidatos);

        // ---- [O][D] Ciclo de vida completo, avisando por correo ----
        Console.WriteLine("-- produccion: aviso por correo --");
        var servicioCorreo = new ServicioDeOrdenes(new ServicioDeNotificacion(new CanalCorreoSmtp()));
        LlevarHastaLista(servicioCorreo, orden, tecnicoAsignado.Nombre);

        orden.ConsumirRepuesto("Fuente 500W", 1, 180.00m);
        orden.ConsumirRepuesto("Pasta termica", 1, 25.50m);

        // El estado LISTA se defiende solo: no hace falta un switch en ningun lado.
        servicioCorreo.CambiarEstado(orden, new EstadoCancelada(), "jefe");

        // [S] Los calculos los hace quien corresponde, no la orden.
        Console.WriteLine($"Costo total: {new CalculadoraDeCostos().CostoTotal(orden):0.00} Bs (lo calcula CalculadoraDeCostos)");
        Console.WriteLine($"Vencida: {new MetricasDeOrden().EstaVencida(orden)} (lo calcula MetricasDeOrden)");

        // ---- [D] Mismo servicio, otra pieza de aviso ----
        Console.WriteLine("-- el cliente prefiere WhatsApp: se cambia la PIEZA, no el servicio --");
        var ordenWhats = new Orden("OT-003", "+591 70011223", "mecanica",
                                   new DateTime(2026, 9, 2), new DateTime(2026, 9, 5));
        LlevarHastaLista(new ServicioDeOrdenes(new ServicioDeNotificacion(new CanalWhatsAppApi())), ordenWhats, "Bruno");

        // ---- [D] Mismo servicio, sin mandar nada: imposible en el ANTES ----
        Console.WriteLine("-- prueba sin servidor de correo: mismo servicio, canal de mentira --");
        var ordenPrueba = new Orden("OT-004", "cliente2@correo.com", "electronica",
                                    new DateTime(2026, 9, 2), new DateTime(2026, 9, 6));
        LlevarHastaLista(new ServicioDeOrdenes(new ServicioDeNotificacion(new CanalRegistroInterno())), ordenPrueba, "Ana");

        // El cliente vino a retirar: OT-002 se cierra. Pasar a ENTREGADA no avisa nada.
        servicioCorreo.CambiarEstado(orden, new EstadoEntregada(), "recepcion");

        // ---- [I] El reporte solo puede leer ----
        Console.WriteLine("-- reporte: recibe el contrato de LECTURA, no el repositorio --");
        var repositorio = new RepositorioEnMemoria();
        repositorio.Guardar(orden);
        repositorio.Guardar(ordenWhats);
        repositorio.Guardar(ordenPrueba);

        new ReporteOperativo(repositorio).OrdenesPorEstado();
        // new ReporteOperativo(repositorio).Guardar(...);  ← ni siquiera compila: no existe en IConsultaDeOrdenes

        Console.WriteLine("Un flujo, 5 principios juntos. Ningun servicio se abrio para cambiar politica ni canal.");
    }
}
