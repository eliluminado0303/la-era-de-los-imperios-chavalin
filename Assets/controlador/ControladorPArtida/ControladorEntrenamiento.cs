// Recibe las solicitudes de entrenamiento (aldeanos y unidades militares),
// valida que el edificio pertenezca al jugador, y delega al propio Edificio
// (que ya conoce su Civilizacion y llama a GestorEntrenamiento).
// También drena la cola de resultados terminados: sin esto, una unidad
// entrenada nunca llega a jugador.Unidades.
public class ControladorEntrenamiento
{
    private readonly Jugador miJugador;
    private readonly GestorEntrenamiento gestorEntrenamiento;
    private readonly ControladorIA controladorIA; // null si este jugador es el humano

    public ControladorEntrenamiento(Jugador miJugador, GestorEntrenamiento gestorEntrenamiento, ControladorIA controladorIA = null)
    {
        this.miJugador = miJugador;
        this.gestorEntrenamiento = gestorEntrenamiento;
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
    public void ActualizarResultados()
    {
        while (gestorEntrenamiento.ResultadosPendientes.TryDequeue(out ResultadoEntrenamiento resultado))
        {
            if (resultado.Resultado is Aldeano aldeano)
            {
               miJugador.AgregarAldeano(aldeano);
            }
            else if (resultado.Resultado is Unidad unidad)
            {
                miJugador.Unidades.Add(unidad);
                controladorIA?.ObservarUnidad(unidad);
            }
        }

        controladorIA?.EvaluarEconomia();
    }
}