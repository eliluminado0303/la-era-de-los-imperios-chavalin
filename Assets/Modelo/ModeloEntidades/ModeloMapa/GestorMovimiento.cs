using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;


public class ResultadoMovimiento 
{
    public bool Exitoso { get; set; }
    public int Fila { get; set; }
    public int Columna { get; set; }
    public Unidad Unidad { get; set; }
    // Solo se llena cuando el movimiento fue de un Aldeano (en ese caso Unidad es null).
    // Se llena SIEMPRE que era un aldeano, haya salido bien o mal, para que
    // quien recibe el resultado sepa a quién se refiere.
    public Aldeano Aldeano { get; set; }
}


public class GestorMovimiento 
{
    public ConcurrentQueue<ResultadoMovimiento> ResultadosPendientes { get; private set; }
    private readonly GestorArchivos gestorArchivos = new GestorArchivos();

    public GestorMovimiento() 
    {
        ResultadosPendientes = new ConcurrentQueue<ResultadoMovimiento>();
    }

    // Cuánto tarda una unidad en llegar según su Velocidad (así Ralentizado
    // y las unidades lentas/rápidas se notan). Velocidad 5 = ~1s. Tope máximo
    // de 1.4s: ControladorCombate espera 1.5s antes de reintentar acercarse.
    public static float DuracionSegundos(Unidad unidad)
    {
        float velocidad = unidad != null ? Math.Max(1, unidad.Velocidad) : 5;
        return Math.Max(0.5f, Math.Min(1.4f, 5f / velocidad));
    }

    public bool IniciarMovimiento(Mapa mapa, int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino, string nombreJugador)
    {
    Unidad unidad = mapa.ObtenerCelda(filaOrigen, columnaOrigen)?.Unidad;
    if (unidad == null || unidad.EnMovimiento) return false;
    if (!mapa.ReservarDestino(filaDestino, columnaDestino)) return false;

    unidad.EnMovimiento = true;
    Task.Run(() => MoverEnSegundoPlano(mapa, unidad, filaOrigen, columnaOrigen, filaDestino, columnaDestino, nombreJugador));
    return true;
    }
    // Mismo patrón que IniciarMovimiento pero para un Aldeano: una tarea
    // en segundo plano espera lo que tarda el desplazamiento y luego lo
    // aplica al Mapa (que valida bajo su propio candado).
    public void IniciarMovimientoAldeano(Mapa mapa, Aldeano aldeano, int filaDestino, int columnaDestino, string nombreJugador, int duracionMs = 1000)
    {
        Task.Run(() => {
            MoverAldeanoEnSegundoPlano(mapa, aldeano, filaDestino, columnaDestino, nombreJugador, duracionMs);
        });
    }

    private void MoverAldeanoEnSegundoPlano(Mapa mapa, Aldeano aldeano, int filaDestino, int columnaDestino, string nombreJugador, int duracionMs)
    {
        Task.Delay(duracionMs).Wait();

        // Si murió mientras caminaba, MoverAldeano lo rechaza y no reaparece.
        bool movido = mapa.MoverAldeano(aldeano, filaDestino, columnaDestino);

        gestorArchivos.RegistrarEvento(
            nombreJugador,
            "Movimiento",
            movido ? $"{aldeano.Nombre} movido a ({filaDestino},{columnaDestino})" : $"Movimiento de {aldeano.Nombre} fallido"
        );

        ResultadosPendientes.Enqueue(new ResultadoMovimiento {
            Exitoso = movido,
            Fila = filaDestino,
            Columna = columnaDestino,
            Aldeano = aldeano
        });
    }

    private void MoverEnSegundoPlano(Mapa mapa, Unidad unidadAMover, int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino, string nombreJugador)
    {
        bool movido = false;
        string motivo = "desconocido";

        try
        {
            Task.Delay((int)(DuracionSegundos(unidadAMover) * 1000f)).Wait();

            Celda celdaOrigen = mapa.ObtenerCelda(filaOrigen, columnaOrigen);
            Celda celdaDestino = mapa.ObtenerCelda(filaDestino, columnaDestino);

            // Se calcula ANTES de mover, solo para poder explicar un fallo en el log.
            motivo = celdaOrigen?.Unidad != unidadAMover ? "la unidad ya no está en el origen (murió o se movió)"
                   : celdaDestino == null ? "destino fuera del mapa"
                   : celdaDestino.Terreno != TipoTerreno.Tierra ? "destino es agua"
                   : celdaDestino.Unidad != null ? "destino ocupado por otra unidad"
                   : celdaDestino.Edificio != null ? "destino ocupado por un edificio"
                   : "desconocido";

            // Solo se mueve si la que está en el origen ES la misma unidad
            // (si murió y otra ocupó su lugar, no se debe mover a la otra).
            if (celdaOrigen?.Unidad == unidadAMover)
                movido = mapa.MoverUnidad(filaOrigen, columnaOrigen, filaDestino, columnaDestino);
        }
        finally
        {
            // Pase lo que pase, la reserva y el flag se liberan: si no, la
            // celda o la unidad quedarían bloqueadas para siempre.
            mapa.LiberarReserva(filaDestino, columnaDestino);
            unidadAMover.EnMovimiento = false;
        }

        gestorArchivos.RegistrarEvento(
            nombreJugador,
            "Movimiento",
            movido ? $"Unidad movida a ({filaDestino},{columnaDestino})"
                   : $"Movimiento fallido ({filaOrigen},{columnaOrigen}) -> ({filaDestino},{columnaDestino}): {motivo}"
        );

        ResultadosPendientes.Enqueue(new ResultadoMovimiento {
            Exitoso = movido,
            Fila = filaDestino,
            Columna = columnaDestino,
            Unidad = movido ? unidadAMover : null
        });
    }
}