using System.Collections.Generic;
public class Mapa
{
    public const int FILAS = 15;
    public const int COLUMNAS = 15;

    private Celda[,] celdas;

    
    private readonly object candado = new object();

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
        if (!EsPosicionValida(fila, columna)) return null;
        return celdas[fila, columna];
    }

    public bool CeldaLibre(int fila, int columna) {
        if (!EsPosicionValida(fila, columna)) return false;
        Celda celda = celdas[fila, columna];
        return celda.Recurso == null && celda.Edificio == null && celda.Unidad == null;
    }

    public bool EsPosicionValida(int fila, int columna)
    {
        return fila >= 0 && fila < FILAS && columna >= 0 && columna < COLUMNAS;
    }

    public bool ColocarRecurso(int fila, int columna, Recurso recurso)
    {
        if (!EsPosicionValida(fila, columna)) return false;
        lock (candado)
        {
            Celda celda = celdas[fila, columna];
            if (celda.Recurso != null || celda.Edificio != null) return false;
            celda.Recurso = recurso;
            return true;
        }
    }

    public bool RetirarRecurso(int fila, int columna)
    {
        if (!EsPosicionValida(fila, columna)) return false;
        lock (candado)
        {
            Celda celda = celdas[fila, columna];
            if (celda.Recurso == null) return false;
            celda.Recurso = null;
            return true;
        }
    }

    public bool ColocarEdificio(int fila, int columna, Edificio edificio)
    {
        if (!EsPosicionValida(fila, columna)) return false;
        lock (candado)
        {
            Celda celda = celdas[fila, columna];
            if (celda.Recurso != null || celda.Edificio != null) return false;
            celda.Edificio = edificio;
            return true;
        }
    }

    public bool RetirarEdificio(int fila, int columna)
    {
        if (!EsPosicionValida(fila, columna)) return false;
        lock (candado)
        {
            Celda celda = celdas[fila, columna];
            if (celda.Edificio == null) return false;
            celda.Edificio = null;
            return true;
        }
    }

    public bool MoverUnidad(int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino) {
        if (!EsPosicionValida(filaOrigen, columnaOrigen) || !EsPosicionValida(filaDestino, columnaDestino)) {
            return false;
        }
        lock (candado)
        {
            Celda origen = celdas[filaOrigen, columnaOrigen];
            Celda destino = celdas[filaDestino, columnaDestino];

            if (origen.Unidad == null) return false;
            if (destino.Unidad != null || destino.Edificio != null) return false;

            destino.Unidad = origen.Unidad;
            origen.Unidad = null;
            return true;
        }
    }

    // --- Métodos de consulta para la IA (no modifican el mapa, solo informan) ---

    public List<(int fila, int columna)> BuscarRecursosDisponibles() {
        List<(int, int)> encontrados = new List<(int, int)>();
        for (int fila = 0; fila < FILAS; fila++)
            for (int columna = 0; columna < COLUMNAS; columna++) {
                Celda celda = celdas[fila, columna];
                if (celda.Recurso != null && !celda.Recurso.EstaAgotado())
                    encontrados.Add((fila, columna));
            }
        return encontrados;
    }

    public List<(int fila, int columna)> BuscarCeldasLibres() {
        List<(int, int)> libres = new List<(int, int)>();
        for (int fila = 0; fila < FILAS; fila++)
            for (int columna = 0; columna < COLUMNAS; columna++)
                if (CeldaLibre(fila, columna)) libres.Add((fila, columna));
        return libres;
    }

    public List<(int fila, int columna)> BuscarUnidades() {
        List<(int, int)> unidades = new List<(int, int)>();
        for (int fila = 0; fila < FILAS; fila++)
            for (int columna = 0; columna < COLUMNAS; columna++)
                if (celdas[fila, columna].Unidad != null) unidades.Add((fila, columna));
        return unidades;
    }

    //  — lo que necesita ControladorIA.BuscarUnidadesEnemigasCerca().
    
    public List<(int fila, int columna)> UnidadesEnRadio(int filaCentro, int columnaCentro, int radio) {
        List<(int, int)> encontrados = new List<(int, int)>();

        int filaMin = System.Math.Max(0, filaCentro - radio);
        int filaMax = System.Math.Min(FILAS - 1, filaCentro + radio);
        int columnaMin = System.Math.Max(0, columnaCentro - radio);
        int columnaMax = System.Math.Min(COLUMNAS - 1, columnaCentro + radio);

        for (int fila = filaMin; fila <= filaMax; fila++) {
            for (int columna = columnaMin; columna <= columnaMax; columna++) {
                if (fila == filaCentro && columna == columnaCentro) continue;
                if (celdas[fila, columna].Unidad != null) {
                    encontrados.Add((fila, columna));
                }
            }
        }
        return encontrados;
    }

    public void GenerarRecursosIniciales() {
        ColocarRecurso(2, 2, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 100 });
        ColocarRecurso(3, 1, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 150 });
        ColocarRecurso(1, 4, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 80 });

        ColocarRecurso(12, 12, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 100 });
        ColocarRecurso(11, 13, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 150 });
        ColocarRecurso(13, 10, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 80 });

        ColocarRecurso(7, 7, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 200 });
    }

    public void ColocarCentrosUrbanosIniciales(Jugador jugadorHumano, Jugador oponente) {
        
        EdificioPrincipal centroJugador = new EdificioPrincipal(jugadorHumano.Nombre, costoOro: 0, costoMadera: 0, costoComida: 0);
        ColocarEdificio(0, 0, centroJugador);
        jugadorHumano.AgregarEdificio(centroJugador);

        EdificioPrincipal centroOponente = new EdificioPrincipal(oponente.Nombre, costoOro: 0, costoMadera: 0, costoComida: 0);
        ColocarEdificio(14, 14, centroOponente);
        oponente.AgregarEdificio(centroOponente);
    }
}