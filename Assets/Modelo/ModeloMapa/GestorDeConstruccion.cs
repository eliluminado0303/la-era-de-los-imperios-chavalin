using System.Collections.Concurrent;
using System.Threading.Tasks;

public class GestorConstruccion {

    public ConcurrentQueue<ResultadoConstruccion> ResultadosPendientes { get; private set; }
    private readonly object candadoMapa = new object();
    private readonly GestorArchivos gestorArchivos = new GestorArchivos();

    public GestorConstruccion() {
        ResultadosPendientes = new ConcurrentQueue<ResultadoConstruccion>();
    }

    public bool IniciarConstruccion(Jugador jugador, Mapa mapa, int fila, int columna, Edificio edificio) {

        // Nuevo: no se puede construir nada (excepto el Edificio Principal en sí)
        // si el jugador todavía no tiene su Edificio Principal en pie.
        bool esElPrincipal = edificio is Edificio.EdificioPrincipal;
        if (!esElPrincipal && !jugador.TieneEdificioPrincipal()) {
            return false;
        }

        if (!jugador.GastarRecurso(TipoRecurso.Oro, edificio.CostoOro)) {
            return false;
        }
        if (!jugador.GastarRecurso(TipoRecurso.Madera, edificio.CostoMadera)) {
            return false;
        }
        if (!jugador.GastarRecurso(TipoRecurso.Comida, edificio.CostoComida)) {
            return false;
        }

        Task.Run(() => {
            ConstruirEnSegundoPlano(mapa, fila, columna, edificio, jugador);
        });

        return true;
    }

    private void ConstruirEnSegundoPlano(Mapa mapa, int fila, int columna, Edificio edificio, Jugador jugador) {
        Task.Delay(5000).Wait();

        bool colocado;
        lock (candadoMapa) {
            colocado = mapa.ColocarEdificio(fila, columna, edificio);
        }

        if (colocado) {
            edificio.AvanzarConstruccion(100); // marca el edificio como terminado
            jugador.AgregarEdificio(edificio);
        }

        gestorArchivos.RegistrarEvento(
            jugador.Nombre,
            "Construccion",
            colocado ? $"{edificio.Nombre} construido en ({fila},{columna})" : "Construccion fallida: celda ocupada"
        );

        ResultadosPendientes.Enqueue(new ResultadoConstruccion {
            NombreEdificio = edificio.Nombre,
            Fila = fila,
            Columna = columna,
            Exitoso = colocado
        });
    }
}