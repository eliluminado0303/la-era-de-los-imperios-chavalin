using System.Collections.Generic;

public class ControladorMapa {

    public Jugador Jugador { get; private set; }
    public Mapa Mapa { get; private set; }
    public Partida Partida { get; private set; }

    private readonly GestorRecoleccion gestorRecoleccion;
    private readonly GestorConstruccion gestorConstruccion;
    private readonly GestorMovimiento gestorMovimiento;
    private readonly GestorArchivos gestorArchivos;
    private readonly ControladorIA controladorIA; // null si este jugador es el humano

    public ControladorMapa(string nombreJugador, List<Jugador> oponentes, Mapa mapaCompartido, ControladorIA controladorIA = null) {
        Jugador = new Jugador(nombreJugador);
        Mapa = mapaCompartido; 
        Partida = new Partida(Jugador, oponentes, Mapa);
        this.controladorIA = controladorIA;

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

    public void ActualizarResultados() {
        while (gestorRecoleccion.ResultadosPendientes.TryDequeue(out ResultadoRecoleccion _)) {
            
        }

        while (gestorConstruccion.ResultadosPendientes.TryDequeue(out ResultadoConstruccion resultado)) {
            if (resultado.Exitoso) {
                controladorIA?.ObservarEdificio(resultado.Edificio);
            }
        }

        while (gestorMovimiento.ResultadosPendientes.TryDequeue(out ResultadoMovimiento resultado)) {
            if (resultado.Exitoso && resultado.Unidad != null) {
                controladorIA?.AlTerminarMovimiento(resultado.Unidad, resultado.Fila, resultado.Columna);
            }
        }
    }
}