using System.Collections.Generic;
public class Mapa
{
    public const int FILAS = 15;
    public const int COLUMNAS = 15;

    private Celda[,] celdas;

    public Mapa()
    {
        celdas = new Celda[FILAS, COLUMNAS];
        InicializarMapa();
    }

    private void InicializarMapa()
    {
        for (int fila = 0; fila < FILAS; fila++)
        {
            for (int columna = 0; columna < COLUMNAS; columna++)
            {
                celdas[fila, columna] = new Celda
                {
                    Fila = fila,
                    Columna = columna,
                    Terreno = TipoTerreno.Tierra,
                    Recurso = null,
                    Edificio = null,
                    Unidad = null
                };
            }
        }
    }

    public Celda ObtenerCelda(int fila, int columna)
    {
        if (!EsPosicionValida(fila, columna))
        {
            return null;
        }

        return celdas[fila, columna];
    }
    // Indica si una celda esta vacia (sin recurso, edificio ni unidad)
    public bool CeldaLibre(int fila, int columna) {
        if (!EsPosicionValida(fila, columna)) return false;
        Celda celda = celdas[fila, columna];
        return celda.Recurso == null && celda.Edificio == null && celda.Unidad == null;
    }

    public bool EsPosicionValida(int fila, int columna)
    {
        return fila >= 0 &&
               fila < FILAS &&
               columna >= 0 &&
               columna < COLUMNAS;
    }

    public bool ColocarRecurso(int fila, int columna, Recurso recurso)
    {
        if (!EsPosicionValida(fila, columna))
        {
            return false;
        }

        Celda celda = celdas[fila, columna];

        if (celda.Recurso != null || celda.Edificio != null)
        {
            return false;
        }

        celda.Recurso = recurso;

        return true;
    }

    public bool RetirarRecurso(int fila, int columna)
    {
        if (!EsPosicionValida(fila, columna))
        {
            return false;
        }

        Celda celda = celdas[fila, columna];

        if (celda.Recurso == null)
        {
            return false;
        }

        celda.Recurso = null;

        return true;
    }

    public bool ColocarEdificio(int fila, int columna, Edificio edificio)
    {
        if (!EsPosicionValida(fila, columna))
        {
            return false;
        }

        Celda celda = celdas[fila, columna];

        if (celda.Recurso != null || celda.Edificio != null)
        {
            return false;
        }

        celda.Edificio = edificio;

        return true;
    }

    public bool RetirarEdificio(int fila, int columna)
    {
        if (!EsPosicionValida(fila, columna))
        {
            return false;
        }

        Celda celda = celdas[fila, columna];

        if (celda.Edificio == null)
        {
            return false;
        }

        celda.Edificio = null;

        return true;
    }
    public bool MoverUnidad(int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino) {
    if (!EsPosicionValida(filaOrigen, columnaOrigen) || !EsPosicionValida(filaDestino, columnaDestino)) {
        return false;
    }

    Celda origen = celdas[filaOrigen, columnaOrigen];
    Celda destino = celdas[filaDestino, columnaDestino];

    if (origen.Unidad == null) {
        return false;
    }

    if (destino.Unidad != null || destino.Edificio != null) {
        return false;
    }

    destino.Unidad = origen.Unidad;
    origen.Unidad = null;

    return true;
}
// --- Métodos de consulta para la IA (no modifican el mapa, solo informan) ---

// Devuelve las celdas donde hay recursos aun disponibles para recolectar
public List<(int fila, int columna)> BuscarRecursosDisponibles() {
    List<(int, int)> encontrados = new List<(int, int)>();
    for (int fila = 0; fila < FILAS; fila++) {
        for (int columna = 0; columna < COLUMNAS; columna++) {
            Celda celda = celdas[fila, columna];
            if (celda.Recurso != null && !celda.Recurso.EstaAgotado()) {
                encontrados.Add((fila, columna));
            }
        }
    }
    return encontrados;
}

// Devuelve las celdas vacias donde se podria construir algo
public List<(int fila, int columna)> BuscarCeldasLibres() {
    List<(int, int)> libres = new List<(int, int)>();
    for (int fila = 0; fila < FILAS; fila++) {
        for (int columna = 0; columna < COLUMNAS; columna++) {
            if (CeldaLibre(fila, columna)) {
                libres.Add((fila, columna));
            }
        }
    }
    return libres;
}

// Devuelve la posicion de todas las unidades presentes en el mapa
public List<(int fila, int columna)> BuscarUnidades() {
    List<(int, int)> unidades = new List<(int, int)>();
    for (int fila = 0; fila < FILAS; fila++) {
        for (int columna = 0; columna < COLUMNAS; columna++) {
            if (celdas[fila, columna].Unidad != null) {
                unidades.Add((fila, columna));
            }
        }
    }
    return unidades;
}
// dentro de Mapa.cs, o podría ir en ControladorMapa
public void GenerarRecursosIniciales() {
    // Zona del jugador humano (esquina superior izquierda)
    ColocarRecurso(2, 2, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 100 });
    ColocarRecurso(3, 1, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 150 });
    ColocarRecurso(1, 4, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 80 });

    // Zona del oponente (esquina inferior derecha)
    ColocarRecurso(12, 12, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 100 });
    ColocarRecurso(11, 13, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 150 });
    ColocarRecurso(13, 10, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 80 });

    // Zona neutral central (opcional, para disputar)
    ColocarRecurso(7, 7, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 200 });
}
public void ColocarCentrosUrbanosIniciales(Jugador jugadorHumano, Jugador oponente) {
    Edificio.EdificioPrincipal centroJugador = new Edificio.EdificioPrincipal(jugadorHumano.Nombre, costoOro: 0, costoMadera: 0, costoComida: 0);
    ColocarEdificio(0, 0, centroJugador);
    jugadorHumano.AgregarEdificio(centroJugador);

    Edificio.EdificioPrincipal centroOponente = new Edificio.EdificioPrincipal(oponente.Nombre, costoOro: 0, costoMadera: 0, costoComida: 0);
    ColocarEdificio(14, 14, centroOponente);
    oponente.AgregarEdificio(centroOponente);
}

}