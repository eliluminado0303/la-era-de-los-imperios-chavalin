using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class GestorRecoleccion {
    public ConcurrentQueue<ResultadoRecoleccion> ResultadosPendientes { get; private set; }

    private readonly object candadoRecursos = new object();
    private readonly GestorArchivos gestorArchivos = new GestorArchivos();
    public GestorRecoleccion() {
        ResultadosPendientes = new ConcurrentQueue<ResultadoRecoleccion>();
    }

    public void IniciarRecoleccion(Aldeano aldeano, Recurso recurso, Jugador jugador) {
        if (aldeano.Ocupado || aldeano.EstaEscondido) {
            return;
        }

        aldeano.Ocupado = true;

        Task.Run(() => {
            RecolectarEnSegundoPlano(aldeano, recurso, jugador);
        });
    }

       private void RecolectarEnSegundoPlano(Aldeano aldeano, Recurso recurso, Jugador jugador) {
        int cantidadObtenida;

       
        Task.Delay(2000).Wait();

        lock (candadoRecursos) {
            cantidadObtenida = recurso.Recolectar(aldeano.VelocidadRecoleccion);
        }

        jugador.AgregarRecurso(recurso.Tipo, cantidadObtenida);

        aldeano.Ocupado = false;
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
    // agregar dentro de GestorRecoleccion

public bool IniciarRecoleccionDesdeMapa(Aldeano aldeano, Mapa mapa, int fila, int columna, Jugador jugador) {
    Celda celda = mapa.ObtenerCelda(fila, columna);

    if (celda == null || celda.Recurso == null) {
        return false;
    }

    IniciarRecoleccion(aldeano, celda.Recurso, jugador);
    return true;
}
}

public class ResultadoRecoleccion {
    public string NombreAldeano { get; set; }
    public TipoRecurso TipoRecurso { get; set; }
    public int Cantidad { get; set; }
}