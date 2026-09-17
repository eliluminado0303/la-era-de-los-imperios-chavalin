using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Ejecuta la recolección de los aldeanos en segundo plano y entrega sus
/// resultados para que el hilo principal los aplique al juego.
/// </summary>
public class GestorAldeanos
{
    private List<Aldeano> aldeanos = new List<Aldeano>();
    private readonly object sync = new object();
    private ConcurrentQueue<(string tipoRecurso, float cantidad)> resultados = new();
    private CancellationTokenSource cts;
    private Task tareaRecoleccion;

    /// <summary>Agrega un aldeano al conjunto que participa en la recolección.</summary>
    public void AgregarAldeano(Aldeano aldeano)
    {
        if (aldeano == null) throw new ArgumentNullException(nameof(aldeano));
        lock (sync) aldeanos.Add(aldeano);
    }

    /// <summary>Inicia el ciclo de recolección si todavía no está activo.</summary>
    public void IniciarRecoleccion(int intervaloMs = 1000)
    {
        if (intervaloMs <= 0) throw new ArgumentOutOfRangeException(nameof(intervaloMs));
        if (tareaRecoleccion != null && !tareaRecoleccion.IsCompleted) return;

        cts = new CancellationTokenSource();
        tareaRecoleccion = Task.Run(() => BucleRecoleccion(intervaloMs, cts.Token));
    }

    /// <summary>Solicita la detención del ciclo de recolección.</summary>
    public void DetenerRecoleccion()
    {
        cts?.Cancel();
        tareaRecoleccion = null;
    }

    private async Task BucleRecoleccion(int intervaloMs, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Aldeano[] aldeanosActuales;
            lock (sync) aldeanosActuales = aldeanos.ToArray();

            foreach (var aldeano in aldeanosActuales)
            {
                float cantidad = aldeano.RecolectarTick();
                if (cantidad > 0)
                {
                    resultados.Enqueue((aldeano.TipoRecurso, cantidad));
                }
            }
            try
            {
                await Task.Delay(intervaloMs, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Vacía la cola de resultados y ejecuta la acción en el hilo que invoca
    /// este método, normalmente el hilo principal desde <c>Update</c>.
    /// </summary>
    public void ProcesarResultados(Action<string, float> aplicarRecurso)
    {
        if (aplicarRecurso == null) throw new ArgumentNullException(nameof(aplicarRecurso));

        while (resultados.TryDequeue(out var item))
        {
            aplicarRecurso(item.tipoRecurso, item.cantidad);
        }
    }
}