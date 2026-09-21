using System.Collections.Generic;
using System.Linq;

// Controla las decisiones de UNA civilización manejada por la IA (se crea
// una instancia por cada oponente IA — con 4 civilizaciones y 1 humano,
// habría 3 de estos activos a la vez).
//
// Es reactiva a propósito: no tiene un bucle propio que la haga "pensar"
// cada X segundos. En vez de eso se suscribe a los eventos de sus propias
// unidades y edificios (FueAtacada, FueAtacado, Muerte) y decide en el
// momento en que algo relevante ocurre. Esto evita crear un hilo/Task
// adicional solo para "vigilar" el estado del juego.
//
// NOTA: depende de Mapa.UnidadesEnRadio(fila, columna, radio), que es el
// método pendiente de agregar (Opción A que hablamos con tu compañera).
public class ControladorIA
{
    private readonly Jugador miJugador;
    private readonly string civilizacion;
    private readonly Mapa mapa;
    private readonly Partida partida;
    private readonly GestorEntrenamiento gestorEntrenamiento;

    private const int RADIO_DETECCION = 3;

    public ControladorIA(Jugador miJugador, string civilizacion, Mapa mapa, Partida partida, GestorEntrenamiento gestorEntrenamiento)
    {
        this.miJugador = miJugador;
        this.civilizacion = civilizacion;
        this.mapa = mapa;
        this.partida = partida;
        this.gestorEntrenamiento = gestorEntrenamiento;
    }

    // Se llama una vez por cada unidad nueva de esta IA (al entrenarla o
    // colocarla), para que la IA empiece a "escuchar" lo que le pasa.
    public void ObservarUnidad(Unidad unidad)
    {
        unidad.FueAtacada += AlSerAtacada;
        unidad.Muerte += AlMorirUnidad;
    }

    // Se llama una vez por cada edificio nuevo de esta IA.
    public void ObservarEdificio(Edificio edificio)
    {
        edificio.FueAtacado += AlSerAtacadoEdificio;
    }

    // Lo llama el Controlador de movimiento de la IA cada vez que ella
    // misma termina de mover una unidad — así revisa si quedó cerca de
    // un enemigo y decide si conviene atacar.
    public void AlTerminarMovimiento(Unidad unidadMovida, int fila, int columna)
    {
        if (unidadMovida.Vida <= 0) return;

        var enemigosCercanos = BuscarUnidadesEnemigasCerca(fila, columna);
        if (enemigosCercanos.Count == 0) return;

        var objetivo = EvaluadorObjetivos.ElegirObjetivo(enemigosCercanos);
        if (objetivo != null) partida.EjecutarAtaque(miJugador, unidadMovida, objetivo);
    }

    // Reacciona de inmediato cuando atacan a una de sus unidades: si sigue
    // viva, contraataca a quien la golpeó. Así la IA nunca se queda "sin
    // hacer nada" mientras la golpean, sin necesitar un timer aparte.
    private void AlSerAtacada(Unidad victima, Unidad atacante)
    {
        if (victima.Vida <= 0) return; // ya murió, AlMorirUnidad se encarga de la limpieza
        partida.EjecutarAtaque(miJugador, victima, atacante);
    }

    // Si atacan un edificio y no queda ninguna unidad viva cerca para
    // defenderlo, entrena un defensor de emergencia (si el oro alcanza).
    // Es deliberadamente simple: la parte de "estrategia económica" completa
    // (cuándo construir, cuándo expandirse) queda para una segunda pasada.
    private void AlSerAtacadoEdificio(Edificio edificio, float daño)
    {
        if (edificio.Vida <= 0) return;
        bool tieneDefensores = miJugador.Unidades.Any(u => u.Vida > 0);
        if (!tieneDefensores)
        {
            gestorEntrenamiento.EntrenarUnidad(miJugador, "Vanguard", civilizacion);
        }
    }

    private void AlMorirUnidad(Unidad unidad)
    {
        unidad.Muerte -= AlMorirUnidad; // se desuscribe: ya no queda nada que escuchar en esta unidad
    }

    private List<Unidad> BuscarUnidadesEnemigasCerca(int fila, int columna)
    {
        var posiciones = mapa.UnidadesEnRadio(fila, columna, RADIO_DETECCION);
        var enemigos = new List<Unidad>();
        foreach (var (f, c) in posiciones)
        {
            Celda celda = mapa.ObtenerCelda(f, c);
            if (celda?.Unidad != null && !miJugador.Unidades.Contains(celda.Unidad))
                enemigos.Add(celda.Unidad);
        }
        return enemigos;
    }
}