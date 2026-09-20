using System.Collections.Concurrent;
using System.Threading.Tasks;

public class GestorEntrenamiento
{
    public ConcurrentQueue<object> ResultadosPendientes { get; } = new();

    public bool EntrenarUnidad(Jugador jugador, string tipoUnidad, string civilizacion)
    {
        Unidad unidad = FabricaUnidades.Crear(tipoUnidad, civilizacion);
        if (!CobrarCosto(jugador, unidad.CostoOro, unidad.CostoMadera, unidad.CostoComida)) return false;

        Task.Run(() => {
            Task.Delay(3000).Wait();
            ResultadosPendientes.Enqueue(unidad);
        });
        return true;
    }

    public bool EntrenarAldeano(Jugador jugador, string civilizacion)
    {
        var aldeano = new Aldeano("Aldeano", civilizacion, velocidad: 4, velocidadRecoleccion: 5);
        if (!CobrarCosto(jugador, aldeano.CostoOro, aldeano.CostoMadera, aldeano.CostoComida)) return false;

        Task.Run(() => {
            Task.Delay(2000).Wait(); // más rápido que una tropa, es economía
            ResultadosPendientes.Enqueue(aldeano);
        });
        return true;
    }

    private bool CobrarCosto(Jugador jugador, int oro, int madera, int comida)
    {
        if (!jugador.GastarRecurso(TipoRecurso.Oro, oro)) return false;
        if (!jugador.GastarRecurso(TipoRecurso.Madera, madera)) return false;
        if (!jugador.GastarRecurso(TipoRecurso.Comida, comida)) return false;
        return true;
    }
}