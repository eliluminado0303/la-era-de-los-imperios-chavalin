using System;
using System.Collections.Generic;

// Punto de arranque del juego: crea el Mapa compartido, la Partida
// compartida, los 4 jugadores (1 humano + 3 IA, una por civilización),
// coloca los 4 Centros Urbanos EN LAS ESQUINAS (con margen respecto al
// borde real del mapa, para que quede espacio para construir alrededor),
// siembra los recursos de arranque de cada base, un botín central, y
// además reparte clusters de recursos por TODO el resto del mapa (para
// que explorar tenga sentido), y arma los controladores de cada jugador
// (ControladorMapa, ControladorCombate, ControladorEntrenamiento, y
// ControladorIA para las 3 IA).
//
// Todo esto ya queda listo al terminar el constructor — no hay fase de
// colocación manual: la Vista solo necesita crear UNA instancia de esta
// clase y llamar a Actualizar() una vez por frame desde su Update().
public class ControladorPartida
{
    private static readonly string[] CIVILIZACIONES = { "Nipones", "Griegos", "Vikingos", "Sumerios" };
    private static readonly Random aleatorio = new Random();

    // Cuántas celdas de margen se dejan libres respecto al borde real del
    // mapa (0..FILAS-1 / 0..COLUMNAS-1) para CUALQUIER Centro Urbano. Se
    // subió de 5 a 8 para que la base quede más lejos de la esquina real
    // (no pegada al agua) y así el círculo de visión inicial (ver
    // radioVisionEdificio en VistaMapa) deje un hueco de verdad para
    // construir, en vez de quedar recortado contra el borde del mapa.
    private const int MARGEN_BORDE = 8;

    // Radio (en casillas) que se deja SIN recursos alrededor de cada Centro
    // Urbano — el "colchón" de espacio libre para construir cuartel, etc.
    private const int RADIO_COLCHON_BASE = 6;

    // Radio libre de recursos alrededor del botín central.
    private const int RADIO_COLCHON_CENTRO = 5;

    public Mapa Mapa { get; }
    public Partida Partida { get; }

    // Posición (fila, columna) donde quedó el Centro Urbano del jugador
    // humano. La Vista la usa para centrar la cámara ahí al arrancar — si
    // no, la cámara se queda mirando el (0,0) del mundo, que es una esquina
    // de agua/tierra sin nada, y da la sensación de que "la niebla tapa
    // todo el mapa" cuando en realidad la base y su alrededor ya están
    // revelados, solo que fuera de cámara.
    public (int fila, int columna) PosicionBaseHumana { get; private set; }

    // Índice 0 = jugador humano; 1..3 = las 3 IA, en el mismo orden en que
    // se resolvieron las civilizaciones.
    public List<ControladorMapa> ControladoresMapa { get; } = new List<ControladorMapa>();
    public List<ControladorCombate> ControladoresCombate { get; } = new List<ControladorCombate>();
    public List<ControladorEntrenamiento> ControladoresEntrenamiento { get; } = new List<ControladorEntrenamiento>();

    public ControladorPartida(string nombreJugadorHumano, string civilizacionHumano)
    {
        Mapa = new Mapa();

        var civilizacionesIA = new List<string>();
        foreach (var civilizacion in CIVILIZACIONES)
            if (civilizacion != civilizacionHumano) civilizacionesIA.Add(civilizacion);

        var jugadorHumano = new Jugador(nombreJugadorHumano);
        var jugadoresIA = new List<Jugador>();
        foreach (var civilizacion in civilizacionesIA)
            jugadoresIA.Add(new Jugador($"IA {civilizacion}"));

        Partida = new Partida(jugadorHumano, jugadoresIA, Mapa);

        var colocacionEnOrden = new List<(Jugador jugador, string civilizacion)> { (jugadorHumano, civilizacionHumano) };
        for (int i = 0; i < jugadoresIA.Count; i++)
            colocacionEnOrden.Add((jugadoresIA[i], civilizacionesIA[i]));

        ColocarBasesYRecursos(colocacionEnOrden);

        var gestorEntrenamientoHumano = new GestorEntrenamiento();
        ControladoresMapa.Add(new ControladorMapa(jugadorHumano, Mapa, Partida));
        ControladoresCombate.Add(new ControladorCombate(jugadorHumano, Mapa, Partida));
        ControladoresEntrenamiento.Add(new ControladorEntrenamiento(jugadorHumano, gestorEntrenamientoHumano));

        for (int i = 0; i < jugadoresIA.Count; i++)
        {
            var jugadorIA = jugadoresIA[i];
            var civilizacionIA = civilizacionesIA[i];
            var gestorEntrenamientoIA = new GestorEntrenamiento();

            var controladorIA = new ControladorIA(jugadorIA, civilizacionIA, Mapa, Partida, gestorEntrenamientoIA);

            ControladoresMapa.Add(new ControladorMapa(jugadorIA, Mapa, Partida, controladorIA));
            ControladoresCombate.Add(new ControladorCombate(jugadorIA, Mapa, Partida));
            ControladoresEntrenamiento.Add(new ControladorEntrenamiento(jugadorIA, gestorEntrenamientoIA, controladorIA));
        }
    }

    // ---------------------------------------------------------------
    // Colocación de bases: 4 esquinas fijas, con margen respecto al borde.
    // ---------------------------------------------------------------

    private void ColocarBasesYRecursos(List<(Jugador jugador, string civilizacion)> colocacionEnOrden)
    {
        var esquinas = ObtenerEsquinas();
        var posicionesOcupadas = new List<(int fila, int columna)>();

        for (int i = 0; i < colocacionEnOrden.Count && i < esquinas.Length; i++)
        {
            var (fila, columna) = esquinas[i];
            ColocarCentroEn(fila, columna, colocacionEnOrden[i], posicionesOcupadas);
            if (i == 0) PosicionBaseHumana = (fila, columna); // índice 0 siempre es el humano
        }

        int centroFila = global::Mapa.FILAS / 2;
        int centroColumna = global::Mapa.COLUMNAS / 2;

        foreach (var (f, c) in posicionesOcupadas)
            GenerarRecursosCercaDe(f, c, centroFila, centroColumna);

        Mapa.ColocarRecurso(centroFila, centroColumna, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 500 });
        Mapa.ColocarRecurso(centroFila - 1, centroColumna + 1, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 500 });
        Mapa.ColocarRecurso(centroFila + 1, centroColumna - 1, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 300 });
        Mapa.ColocarRecurso(centroFila + 1, centroColumna + 1, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 400 });

        SembrarRecursosDispersos(posicionesOcupadas, centroFila, centroColumna);
    }

    private (int fila, int columna)[] ObtenerEsquinas()
    {
        int filaCercana = MARGEN_BORDE;
        int filaLejana = global::Mapa.FILAS - 1 - MARGEN_BORDE;
        int columnaCercana = MARGEN_BORDE;
        int columnaLejana = global::Mapa.COLUMNAS - 1 - MARGEN_BORDE;

        return new (int, int)[]
        {
            (filaCercana, columnaCercana),
            (filaCercana, columnaLejana),
            (filaLejana, columnaCercana),
            (filaLejana, columnaLejana),
        };
    }

    // Reserva inicial que se le da a CADA jugador (humano y las 3 IA por
    // igual) apenas se coloca su Centro Urbano. Sin esto el juego no tiene
    // forma de arrancar: los recursos del mapa son solo vetas/bosques que
    // hace falta un aldeano para recolectar, pero el primer aldeano cuesta
    // 25 oro + 30 comida y arrancabas con 0 de todo — nadie podía pagar ni
    // el primer aldeano. Esto alcanza para varios aldeanos de arranque y
    // deja algo de colchón para el primer Cuartel (150 oro / 100 madera)
    // mientras juntás más del mapa.
    private const int ORO_INICIAL = 150;
    private const int MADERA_INICIAL = 150;
    private const int COMIDA_INICIAL = 150;

    private void ColocarCentroEn(int fila, int columna, (Jugador jugador, string civilizacion) datos, List<(int fila, int columna)> posicionesOcupadas)
    {
        var (jugador, civilizacion) = datos;
        var centro = new EdificioPrincipal(civilizacion, costoOro: 0, costoMadera: 0, costoComida: 0);
        // Los edificios nacen "en construcción" (EstaConstruido = false) y
        // ProducirAldeano() se niega a producir nada hasta que eso cambie —
        // normalmente lo hace GestorDeConstruccion cuando termina de
        // construirse un edificio a mitad de partida. El Centro Urbano
        // inicial nunca pasa por ese camino, así que sin esta línea quedaba
        // "construyéndose" para siempre y jamás podía entrenar un aldeano,
        // por más oro que tuvieras.
        centro.AvanzarConstruccion(100);
        Mapa.ColocarEdificio(fila, columna, centro);
        jugador.AgregarEdificio(centro);
        posicionesOcupadas.Add((fila, columna));

        jugador.AgregarRecurso(TipoRecurso.Oro, ORO_INICIAL);
        jugador.AgregarRecurso(TipoRecurso.Madera, MADERA_INICIAL);
        jugador.AgregarRecurso(TipoRecurso.Comida, COMIDA_INICIAL);
    }

    private void GenerarRecursosCercaDe(int filaBase, int columnaBase, int centroFila, int centroColumna)
    {
        int dirFila = filaBase <= centroFila ? 1 : -1;
        int dirColumna = columnaBase <= centroColumna ? 1 : -1;

        Mapa.ColocarRecurso(filaBase + dirFila * 2, columnaBase + dirColumna * 1, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 250 });
        Mapa.ColocarRecurso(filaBase + dirFila * 1, columnaBase + dirColumna * 3, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 300 });
        Mapa.ColocarRecurso(filaBase + dirFila * 3, columnaBase + dirColumna * 1, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 180 });
        Mapa.ColocarRecurso(filaBase + dirFila * 2, columnaBase + dirColumna * 4, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 200 });
    }

    // ---------------------------------------------------------------
    // Recursos dispersos por el resto del mapa.
    // ---------------------------------------------------------------

    private void SembrarRecursosDispersos(List<(int fila, int columna)> centrosBase, int centroFila, int centroColumna)
    {
        const int ESPACIADO = 7;   // antes 11 — grilla más tupida = más clusters repartidos
        const int JITTER = 3;

        for (int filaBase = MARGEN_BORDE; filaBase < global::Mapa.FILAS - MARGEN_BORDE; filaBase += ESPACIADO)
        {
            for (int columnaBase = MARGEN_BORDE; columnaBase < global::Mapa.COLUMNAS - MARGEN_BORDE; columnaBase += ESPACIADO)
            {
                int fila = Recortar(filaBase + aleatorio.Next(-JITTER, JITTER + 1), MARGEN_BORDE, global::Mapa.FILAS - 1 - MARGEN_BORDE);
                int columna = Recortar(columnaBase + aleatorio.Next(-JITTER, JITTER + 1), MARGEN_BORDE, global::Mapa.COLUMNAS - 1 - MARGEN_BORDE);

                if (DemasiadoCerca(fila, columna, centrosBase, RADIO_COLCHON_BASE)) continue;
                if (DistanciaChebyshev(fila, columna, centroFila, centroColumna) < RADIO_COLCHON_CENTRO) continue;

                SembrarClusterEn(fila, columna);
            }
        }
    }

    // Un cluster = 1 celda central + 5 vecinas del mismo tipo (antes eran
    // solo 2 vecinas) — se siente más como una veta de oro real o un
    // bosquecito, no un puñado de puntos sueltos.
    private void SembrarClusterEn(int fila, int columna)
    {
        TipoRecurso tipo = (TipoRecurso)aleatorio.Next(0, 3);
        int cantidad = tipo == TipoRecurso.Oro ? 300 : tipo == TipoRecurso.Madera ? 350 : 220;

        (int deltaFila, int deltaColumna)[] celdasDelCluster =
        {
            (0, 0), (1, 0), (0, 1), (-1, 0), (0, -1), (1, 1),
        };
        foreach (var (deltaFila, deltaColumna) in celdasDelCluster)
        {
            int f = fila + deltaFila;
            int c = columna + deltaColumna;
            if (!Mapa.EsPosicionValida(f, c) || !Mapa.CeldaLibre(f, c)) continue;
            Mapa.ColocarRecurso(f, c, new Recurso { Tipo = tipo, Cantidad = cantidad });
        }
    }

    private bool DemasiadoCerca(int fila, int columna, List<(int fila, int columna)> posiciones, int radioMinimo)
    {
        foreach (var (f, c) in posiciones)
            if (DistanciaChebyshev(fila, columna, f, c) < radioMinimo) return true;
        return false;
    }

    private static int DistanciaChebyshev(int f1, int c1, int f2, int c2)
        => Math.Max(Math.Abs(f1 - f2), Math.Abs(c1 - c2));

    private static int Recortar(int valor, int minimo, int maximo)
    {
        if (valor < minimo) return minimo;
        if (valor > maximo) return maximo;
        return valor;
    }

    public bool Actualizar()
    {
        foreach (var controladorMapa in ControladoresMapa) controladorMapa.ActualizarResultados();
        foreach (var controladorEntrenamiento in ControladoresEntrenamiento) controladorEntrenamiento.ActualizarResultados();

        return ControladoresMapa[0].VerificarFinDePartida();
    }
}