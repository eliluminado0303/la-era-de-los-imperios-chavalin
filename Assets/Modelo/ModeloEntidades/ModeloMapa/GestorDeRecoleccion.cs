using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class GestorRecoleccion {
    public ConcurrentQueue<ResultadoRecoleccion> ResultadosPendientes { get; private set; }

    private readonly object candadoRecursos = new object();
    private readonly GestorArchivos gestorArchivos = new GestorArchivos();

    // Un aldeano solo puede tener un ciclo de recolección corriendo a la
    // vez. Si se le da una nueva orden mientras el anterior sigue en
    // curso, se cancela el anterior (no llega a entregar ese ciclo) y
    // arranca el nuevo enseguida — así una orden nueva SIEMPRE interrumpe
    // a la anterior en vez de tener que esperar a que termine sola.
    private readonly ConcurrentDictionary<Aldeano, CancellationTokenSource> tareasEnCurso = new ConcurrentDictionary<Aldeano, CancellationTokenSource>();

    public GestorRecoleccion() {
        ResultadosPendientes = new ConcurrentQueue<ResultadoRecoleccion>();
    }

    public bool IniciarRecoleccion(Aldeano aldeano, Recurso recurso, Jugador jugador) {
        // Límite real: si el recurso ya está agotado, no tiene sentido
        // arrancar un ciclo de 2 segundos para terminar recolectando 0.
        if (recurso.EstaAgotado()) {
            return false;
        }

        if (tareasEnCurso.TryRemove(aldeano, out var anterior)) {
            anterior.Cancel();
        }

        var cts = new CancellationTokenSource();
        tareasEnCurso[aldeano] = cts;
        aldeano.Ocupado = true;

        Task.Run(() => {
            RecolectarEnSegundoPlano(aldeano, recurso, jugador, cts.Token);
        });
        return true;
    }

       // Antes esto hacía UN solo viaje (2s, sacaba un puñado, y quedaba
       // "Libre" de nuevo) — ahora el aldeano se queda trabajando la MISMA
       // veta en bucle, cada 2s, hasta que se agota o le mandan una orden
       // nueva (eso sigue cancelando este bucle vía el token, igual que
       // antes). Ocupado se mantiene en true durante TODO el bucle, no
       // solo durante un viaje — así el HUD muestra "Recolectando..." de
       // forma consistente mientras dure.
       private void RecolectarEnSegundoPlano(Aldeano aldeano, Recurso recurso, Jugador jugador, CancellationToken token) {
        while (!token.IsCancellationRequested && !recurso.EstaAgotado()) {
            try {
                Task.Delay(2000, token).Wait();
            } catch (AggregateException) {
                return; // se canceló: le dieron una orden nueva antes de que terminara este ciclo
            }

            if (token.IsCancellationRequested) return;

            int cantidadObtenida;
            lock (candadoRecursos) {
                cantidadObtenida = recurso.Recolectar(aldeano.VelocidadRecoleccion);
            }

            jugador.AgregarRecurso(recurso.Tipo, cantidadObtenida);

            gestorArchivos.RegistrarEvento(
                jugador.Nombre,
                "Recoleccion",
                $"{aldeano.Nombre} recolecto {cantidadObtenida} de {recurso.Tipo}");

            ResultadosPendientes.Enqueue(new ResultadoRecoleccion {
                NombreAldeano = aldeano.Nombre,
                TipoRecurso = recurso.Tipo,
                Cantidad = cantidadObtenida
            });
        }

        aldeano.Ocupado = false;
        tareasEnCurso.TryRemove(aldeano, out _);
    }
    // agregar dentro de GestorRecoleccion

public bool IniciarRecoleccionDesdeMapa(Aldeano aldeano, Mapa mapa, int fila, int columna, Jugador jugador) {
    Celda celda = mapa.ObtenerCelda(fila, columna);

    if (celda == null || celda.Recurso == null) {
        return false;
    }

    // Antes esto no se chequeaba acá: se podía mandar a un aldeano a una
    // celda ya vacía y arrancaba igual un ciclo completo de 2 segundos
    // para conseguir 0 de recurso.
    if (celda.Recurso.EstaAgotado()) {
        return false;
    }

    return IniciarRecoleccion(aldeano, celda.Recurso, jugador);
}
}

public class ResultadoRecoleccion {
    public string NombreAldeano { get; set; }
    public TipoRecurso TipoRecurso { get; set; }
    public int Cantidad { get; set; }
}