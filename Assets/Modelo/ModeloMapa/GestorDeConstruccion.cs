using System.Collections.Concurrent;
using System.Threading.Tasks;

public class GestorConstruccion {

    public ConcurrentQueue<ResultadoConstruccion> ResultadosPendientes { get; private set; }
    private readonly object candadoMapa = new object();

    public GestorConstruccion() {
        ResultadosPendientes = new ConcurrentQueue<ResultadoConstruccion>();
    }

    public bool IniciarConstruccion(Jugador jugador, Mapa mapa, int fila, int columna, Edificio edificio) {
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
        // Simula el tiempo real que toma construir (5 segundos, ajustable)
        Task.Delay(5000).Wait();

        bool colocado;
        lock (candadoMapa) {
            colocado = mapa.ColocarEdificio(fila, columna, edificio);
        }

        if (colocado) {
            jugador.AgregarEdificio(edificio);
        }

        ResultadosPendientes.Enqueue(new ResultadoConstruccion {
            NombreEdificio = edificio.Nombre,
            Fila = fila,
            Columna = columna,
            Exitoso = colocado
        });
    }
}

public class ResultadoConstruccion {
    public string NombreEdificio { get; set; }
    public int Fila { get; set; }
    public int Columna { get; set; }
    public bool Exitoso { get; set; }
}