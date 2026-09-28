using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public static class TiempoDeViaje
{
    public static float Calcular(int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino, float velocidad)
    {
        if (filaOrigen < 0 || columnaOrigen < 0) return 0f;
        int distancia = Math.Max(Math.Abs(filaDestino - filaOrigen), Math.Abs(columnaDestino - columnaOrigen));
        if (distancia == 0) return 0f;
        if (velocidad <= 0) velocidad = 1f;
        return distancia / velocidad;
    }
}

public class GestorRecoleccion {
    public ConcurrentQueue<ResultadoRecoleccion> ResultadosPendientes { get; private set; }
    private readonly object candadoRecursos = new object();
    private readonly GestorArchivos gestorArchivos = new GestorArchivos();
    private readonly ConcurrentDictionary<Aldeano, CancellationTokenSource> tareasEnCurso = new ConcurrentDictionary<Aldeano, CancellationTokenSource>();

    public GestorRecoleccion() { ResultadosPendientes = new ConcurrentQueue<ResultadoRecoleccion>(); }

    public bool IniciarRecoleccion(Aldeano aldeano, Recurso recurso, Jugador jugador) {
        if (recurso.EstaAgotado()) return false;
        if (!aldeano.EstaVivo) return false; // del repo: seguridad de muerte

        if (tareasEnCurso.TryRemove(aldeano, out var anterior)) anterior.Cancel();

        var cts = new CancellationTokenSource();
        tareasEnCurso[aldeano] = cts;
        aldeano.Ocupado = true;
        Task.Run(() => RecolectarEnSegundoPlano(aldeano, recurso, jugador, cts.Token));
        return true;
    }

    private void RecolectarEnSegundoPlano(Aldeano aldeano, Recurso recurso, Jugador jugador, CancellationToken token) {
        while (!token.IsCancellationRequested && !recurso.EstaAgotado() && aldeano.EstaVivo) {
            try { Task.Delay(2000, token).Wait(); }
            catch (AggregateException) { return; }

            if (token.IsCancellationRequested) return;
            if (!aldeano.EstaVivo) { tareasEnCurso.TryRemove(aldeano, out _); return; }

            int cantidadObtenida;
            lock (candadoRecursos) { cantidadObtenida = recurso.Recolectar(aldeano.VelocidadRecoleccion); }

            jugador.AgregarRecurso(recurso.Tipo, cantidadObtenida);
            gestorArchivos.RegistrarEvento(jugador.Nombre, "Recoleccion", $"{aldeano.Nombre} recolecto {cantidadObtenida} de {recurso.Tipo}");
            ResultadosPendientes.Enqueue(new ResultadoRecoleccion { NombreAldeano = aldeano.Nombre, TipoRecurso = recurso.Tipo, Cantidad = cantidadObtenida });
        }

        if (!token.IsCancellationRequested) aldeano.RecolectandoTipo = null;
        aldeano.Ocupado = false;
        tareasEnCurso.TryRemove(aldeano, out _);
    }

    public bool IniciarRecoleccionDesdeMapa(Aldeano aldeano, Mapa mapa, int fila, int columna, Jugador jugador) {
        if (!aldeano.EstaVivo) return false; // del repo: mismo criterio, aplicado también acá
        Celda celda = mapa.ObtenerCelda(fila, columna);
        if (celda == null || celda.Recurso == null || celda.Recurso.EstaAgotado()) return false;

        if (tareasEnCurso.TryRemove(aldeano, out var anterior)) anterior.Cancel();

        var cts = new CancellationTokenSource();
        tareasEnCurso[aldeano] = cts;
        aldeano.Ocupado = true;
        aldeano.RecolectandoTipo = null;

        int filaOrigen = aldeano.Fila, columnaOrigen = aldeano.Columna;
        Task.Run(() => CaminarYRecolectar(aldeano, mapa, filaOrigen, columnaOrigen, fila, columna, jugador, cts.Token));
        return true;
    }

    private void CaminarYRecolectar(Aldeano aldeano, Mapa mapa, int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino, Jugador jugador, CancellationToken token) {
        float duracionCaminata = TiempoDeViaje.Calcular(filaOrigen, columnaOrigen, filaDestino, columnaDestino, aldeano.Velocidad);

        if (duracionCaminata > 0f) {
            try { Task.Delay((int)(duracionCaminata * 1000), token).Wait(); }
            catch (AggregateException) { aldeano.Ocupado = false; tareasEnCurso.TryRemove(aldeano, out _); return; }

            if (token.IsCancellationRequested || !aldeano.EstaVivo) { // EstaVivo agregado: pudo morir mientras caminaba
                aldeano.Ocupado = false; tareasEnCurso.TryRemove(aldeano, out _); return;
            }
        }

        mapa.ColocarAldeano(filaDestino, columnaDestino, aldeano);
        Celda celdaDestino = mapa.ObtenerCelda(filaDestino, columnaDestino);
        if (celdaDestino?.Recurso == null || celdaDestino.Recurso.EstaAgotado()) {
            aldeano.RecolectandoTipo = null; aldeano.Ocupado = false; tareasEnCurso.TryRemove(aldeano, out _); return;
        }

        aldeano.RecolectandoTipo = celdaDestino.Recurso.Tipo;
        RecolectarEnSegundoPlano(aldeano, celdaDestino.Recurso, jugador, token);
    }
}

public class ResultadoRecoleccion {
    public string NombreAldeano { get; set; }
    public TipoRecurso TipoRecurso { get; set; }
    public int Cantidad { get; set; }
}