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

    public void GenerarRecursosIniciales() 
    {
        // Un poco de cada recurso cerca de cada esquina, para que cada
        // civilización arranque su economía sin tener que pelear de entrada.
        (int fila, int columna)[] esquinas = { (0, 0), (0, COLUMNAS - 1), (FILAS - 1, 0), (FILAS - 1, COLUMNAS - 1) };
        foreach (var (fEsquina, cEsquina) in esquinas) {
            int dirFila = fEsquina == 0 ? 1 : -1;
            int dirColumna = cEsquina == 0 ? 1 : -1;
            ColocarRecurso(fEsquina + dirFila * 2, cEsquina + dirColumna * 1, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 100 });
            ColocarRecurso(fEsquina + dirFila * 1, cEsquina + dirColumna * 3, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 150 });
            ColocarRecurso(fEsquina + dirFila * 3, cEsquina + dirColumna * 1, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 80 });
        }

        // El centro concentra los mejores recursos: más cantidad, y queda a
        // la misma distancia de las 4 esquinas, así que vale la pena disputarlo.
        int centro = FILAS / 2;
        ColocarRecurso(centro, centro, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 400 });
        ColocarRecurso(centro - 1, centro + 1, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 400 });
        ColocarRecurso(centro + 1, centro - 1, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 400 });
        ColocarRecurso(centro + 1, centro + 1, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 300 });
    }

   // Coloca hasta 4 Centros Urbanos, uno en cada esquina del mapa 15x15.
    // Recibe (jugador, civilizacion) en vez de solo Jugador porque
    // EdificioPrincipal necesita la CIVILIZACIÓN, no el nombre del jugador
    // (antes se le pasaba jugador.Nombre por error).
    public void ColocarCentrosUrbanosIniciales(List<(Jugador jugador, string civilizacion)> jugadoresEnOrden) {
        (int fila, int columna)[] esquinas = { (0, 0), (0, COLUMNAS - 1), (FILAS - 1, 0), (FILAS - 1, COLUMNAS - 1) };

        for (int i = 0; i < jugadoresEnOrden.Count && i < esquinas.Length; i++) {
            var (fila, columna) = esquinas[i];
            var (jugador, civilizacion) = jugadoresEnOrden[i];
            var centro = new EdificioPrincipal(civilizacion, costoOro: 0, costoMadera: 0, costoComida: 0);
            ColocarEdificio(fila, columna, centro);
            jugador.AgregarEdificio(centro);
        }
    }
}