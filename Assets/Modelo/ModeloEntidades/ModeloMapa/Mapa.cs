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
        lock (candado) { if (reservadas.Contains((fila, columna))) return false; }
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
    // Cada celda dibuja (y cuenta para la niebla y para los ataques) a UN
    // solo aldeano, así que dos aldeanos vivos no comparten celda. Un
    // aldeano sí puede estar encima de un recurso o un edificio (es solo
    // una posición visual, no bloquea nada), pero no encima de otro aldeano.

    private bool HayOtroAldeanoVivo(int fila, int columna, Aldeano excluir)
    {
        Aldeano otro = celdas[fila, columna].Aldeano;
        return otro != null && otro != excluir && otro.EstaVivo;
    }

    // Busca la celda de tierra más cercana al centro (por anillos, de
    // minRadio a maxRadio) donde no haya otro aldeano vivo. Dentro del mismo
    // anillo prefiere las celdas realmente libres (sin recurso, edificio ni
    // unidad) y, a igualdad, la más cercana a (filaRef, columnaRef).
    public bool BuscarCeldaParaAldeano(int filaCentro, int columnaCentro, Aldeano excluir, int minRadio, int maxRadio, int filaRef, int columnaRef, out int fila, out int columna)
    {
        fila = columna = 0;
        for (int radio = minRadio; radio <= maxRadio; radio++)
        {
            int mejorPuntaje = int.MaxValue;
            bool encontrada = false;

            for (int df = -radio; df <= radio; df++)
            {
                for (int dc = -radio; dc <= radio; dc++)
                {
                    if (System.Math.Max(System.Math.Abs(df), System.Math.Abs(dc)) != radio) continue; // solo el borde del anillo

                    int f = filaCentro + df;
                    int c = columnaCentro + dc;
                    if (!EsTierra(f, c) || HayOtroAldeanoVivo(f, c, excluir)) continue;

                    int puntaje = (CeldaLibre(f, c) ? 0 : 1000)
                                + System.Math.Max(System.Math.Abs(f - filaRef), System.Math.Abs(c - columnaRef));
                    if (puntaje < mejorPuntaje) { mejorPuntaje = puntaje; fila = f; columna = c; encontrada = true; }
                }
            }
            if (encontrada) return true;
        }
        return false;
    }

    // Coloca (o reubica) un aldeano en una celda. Si ya hay otro aldeano
    // ahí, lo deja en la celda libre más cercana en vez de apilarlos.
    public void ColocarAldeano(int fila, int columna, Aldeano aldeano)
    {
        if (!EsPosicionValida(fila, columna)) return;
        lock (candado)
        {
            if (HayOtroAldeanoVivo(fila, columna, aldeano) &&
                BuscarCeldaParaAldeano(fila, columna, aldeano, 1, 4, fila, columna, out int filaLibre, out int columnaLibre))
            {
                fila = filaLibre;
                columna = columnaLibre;
            }

            if (aldeano.Fila >= 0 && aldeano.Columna >= 0 && EsPosicionValida(aldeano.Fila, aldeano.Columna) &&
                celdas[aldeano.Fila, aldeano.Columna].Aldeano == aldeano)
                celdas[aldeano.Fila, aldeano.Columna].Aldeano = null;

            celdas[fila, columna].Aldeano = aldeano;
            aldeano.Fila = fila;
            aldeano.Columna = columna;
        }
    }

    // Desplaza a un aldeano vivo hasta otra celda. A diferencia de
    // ColocarAldeano (que se usa al aparecer), acá se exige que el destino
    // sea tierra (un aldeano no camina sobre el agua) y que no lo ocupe otro
    // aldeano: si otro llegó primero, el desplazamiento falla y quien lo
    // pidió puede elegir otra celda.
    public bool MoverAldeano(Aldeano aldeano, int filaDestino, int columnaDestino)
    {
        if (aldeano == null || !aldeano.EstaVivo) return false;
        if (!EsPosicionValida(filaDestino, columnaDestino)) return false;
        lock (candado)
        {
            if (celdas[filaDestino, columnaDestino].Terreno != TipoTerreno.Tierra) return false;
            if (aldeano.Fila < 0 || aldeano.Columna < 0) return false; // todavía no está en el mapa
            if (HayOtroAldeanoVivo(filaDestino, columnaDestino, aldeano)) return false;

            celdas[filaDestino, columnaDestino].Aldeano = aldeano;
            if (EsPosicionValida(aldeano.Fila, aldeano.Columna) &&
                celdas[aldeano.Fila, aldeano.Columna].Aldeano == aldeano &&
                (aldeano.Fila != filaDestino || aldeano.Columna != columnaDestino))
                celdas[aldeano.Fila, aldeano.Columna].Aldeano = null;

            aldeano.Fila = filaDestino;
            aldeano.Columna = columnaDestino;
            return true;
        }
    }

    // Saca al aldeano de su celda (cuando muere). Solo borra la celda si
    // el que está ahí es ese mismo aldeano.
    public bool RetirarAldeano(Aldeano aldeano)
    {
        if (aldeano == null || !EsPosicionValida(aldeano.Fila, aldeano.Columna)) return false;
        lock (candado)
        {
            Celda celda = celdas[aldeano.Fila, aldeano.Columna];
            if (celda.Aldeano != aldeano) return false;
            celda.Aldeano = null;
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
            if ((celda.Recurso != null && !celda.Recurso.EstaAgotado()) || celda.Edificio != null) return false;
            celda.Edificio = edificio;
            edificio.Fila = fila;
            edificio.Columna = columna;
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
    // Saca a ese edificio concreto de su celda (cuando lo destruyen). Solo
    // borra la celda si el que está ahí es ese mismo edificio.
    public bool RetirarEdificio(Edificio edificio)
    {
        if (edificio == null || !EsPosicionValida(edificio.Fila, edificio.Columna)) return false;
        lock (candado)
        {
            Celda celda = celdas[edificio.Fila, edificio.Columna];
            if (celda.Edificio != edificio) return false;
            celda.Edificio = null;
            return true;
        }
    }

        public bool ColocarUnidad(int fila, int columna, Unidad unidad)
    {
        if (!EsPosicionValida(fila, columna)) return false;
        lock (candado)
        {
            Celda celda = celdas[fila, columna];
            if (celda.Unidad != null || celda.Edificio != null) return false;
            celda.Unidad = unidad;
            unidad.Fila = fila;
            unidad.Columna = columna;
            return true;
        }
    }

    // Saca a una unidad de su celda (cuando muere). Solo borra la celda si
    // el que está ahí es esa misma unidad, para no pisar a otra.
    public bool RetirarUnidad(Unidad unidad)
    {
        if (unidad == null || !EsPosicionValida(unidad.Fila, unidad.Columna)) return false;
        lock (candado)
        {
            Celda celda = celdas[unidad.Fila, unidad.Columna];
            if (celda.Unidad != unidad) return false;
            celda.Unidad = null;
            return true;
        }
    }

    // Busca la celda libre más cercana a (filaDesde, columnaDesde) que esté
    // a distancia <= rango del objetivo. Es donde una unidad se para para
    // poder atacar (cuerpo a cuerpo: una celda pegada al objetivo; a
    // distancia: el borde de su alcance).
    public bool BuscarCeldaLibreEnRango(int filaObjetivo, int columnaObjetivo, int rango, int filaDesde, int columnaDesde, out int fila, out int columna)
    {
        rango = System.Math.Max(1, rango);
        int mejorDistancia = int.MaxValue;
        fila = columna = 0;
        bool encontrada = false;

        for (int df = -rango; df <= rango; df++)
        {
            for (int dc = -rango; dc <= rango; dc++)
            {
                int f = filaObjetivo + df;
                int c = columnaObjetivo + dc;
                if (!CeldaLibre(f, c)) continue;

                int distancia = System.Math.Max(System.Math.Abs(f - filaDesde), System.Math.Abs(c - columnaDesde));
                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    fila = f;
                    columna = c;
                    encontrada = true;
                }
            }
        }
        return encontrada;
    }

    // Aldeanos vivos dentro del radio (distancia de tablero). Lo usa la IA
    // para encontrar aldeanos enemigos a los que atacar.
    public List<Aldeano> AldeanosEnRadio(int filaCentro, int columnaCentro, int radio)
    {
        var encontrados = new List<Aldeano>();
        int filaMin = System.Math.Max(0, filaCentro - radio);
        int filaMax = System.Math.Min(FILAS - 1, filaCentro + radio);
        int columnaMin = System.Math.Max(0, columnaCentro - radio);
        int columnaMax = System.Math.Min(COLUMNAS - 1, columnaCentro + radio);

        for (int fila = filaMin; fila <= filaMax; fila++)
            for (int columna = columnaMin; columna <= columnaMax; columna++)
            {
                Aldeano aldeano = celdas[fila, columna].Aldeano;
                if (aldeano != null && aldeano.EstaVivo) encontrados.Add(aldeano);
            }
        return encontrados;
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
            // Mantiene la posición guardada en la propia unidad al día
            // (ColocarUnidad ya lo hacía; al mover se quedaba desactualizada).
            destino.Unidad.Fila = filaDestino;
            destino.Unidad.Columna = columnaDestino;
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
    private readonly HashSet<(int, int)> reservadas = new HashSet<(int, int)>();

    public bool ReservarDestino(int fila, int columna)
    {
    lock (candado)
    {
        if (!CeldaLibre(fila, columna)) return false;
        reservadas.Add((fila, columna));
        return true;
    }
    }

    public void LiberarReserva(int fila, int columna)
    {
    lock (candado) { reservadas.Remove((fila, columna)); }
    }
}