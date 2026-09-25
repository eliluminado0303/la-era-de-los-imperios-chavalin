using System.Collections.Generic;
public class Mapa
{
    public const int FILAS = 60;
    public const int COLUMNAS = 60;

    private Celda[,] celdas;

    private readonly object candado = new object();

    public Mapa()
    {
        celdas = new Celda[FILAS, COLUMNAS];
        InicializarMapa();
    }

    // Grosor (en casillas) de la franja de agua que rodea la isla por los
    // 4 lados. Debe coincidir con lo que dibuja VistaMapa: lo que se ve
    // como "playa" también es agua a efectos de juego (ahí no se construye
    // ni se mueve nadie).
    public const int ANCHO_AGUA_PERIMETRAL = 6;

    private void InicializarMapa()
    {
        // El mapa se GENERA (ya no depende del tilemap dibujado): una isla
        // de tierra rodeada de agua perimetral. La lógica solo conoce las
        // celdas 0..FILAS-1 / 0..COLUMNAS-1; el agua decorativa de afuera
        // la dibuja VistaMapa con sus anillos, así que acá marcamos como
        // Agua la franja interna de ANCHO_AGUA_PERIMETRAL casillas.
        for (int fila = 0; fila < FILAS; fila++)
        {
            for (int columna = 0; columna < COLUMNAS; columna++)
            {
                bool esAgua = fila < ANCHO_AGUA_PERIMETRAL || fila >= FILAS - ANCHO_AGUA_PERIMETRAL
                           || columna < ANCHO_AGUA_PERIMETRAL || columna >= COLUMNAS - ANCHO_AGUA_PERIMETRAL;

                celdas[fila, columna] = new Celda
                {
                    Fila = fila,
                    Columna = columna,
                    Terreno = esAgua ? TipoTerreno.Agua : TipoTerreno.Tierra,
                    Recurso = null,
                    Edificio = null,
                    Unidad = null
                };
            }
        }
    }

    // Consultas de terreno para movimiento/construcción: las celdas de agua
    // NO son transitables ni construibles. Las unidades actuales son
    // terrestres; si algún día hay barcos, se agrega un "EsNavegable".
    public bool EsTierra(int fila, int columna)
    {
        if (!EsPosicionValida(fila, columna)) return false;
        return celdas[fila, columna].Terreno == TipoTerreno.Tierra;
    }

    public bool EsAgua(int fila, int columna)
    {
        if (!EsPosicionValida(fila, columna)) return false;
        return celdas[fila, columna].Terreno == TipoTerreno.Agua;
    }

    public Celda ObtenerCelda(int fila, int columna)
    {
        if (!EsPosicionValida(fila, columna)) return null;
        return celdas[fila, columna];
    }

    public bool CeldaLibre(int fila, int columna) {
        if (!EsPosicionValida(fila, columna)) return false;
        Celda celda = celdas[fila, columna];
        // El agua nunca está "libre": no se puede construir ahí ni aparcar
        // unidades. (MoverUnidad lo verifica aparte con EsTierra.)
        if (celda.Terreno != TipoTerreno.Tierra) return false;
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
            if (celda.Terreno != TipoTerreno.Tierra) return false; // nada crece en el agua
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
            if (celda.Terreno != TipoTerreno.Tierra) return false; // no se construye sobre el agua
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
            // Las unidades terrestres no pueden entrar al agua.
            if (destino.Terreno != TipoTerreno.Tierra) return false;
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

    // ---------------------------------------------------------------
    // Generación de recursos DISPERSOS por todo el mapa (bosques, vetas de
    // oro, rebaños de comida). Se llama una sola vez, justo después de
    // colocar los 4 Centros Urbanos y los recursos "de arranque" cerca de
    // cada base — así el resto del mapa no queda vacío y hay motivo para
    // expandirse y pelear por territorio.
    //
    // "celdasReservadas" es el colchón de celdas que se deja libre a
    // propósito alrededor de cada base (para que el jugador tenga lugar
    // para construir sin que un bosque le tape la entrada).
    // "margen" mantiene los clusters lejos de la franja de agua.
    public void GenerarRecursosDispersos(HashSet<(int fila, int columna)> celdasReservadas, int margen)
    {
        var rng = new System.Random();

        // Bosques de madera: clusters grandes e irregulares, son los que
        // más "llenan" visualmente el mapa (como un bosque real).
        GenerarClusters(rng, TipoRecurso.Madera, cantidadClusters: 55, tamañoMin: 4, tamañoMax: 10, cantidadPorCelda: 120, celdasReservadas, margen);

        // Vetas de oro: clusters chicos y más escasos, para que valga la
        // pena disputarlos.
        GenerarClusters(rng, TipoRecurso.Oro, cantidadClusters: 24, tamañoMin: 2, tamañoMax: 4, cantidadPorCelda: 250, celdasReservadas, margen);

        // Rebaños de comida: clusters chicos repartidos entre los bosques.
        GenerarClusters(rng, TipoRecurso.Comida, cantidadClusters: 28, tamañoMin: 2, tamañoMax: 5, cantidadPorCelda: 100, celdasReservadas, margen);
    }

    private void GenerarClusters(System.Random rng, TipoRecurso tipo, int cantidadClusters, int tamañoMin, int tamañoMax, int cantidadPorCelda, HashSet<(int fila, int columna)> celdasReservadas, int margen)
    {
        int intentosMaximos = cantidadClusters * 25; // por si muchas semillas caen en celdas ocupadas/reservadas
        int clustersColocados = 0;
        int intentos = 0;

        while (clustersColocados < cantidadClusters && intentos < intentosMaximos)
        {
            intentos++;

            int filaSemilla = rng.Next(margen, FILAS - margen);
            int columnaSemilla = rng.Next(margen, COLUMNAS - margen);

            if (!CeldaLibre(filaSemilla, columnaSemilla)) continue;
            if (celdasReservadas.Contains((filaSemilla, columnaSemilla))) continue;

            int tamañoCluster = rng.Next(tamañoMin, tamañoMax + 1);
            var celdasCluster = CrecerCluster(rng, filaSemilla, columnaSemilla, tamañoCluster, celdasReservadas);
            if (celdasCluster.Count == 0) continue;

            foreach (var (f, c) in celdasCluster)
                ColocarRecurso(f, c, new Recurso { Tipo = tipo, Cantidad = cantidadPorCelda });

            clustersColocados++;
        }
    }

    // Crece un cluster orgánico (no un cuadrado perfecto) a partir de una
    // celda semilla: expande hacia vecinos ortogonales elegidos al azar
    // hasta llegar al tamaño pedido o quedarse sin frontera disponible.
    private List<(int fila, int columna)> CrecerCluster(System.Random rng, int filaInicio, int columnaInicio, int tamaño, HashSet<(int fila, int columna)> celdasReservadas)
    {
        var resultado = new List<(int, int)>();
        var frontera = new List<(int, int)> { (filaInicio, columnaInicio) };
        var visitadas = new HashSet<(int, int)> { (filaInicio, columnaInicio) };

        while (resultado.Count < tamaño && frontera.Count > 0)
        {
            int indice = rng.Next(frontera.Count);
            var (fila, columna) = frontera[indice];
            frontera.RemoveAt(indice);

            if (!CeldaLibre(fila, columna) || celdasReservadas.Contains((fila, columna))) continue;
            resultado.Add((fila, columna));

            (int, int)[] vecinos = { (fila - 1, columna), (fila + 1, columna), (fila, columna - 1), (fila, columna + 1) };
            foreach (var (filaVecina, columnaVecina) in vecinos)
            {
                if (EsPosicionValida(filaVecina, columnaVecina) && !visitadas.Contains((filaVecina, columnaVecina)))
                {
                    visitadas.Add((filaVecina, columnaVecina));
                    frontera.Add((filaVecina, columnaVecina));
                }
            }
        }

        return resultado;
    }
}