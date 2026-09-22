using System.Collections.Concurrent;
using System.Threading.Tasks;

public class GestorConstruccion {

    public ConcurrentQueue<ResultadoConstruccion> ResultadosPendientes { get; private set; }
    private readonly GestorArchivos gestorArchivos = new GestorArchivos();

    public GestorConstruccion() {
        ResultadosPendientes = new ConcurrentQueue<ResultadoConstruccion>();
    }

    public bool IniciarConstruccion(Jugador jugador, Mapa mapa, int fila, int columna, Edificio edificio) {

        
        bool esElPrincipal = edificio is EdificioPrincipal;
        if (!esElPrincipal && !jugador.TieneEdificioPrincipal()) {
            return false;
        }

        if (!jugador.GastarRecurso(TipoRecurso.Oro, edificio.CostoOro)) return false;
        if (!jugador.GastarRecurso(TipoRecurso.Madera, edificio.CostoMadera)) return false;
        if (!jugador.GastarRecurso(TipoRecurso.Comida, edificio.CostoComida)) return false;

        Task.Run(() => {
            ConstruirEnSegundoPlano(mapa, fila, columna, edificio, jugador);
        });

        return true;
    }

    private void ConstruirEnSegundoPlano(Mapa mapa, int fila, int columna, Edificio edificio, Jugador jugador) {
        Task.Delay(5000).Wait();

        bool colocado = mapa.ColocarEdificio(fila, columna, edificio);

        if (colocado) {
            edificio.AvanzarConstruccion(100);
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
            Exitoso = colocado,
            Edificio = edificio // necesario para que ControladorMapa pueda avisarle a la IA
        });
    }
}
public class ResultadoConstruccion {
    public string NombreEdificio { get; set; }
    public int Fila { get; set; }
    public int Columna { get; set; }
    public bool Exitoso { get; set; }
    public Edificio Edificio { get; set; }
}