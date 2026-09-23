using System.Collections.Generic;

// Punto de arranque del juego: crea el Mapa compartido, la Partida
// compartida, los 4 jugadores (1 humano + 3 IA, una por civilización),
// coloca Centros Urbanos y recursos iniciales, y arma los controladores
// de cada jugador (ControladorMapa, ControladorCombate,
// ControladorEntrenamiento, y ControladorIA para las 3 IA).
//
// La Vista solo necesita: crear UNA instancia de esta clase al empezar la
// partida, y llamar a Actualizar() una vez por frame desde su Update().
public class ControladorPartida
{
    private static readonly string[] CIVILIZACIONES = { "Nipones", "Griegos", "Vikingos", "Sumerios" };

    public Mapa Mapa { get; }
    public Partida Partida { get; }

    // Índice 0 = jugador humano; 1..3 = las 3 IA, en el mismo orden que
    // quedaron colocadas en las esquinas del mapa.
    public List<ControladorMapa> ControladoresMapa { get; } = new List<ControladorMapa>();
    public List<ControladorCombate> ControladoresCombate { get; } = new List<ControladorCombate>();
    public List<ControladorEntrenamiento> ControladoresEntrenamiento { get; } = new List<ControladorEntrenamiento>();

    public ControladorPartida(string nombreJugadorHumano, string civilizacionHumano)
    {
        Mapa = new Mapa();

        // El humano ocupa una de las 4 civilizaciones; las otras 3 quedan
        // para las IA, en el mismo orden de CIVILIZACIONES.
        var civilizacionesIA = new List<string>();
        foreach (var civilizacion in CIVILIZACIONES)
            if (civilizacion != civilizacionHumano) civilizacionesIA.Add(civilizacion);

        var jugadorHumano = new Jugador(nombreJugadorHumano);
        var jugadoresIA = new List<Jugador>();
        foreach (var civilizacion in civilizacionesIA)
            jugadoresIA.Add(new Jugador($"IA {civilizacion}"));

        Partida = new Partida(jugadorHumano, jugadoresIA, Mapa);

        // Centros Urbanos: el humano siempre en la primera esquina, luego
        // las 3 IA en el mismo orden que civilizacionesIA/jugadoresIA.
        var colocacion = new List<(Jugador jugador, string civilizacion)> { (jugadorHumano, civilizacionHumano) };
        for (int i = 0; i < jugadoresIA.Count; i++)
            colocacion.Add((jugadoresIA[i], civilizacionesIA[i]));
        Mapa.ColocarCentrosUrbanosIniciales(colocacion);
        Mapa.GenerarRecursosIniciales();

        // Controlador del jugador humano (sin IA propia: sus decisiones
        // vienen de la Vista, no de ControladorIA).
        var gestorEntrenamientoHumano = new GestorEntrenamiento();
        ControladoresMapa.Add(new ControladorMapa(jugadorHumano, Mapa, Partida));
        ControladoresCombate.Add(new ControladorCombate(jugadorHumano, Mapa, Partida));
        ControladoresEntrenamiento.Add(new ControladorEntrenamiento(jugadorHumano, gestorEntrenamientoHumano));

        // Una IA por cada civilización restante.
        for (int i = 0; i < jugadoresIA.Count; i++)
        {
            var jugadorIA = jugadoresIA[i];
            var civilizacionIA = civilizacionesIA[i];
            var gestorEntrenamientoIA = new GestorEntrenamiento();

            var controladorIA = new ControladorIA(jugadorIA, civilizacionIA, Mapa, Partida, gestorEntrenamientoIA);

            ControladoresMapa.Add(new ControladorMapa(jugadorIA, Mapa, Partida, controladorIA));
            ControladoresCombate.Add(new ControladorCombate(jugadorIA, Mapa, Partida));
            ControladoresEntrenamiento.Add(new ControladorEntrenamiento(jugadorIA, gestorEntrenamientoIA, controladorIA));
        }
    }

    // Llamar UNA VEZ POR FRAME desde el Update() de Unity. Vacía las colas
    // de los 4 jugadores (recolección/construcción/movimiento de cada
    // ControladorMapa, entrenamiento de cada ControladorEntrenamiento) y
    // revisa si la partida ya terminó. Devuelve true cuando termina.
    public bool Actualizar()
    {
        foreach (var controladorMapa in ControladoresMapa) controladorMapa.ActualizarResultados();
        foreach (var controladorEntrenamiento in ControladoresEntrenamiento) controladorEntrenamiento.ActualizarResultados();

        return ControladoresMapa[0].VerificarFinDePartida(); // cualquiera de los 4 sirve: comparten la misma Partida
    }
}