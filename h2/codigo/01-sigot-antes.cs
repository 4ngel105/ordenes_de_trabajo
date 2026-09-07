// H2 · SIGOT — EL ANTES (el diagrama del H1, hecho código)
// Órdenes de trabajo de un taller: recibir, diagnosticar, reparar, entregar y avisar al cliente.
// La regla de negocio funciona… pero está atornillada a sus detalles y hace de todo.
// Este archivo es el espejo de h2/antes.drawio: los mismos 6 olores, en C#.

namespace Sigot.Antes;

public enum EstadoOrden
{
    RECIBIDA, DIAGNOSTICADA, EN_REPARACION, LISTA, ENTREGADA, CANCELADA
}

// Detalle de BAJO nivel: el canal de aviso. Clase concreta, sin ningún contrato.
public class NotificadorSmtp
{
    public void Enviar(string destino, string mensaje)
    {
        Console.WriteLine($"[SMTP] Correo a {destino}: \"{mensaje}\"");
    }
}

// Una línea de repuesto consumido en la reparación.
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

// LA CLASE GORDA. Acá viven los seis olores del H1.
public class OrdenDeTrabajo
{
    public string Codigo { get; }
    public string EmailCliente { get; }
    public EstadoOrden Estado { get; private set; }
    public DateTime FechaRecepcion { get; }
    public DateTime FechaPrometida { get; }
    public DateTime? FechaCierre { get; private set; }
    public List<UsoDeRepuesto> Repuestos { get; } = new();

    public OrdenDeTrabajo(string codigo, string emailCliente, DateTime recepcion, DateTime prometida)
    {
        Codigo = codigo;
        EmailCliente = emailCliente;
        FechaRecepcion = recepcion;
        FechaPrometida = prometida;
        Estado = EstadoOrden.RECIBIDA;
    }

    // OLOR 2 — SWITCH POR TIPO (rompe OCP).
    // La tabla de transiciones vive dentro de la clase. Agregar un estado nuevo
    // obliga a abrir este switch, o sea a MODIFICAR la clase que custodia el ciclo de vida.
    public bool PuedeTransicionarA(EstadoOrden destino)
    {
        switch (Estado)
        {
            case EstadoOrden.RECIBIDA:
                return destino == EstadoOrden.DIAGNOSTICADA || destino == EstadoOrden.CANCELADA;
            case EstadoOrden.DIAGNOSTICADA:
                return destino == EstadoOrden.EN_REPARACION || destino == EstadoOrden.CANCELADA;
            case EstadoOrden.EN_REPARACION:
                return destino == EstadoOrden.LISTA || destino == EstadoOrden.DIAGNOSTICADA;
            case EstadoOrden.LISTA:
                return destino == EstadoOrden.ENTREGADA;
            default:
                return false;   // ENTREGADA y CANCELADA son terminales
        }
    }

    public void CambiarEstado(EstadoOrden destino, string autor)
    {
        if (!PuedeTransicionarA(destino))
        {
            Console.WriteLine($"[RECHAZADO] {Codigo}: de {Estado} no se puede pasar a {destino}");
            return;
        }

        Console.WriteLine($"[BITACORA] {Codigo}: {Estado} -> {destino} (por {autor})");
        Estado = destino;
        if (destino == EstadoOrden.ENTREGADA) FechaCierre = new DateTime(2026, 9, 4);

        // OLOR 3 y 4 — new INCRUSTADO EN EL DOMINIO (rompe DIP).
        // La orden fabrica su propio notificador. Quedó casada con el correo para siempre:
        // si el cliente prefiere WhatsApp hay que abrir esta clase, y no hay manera de
        // probar el ciclo de vida sin mandar un correo de verdad.
        if (destino == EstadoOrden.LISTA)
        {
            var notificador = new NotificadorSmtp();          // ← atornillado al correo
            notificador.Enviar(EmailCliente, $"Su orden {Codigo} está lista para retirar");
        }
    }

    public void ConsumirRepuesto(string descripcion, int cantidad, decimal precioUnitario)
    {
        Repuestos.Add(new UsoDeRepuesto(descripcion, cantidad, precioUnitario));
    }

    // OLOR 1 — CLASE GORDA (rompe SRP).
    // Estos tres métodos no tienen nada que ver con custodiar estados: son la fórmula
    // de facturación y el cálculo de SLA. Cambia el IVA y hay que tocar la misma clase
    // que guarda la máquina de estados, y volver a probar todo el ciclo de vida.
    public decimal CostoTotal()
    {
        decimal total = 0;
        foreach (var uso in Repuestos)
        {
            total += uso.PrecioUnitario * uso.Cantidad;
        }
        return total;
    }

    public TimeSpan TiempoDeResolucion() => (FechaCierre ?? new DateTime(2026, 9, 4)) - FechaRecepcion;

    public bool EstaVencida() => Estado != EstadoOrden.ENTREGADA && new DateTime(2026, 9, 4) > FechaPrometida;
}

// OLOR 6 — DOBLE RESPONSABILIDAD (rompe SRP).
// Tecnico es a la vez la identidad que se autentica (M0) y el recurso con capacidad
// de trabajo (M2). Dos módulos distintos, dos ritmos de cambio, una sola clase.
public class Tecnico
{
    public string Nombre { get; }
    public string Usuario { get; }          // ← identidad (M0)
    public string Email { get; }            // ← identidad (M0)
    public string Especialidad { get; }     // ← capacidad de trabajo (M2)
    public int CapacidadMaxima { get; }     // ← capacidad de trabajo (M2)
    public int OrdenesActivas { get; set; } // ← capacidad de trabajo (M2)

    public Tecnico(string nombre, string usuario, string email, string especialidad, int capacidadMaxima, int ordenesActivas)
    {
        Nombre = nombre;
        Usuario = usuario;
        Email = email;
        Especialidad = especialidad;
        CapacidadMaxima = capacidadMaxima;
        OrdenesActivas = ordenesActivas;
    }

    public bool PuedeEjecutar(string accion) => accion == "registrarAvance";   // identidad
    public int CargaActual() => OrdenesActivas;                                // capacidad
    public bool EstaDisponible() => OrdenesActivas < CapacidadMaxima;          // capacidad
}

// OLOR 5 — MÓDULO DE LECTURA CON ACCESO TOTAL (rompe ISP y DIP).
// El reporte alcanza las clases concretas del dominio y ve su API completa,
// incluida CambiarEstado(), aunque por diseño M6 es de solo lectura.
public class ReporteOperativo
{
    private readonly List<OrdenDeTrabajo> _ordenes;

    public ReporteOperativo(List<OrdenDeTrabajo> ordenes) => _ordenes = ordenes;

    public void TiemposDeResolucion()
    {
        foreach (var orden in _ordenes)
        {
            Console.WriteLine($"[REPORTE] {orden.Codigo}: {orden.TiempoDeResolucion().Days} dias, {orden.CostoTotal():0.00} Bs");
            // Nada le impide escribir: orden.CambiarEstado(...) está a la mano.
        }
    }
}

public static class Demo
{
    public static void Correr()
    {
        var orden = new OrdenDeTrabajo(
            "OT-001", "cliente@correo.com",
            recepcion: new DateTime(2026, 9, 1),
            prometida: new DateTime(2026, 9, 3));

        // El ciclo de vida normal de una reparación.
        orden.CambiarEstado(EstadoOrden.DIAGNOSTICADA, "recepcion");
        orden.CambiarEstado(EstadoOrden.EN_REPARACION, "jefe");

        orden.ConsumirRepuesto("Fuente 500W", 1, 180.00m);
        orden.ConsumirRepuesto("Pasta termica", 1, 25.50m);

        // Al pasar a LISTA la orden fabrica su notificador y manda el correo. Sin alternativa.
        orden.CambiarEstado(EstadoOrden.LISTA, "tecnico");

        // El switch hace bien su trabajo: LISTA no puede cancelarse. Pero el precio es
        // que la regla vive dentro de la clase.
        orden.CambiarEstado(EstadoOrden.CANCELADA, "jefe");

        Console.WriteLine($"Costo total: {orden.CostoTotal():0.00} Bs (lo calcula la propia orden)");
        Console.WriteLine($"Vencida: {orden.EstaVencida()} (tambien lo calcula la propia orden)");

        Console.WriteLine("Funciona... pero un estado nuevo o un canal nuevo = abrir la clase, y sin correo real no hay como probarla.");
    }
}
