using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Threading.Tasks;

// Coordina la producción de unidades y aldeanos sin bloquear el hilo principal.
public class GestorEntrenamiento
{
    // La cola permite entregar resultados desde la tarea de entrenamiento al juego.
    public ConcurrentQueue<ResultadoEntrenamiento> ResultadosPendientes { get; } =
        new ConcurrentQueue<ResultadoEntrenamiento>();
    // Permite cancelar los entrenamientos pendientes cuando se cierre la partida.
    private readonly CancellationTokenSource cancelacion = new CancellationTokenSource();

    // Cobra el coste y comienza el entrenamiento de una unidad militar.
    public bool EntrenarUnidad(Jugador jugador, string tipoUnidad, string civilizacion)
    {
        if (jugador == null) throw new ArgumentNullException(nameof(jugador));
        Unidad unidad = FabricaUnidades.Crear(tipoUnidad, civilizacion);

        // Solo puede haber un héroe vivo en batalla por jugador a la vez.
        if (unidad is Heroe && jugador.Unidades.Exists(u => u is Heroe && u.Vida > 0))
            return false;

        if (!CobrarCosto(jugador, unidad.CostoOro, unidad.CostoMadera, unidad.CostoComida)) return false;

        _ = EntrenarAsync(unidad, 3000, cancelacion.Token);
        return true;
    }

    // Cobra el coste y comienza el entrenamiento de un aldeano.
    public bool EntrenarAldeano(Jugador jugador, string civilizacion)
    {
        if (jugador == null) throw new ArgumentNullException(nameof(jugador));
        var aldeano = new Aldeano("Aldeano", civilizacion, velocidad: 4, velocidadRecoleccion: 5);
        if (!CobrarCosto(jugador, aldeano.CostoOro, aldeano.CostoMadera, aldeano.CostoComida)) return false;

        _ = EntrenarAsync(aldeano, 2000, cancelacion.Token);
        return true;
    }

    // Cancela los entrenamientos que todavía no han terminado.
    public void Detener()
    {
        cancelacion.Cancel();
    }

    // El jugador valida y descuenta los tres recursos como una sola operación.
   private bool CobrarCosto(Jugador jugador, int oro, int madera, int comida)
    {
    if (!jugador.GastarRecurso(TipoRecurso.Oro, oro)) return false;
    if (!jugador.GastarRecurso(TipoRecurso.Madera, madera)) return false;
    if (!jugador.GastarRecurso(TipoRecurso.Comida, comida)) return false;
    return true;
    }

    // Espera sin bloquear un hilo y publica el resultado cuando termina.
    private async Task EntrenarAsync(object resultado, int duracionMs, CancellationToken token)
    {
        try
        {
            await Task.Delay(duracionMs, token);
            ResultadosPendientes.Enqueue(new ResultadoEntrenamiento(resultado));
        }
        catch (TaskCanceledException)
        {
        }
    }
}

// Envuelve el resultado terminado para mantener una cola con un tipo común.
public sealed class ResultadoEntrenamiento
{
    public object Resultado { get; }

    public ResultadoEntrenamiento(object resultado)
    {
        Resultado = resultado ?? throw new ArgumentNullException(nameof(resultado));
    }
}