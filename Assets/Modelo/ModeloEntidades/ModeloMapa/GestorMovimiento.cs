using System.Collections.Concurrent;
using System.Threading.Tasks;


public class ResultadoMovimiento 
{
    public bool Exitoso { get; set; }
    public int Fila { get; set; }
    public int Columna { get; set; }
    public Unidad Unidad { get; set; }
}


public class GestorMovimiento 
{
    public ConcurrentQueue<ResultadoMovimiento> ResultadosPendientes { get; private set; }
    private readonly GestorArchivos gestorArchivos = new GestorArchivos();

    public GestorMovimiento() 
    {
        ResultadosPendientes = new ConcurrentQueue<ResultadoMovimiento>();
    }

    public void IniciarMovimiento(Mapa mapa, int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino, string nombreJugador) 
    {
        Task.Run(() => {
            MoverEnSegundoPlano(mapa, filaOrigen, columnaOrigen, filaDestino, columnaDestino, nombreJugador);
        });
    }

    private void MoverEnSegundoPlano(Mapa mapa, int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino, string nombreJugador) 
    {
        Task.Delay(1000).Wait();

        Celda celdaOrigen = mapa.ObtenerCelda(filaOrigen, columnaOrigen);
        Unidad unidadAMover = celdaOrigen?.Unidad;

        bool movido = mapa.MoverUnidad(filaOrigen, columnaOrigen, filaDestino, columnaDestino);

        gestorArchivos.RegistrarEvento(
            nombreJugador,
            "Movimiento",
            movido ? $"Unidad movida a ({filaDestino},{columnaDestino})" : "Movimiento fallido"
        );

        ResultadosPendientes.Enqueue(new ResultadoMovimiento {
            Exitoso = movido,
            Fila = filaDestino,
            Columna = columnaDestino,
            Unidad = movido ? unidadAMover : null
        });
    }
}