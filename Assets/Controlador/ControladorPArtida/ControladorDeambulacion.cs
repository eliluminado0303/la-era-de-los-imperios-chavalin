using System.Collections.Generic;
using System.Linq;

// Hace que las unidades y los aldeanos "libres" (sin nada mejor que hacer) caminen solos
// cerca de donde están, en vez de quedarse paradas para siempre — aplica
// tanto al jugador humano como a cada IA (una instancia por jugador, igual
// que ControladorEdificiosEspeciales). Es deambular, no patrullar: no sigue
// una ruta fija, cada tanto elige un destino al azar cerca de su posición
// actual. Un aldeano solo deambula si no está recolectando; una unidad
// militar, si no tiene un enemigo cerca.
//
// Reutiliza el MISMO camino que un movimiento pedido por el jugador
// (ControladorMapa.SolicitarMovimiento -> GestorMovimiento), así que de
// paso hereda gratis la reacción de combate de ControladorIA: si una unidad
// de la IA deambulando termina cerca de un enemigo, ataca igual que si el
// movimiento hubiera sido a propósito (ver ControladorIA.AlTerminarMovimiento).
public class ControladorDeambulacion
{
    private const float INTERVALO_MINIMO_SEGUNDOS = 4f;
    private const float INTERVALO_MAXIMO_SEGUNDOS = 9f;
    private const int RADIO_DEAMBULACION = 2;       // qué tan lejos de sí misma puede vagar en un paso
    private const int RADIO_DETECCION_COMBATE = 3;  // si hay un enemigo así de cerca, prioriza pelear y no deambula

    private static readonly System.Random aleatorio = new System.Random();

    private readonly Jugador miJugador;
    private readonly Mapa mapa;
    private readonly ControladorMapa controladorMapa;

    // Cooldown propio por unidad, para que no todas deambulen sincronizadas
    // ni se les dispare un movimiento nuevo antes de que termine el anterior
    // (GestorMovimiento tarda ~1s en aplicar cada movimiento).
    private readonly Dictionary<Unidad, float> tiempoHastaProximoDeambular = new Dictionary<Unidad, float>();
    private readonly Dictionary<Aldeano, float> tiempoHastaProximoDeambularAldeano = new Dictionary<Aldeano, float>();

    // Opcional: si devuelve true para una unidad, no se la hace deambular
    // (por ejemplo, porque va caminando hacia un enemigo para atacarlo).
    public System.Func<Unidad, bool> UnidadOcupada { get; set; }

    public ControladorDeambulacion(Jugador miJugador, Mapa mapa, ControladorMapa controladorMapa)
    {
        this.miJugador = miJugador;
        this.mapa = mapa;
        this.controladorMapa = controladorMapa;
    }

    // "unidadExcluida": la que el jugador humano tiene seleccionada ahora
    // mismo en la Vista (si la hay) — no tiene sentido que se te vaya
    // caminando justo cuando estás por darle una orden. Las IA siempre
    // pasan null acá (no tienen concepto de "seleccionada").
    // "aldeanoExcluido": igual, pero para el aldeano elegido en la lista del HUD.
    public void Actualizar(float deltaTime, Unidad unidadExcluida = null, Aldeano aldeanoExcluido = null)
    {
        DeambularAldeanos(deltaTime, aldeanoExcluido);

        foreach (var unidad in miJugador.Unidades.ToList())
        {
            if (unidad.Vida <= 0 || unidad == unidadExcluida) continue;
            if (UnidadOcupada != null && UnidadOcupada(unidad)) continue;

            if (!tiempoHastaProximoDeambular.TryGetValue(unidad, out float restante))
                restante = SiguienteIntervalo();

            restante -= deltaTime;
            if (restante > 0) { tiempoHastaProximoDeambular[unidad] = restante; continue; }

            tiempoHastaProximoDeambular[unidad] = SiguienteIntervalo(); // se reinicia pase lo que pase

            if (!BuscarPosicionActual(unidad, out int filaActual, out int columnaActual)) continue;
            if (HayEnemigoCerca(unidad, filaActual, columnaActual)) continue; // prioriza combate, no deambula

            if (ElegirDestinoLibre(filaActual, columnaActual, out int filaDestino, out int columnaDestino))
                controladorMapa.SolicitarMovimiento(filaActual, columnaActual, filaDestino, columnaDestino);
        }
    }

    // Los aldeanos que no están recolectando caminan cerca de donde están.
    // A diferencia de las unidades, el Aldeano SÍ guarda su propia
    // Fila/Columna (Mapa.ColocarAldeano/MoverAldeano las mantienen), así
    // que no hace falta buscarlo en la cuadrícula.
    private void DeambularAldeanos(float deltaTime, Aldeano aldeanoExcluido)
    {
        foreach (var aldeano in miJugador.Aldeanos.ToList())
        {
            if (!aldeano.EstaVivo || aldeano.Ocupado || aldeano == aldeanoExcluido) continue;
            if (aldeano.Fila < 0 || aldeano.Columna < 0) continue; // todavía sin colocar

            if (!tiempoHastaProximoDeambularAldeano.TryGetValue(aldeano, out float restante))
                restante = SiguienteIntervalo();

            restante -= deltaTime;
            if (restante > 0) { tiempoHastaProximoDeambularAldeano[aldeano] = restante; continue; }

            tiempoHastaProximoDeambularAldeano[aldeano] = SiguienteIntervalo();

            if (ElegirDestinoLibre(aldeano.Fila, aldeano.Columna, out int filaDestino, out int columnaDestino))
                controladorMapa.SolicitarMovimientoAldeano(aldeano, filaDestino, columnaDestino);
        }

        // Limpia a los que ya no existen para que el diccionario no crezca.
        if (tiempoHastaProximoDeambularAldeano.Count > miJugador.Aldeanos.Count + 8)
        {
            var vivos = new HashSet<Aldeano>(miJugador.Aldeanos);
            foreach (var muerto in tiempoHastaProximoDeambularAldeano.Keys.Where(a => !vivos.Contains(a)).ToList())
                tiempoHastaProximoDeambularAldeano.Remove(muerto);
        }
    }

    private float SiguienteIntervalo()
        => INTERVALO_MINIMO_SEGUNDOS + (float)aleatorio.NextDouble() * (INTERVALO_MAXIMO_SEGUNDOS - INTERVALO_MINIMO_SEGUNDOS);

    // La Unidad no guarda su propia fila/columna (esa info vive en la Celda
    // que la contiene), así que hay que buscarla. Solo se hace cuando le
    // toca el turno de deambular a esa unidad (cada 4-9s), no por frame.
    private bool BuscarPosicionActual(Unidad unidad, out int fila, out int columna)
    {
        for (int f = 0; f < Mapa.FILAS; f++)
            for (int c = 0; c < Mapa.COLUMNAS; c++)
                if (mapa.ObtenerCelda(f, c).Unidad == unidad) { fila = f; columna = c; return true; }
        fila = columna = 0;
        return false;
    }

    private bool HayEnemigoCerca(Unidad unidad, int fila, int columna)
    {
        foreach (var (f, c) in mapa.UnidadesEnRadio(fila, columna, RADIO_DETECCION_COMBATE))
        {
            var otra = mapa.ObtenerCelda(f, c)?.Unidad;
            if (otra != null && otra.Civilizacion != unidad.Civilizacion && otra.Vida > 0) return true;
        }
        return false;
    }

    private bool ElegirDestinoLibre(int filaActual, int columnaActual, out int fila, out int columna)
    {
        for (int intento = 0; intento < 6; intento++)
        {
            int deltaFila = aleatorio.Next(-RADIO_DEAMBULACION, RADIO_DEAMBULACION + 1);
            int deltaColumna = aleatorio.Next(-RADIO_DEAMBULACION, RADIO_DEAMBULACION + 1);
            if (deltaFila == 0 && deltaColumna == 0) continue;

            int f = filaActual + deltaFila;
            int c = columnaActual + deltaColumna;
            if (mapa.EsPosicionValida(f, c) && mapa.CeldaLibre(f, c)) { fila = f; columna = c; return true; }
        }
        fila = columna = 0;
        return false;
    }
}