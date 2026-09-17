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
}