using System.Collections.Generic;

public class ControladorMapa {

    public Jugador Jugador { get; private set; }
    public Mapa Mapa { get; private set; }
    public Partida Partida { get; private set; }

    private readonly GestorRecoleccion gestorRecoleccion;
    private readonly GestorConstruccion gestorConstruccion;
    private readonly GestorMovimiento gestorMovimiento;
    private readonly GestorArchivos gestorArchivos;

    public ControladorMapa(string nombreJugador, List<Jugador> oponentes) {
        Jugador = new Jugador(nombreJugador);
        Mapa = new Mapa();
        Partida = new Partida(Jugador, oponentes, Mapa);

        gestorRecoleccion = new GestorRecoleccion();
        gestorConstruccion = new GestorConstruccion();
        gestorMovimiento = new GestorMovimiento();
        gestorArchivos = new GestorArchivos();
    }

    public void IniciarPartida() {
        gestorArchivos.GuardarConfiguracion(Mapa, Jugador);
    }

    public bool SolicitarRecoleccion(Aldeano aldeano, int fila, int columna) {
        return gestorRecoleccion.IniciarRecoleccionDesdeMapa(aldeano, Mapa, fila, columna, Jugador);
    }

    public bool SolicitarConstruccion(int fila, int columna, Edificio edificio) {
        return gestorConstruccion.IniciarConstruccion(Jugador, Mapa, fila, columna, edificio);
    }

    public void SolicitarMovimiento(int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino) {
        gestorMovimiento.IniciarMovimiento(Mapa, filaOrigen, columnaOrigen, filaDestino, columnaDestino, Jugador.Nombre);
    }

    public bool VerificarFinDePartida() {
        bool termino = Partida.VerificarGanador();
        if (termino) {
            gestorArchivos.GuardarResultadoFinal(Partida);
        }
        return termino;
    }
}