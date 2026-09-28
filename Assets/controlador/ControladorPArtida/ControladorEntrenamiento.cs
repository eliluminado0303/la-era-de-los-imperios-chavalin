using System.Linq;
// Recibe las solicitudes de entrenamiento (aldeanos y unidades militares),
// valida que el edificio pertenezca al jugador, y delega al propio Edificio
// (que ya conoce su Civilizacion y llama a GestorEntrenamiento).
// También drena la cola de resultados terminados: sin esto, una unidad
// entrenada nunca llega a jugador.Unidades.
public class ControladorEntrenamiento
{
    private readonly Mapa mapa; // <-- nuevo campo
    private readonly Jugador miJugador;
    private readonly GestorEntrenamiento gestorEntrenamiento;
    private readonly ControladorIA controladorIA; // null si este jugador es el humano
    

    public ControladorEntrenamiento(Jugador miJugador, GestorEntrenamiento gestorEntrenamiento, Mapa mapa, ControladorIA controladorIA = null)
    {
        this.miJugador = miJugador;
        this.gestorEntrenamiento = gestorEntrenamiento;
        this.mapa = mapa;
        this.controladorIA = controladorIA;
    }

    public bool SolicitarAldeano(EdificioPrincipal centroUrbano)
    {
        if (centroUrbano == null || !miJugador.Edificios.Contains(centroUrbano)) return false;
        return centroUrbano.ProducirAldeano(miJugador, gestorEntrenamiento);
    }

    public bool SolicitarUnidad(EdificioEntrenamiento cuartel, string tipoUnidad)
    {
        if (cuartel == null || !miJugador.Edificios.Contains(cuartel)) return false;
        return cuartel.ProducirUnidad(miJugador, gestorEntrenamiento, tipoUnidad);
    }

    // Llamar UNA VEZ POR FRAME desde el Update() de Unity (una vez por cada
    // jugador, humano o IA). El hilo secundario solo avisa que terminó; quien
    // aplica el resultado al Modelo es siempre el hilo principal aquí, para
    // no tocar las listas del Jugador desde dos hilos a la vez.
    public void ActualizarResultados(float deltaTime = 0f)
    {
        while (gestorEntrenamiento.ResultadosPendientes.TryDequeue(out ResultadoEntrenamiento resultado))
        {
            if (resultado.Resultado is Aldeano aldeano)
            {
                miJugador.AgregarAldeano(aldeano);
                ColocarAldeanoCercaDelCentroUrbano(aldeano);
            }
            else if (resultado.Resultado is Unidad unidad)
            {
                miJugador.Unidades.Add(unidad);
                ColocarUnidadCercaDelCuartel(unidad);
                controladorIA?.ObservarUnidad(unidad);
            }
        }

        controladorIA?.EvaluarEconomia(deltaTime);
        // Decide (con cooldown propio en segundos reales) si manda una
        // oleada de unidades libres a atacar la base enemiga más cercana,
        // en vez de dejarlas paradas o solo deambulando cerca de casa.
        controladorIA?.EvaluarMilitar(deltaTime);
    }
        private void ColocarUnidadCercaDelCuartel(Unidad unidad)
    {
        var cuartel = miJugador.Edificios.OfType<EdificioEntrenamiento>().FirstOrDefault();
        if (cuartel == null) return;

        (int deltaFila, int deltaColumna)[] posiciones = { (1, 1), (1, -1), (-1, 1), (-1, -1), (0, 2), (2, 0), (0, -2), (-2, 0) };
        foreach (var (deltaFila, deltaColumna) in posiciones)
        {
            int f = cuartel.Fila + deltaFila, c = cuartel.Columna + deltaColumna;
            if (mapa.EsPosicionValida(f, c) && mapa.CeldaLibre(f, c))
            {
                mapa.ColocarUnidad(f, c, unidad);
                return;
            }
        }
    }
        private void ColocarAldeanoCercaDelCentroUrbano(Aldeano aldeano)
    {
        var centroUrbano = miJugador.Edificios.OfType<EdificioPrincipal>().FirstOrDefault();
        if (centroUrbano == null) return;

        (int deltaFila, int deltaColumna)[] posiciones = { (1, 1), (1, -1), (-1, 1), (-1, -1), (0, 2), (2, 0), (0, -2), (-2, 0) };
        foreach (var (deltaFila, deltaColumna) in posiciones)
        {
            int f = centroUrbano.Fila + deltaFila, c = centroUrbano.Columna + deltaColumna;
            if (mapa.EsPosicionValida(f, c) && mapa.CeldaLibre(f, c))
            {
                mapa.ColocarAldeano(f, c, aldeano);
                return;
            }
        }
    }
}
