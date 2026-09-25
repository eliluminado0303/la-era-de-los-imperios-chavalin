using System.Collections.Generic;

// Punto de arranque del juego: crea el Mapa compartido, la Partida
// compartida, los 4 jugadores (1 humano + 3 IA, una por civilización),
// arma los controladores de cada jugador (ControladorMapa, ControladorCombate,
// ControladorEntrenamiento, y ControladorIA para las 3 IA) — pero YA NO
// coloca los Centros Urbanos ni los recursos aquí.
//
// Eso ahora pasa en una fase de colocación explícita: la Vista debe llamar
// a ColocarCentroUrbanoHumano(fila, columna) con el clic del jugador antes
// de que la partida arranque de verdad. Ese método coloca el centro humano
// donde el jugador eligió y ubica los 3 centros de la IA por simetría
// rotacional alrededor del centro del mapa, así el punto de partida queda
// razonablemente parejo sea cual sea el lugar elegido (y de paso resuelve
// el viejo "todos en la esquina (0,0)").
//
// La Vista solo necesita: crear UNA instancia de esta clase al empezar la
// partida, esperar a que ColocarCentroUrbanoHumano() devuelva true, y
// recién ahí empezar a llamar a Actualizar() una vez por frame desde su
// Update(). Actualizar() se protege solo: si todavía no se colocaron los
// centros, no hace nada (ver nota de PartidaEnCurso más abajo).
public class ControladorPartida
{
    private static readonly string[] CIVILIZACIONES = { "Nipones", "Griegos", "Vikingos", "Sumerios" };

    // Cuántas celdas de margen se dejan libres respecto al borde del mapa
    // para cualquier Centro Urbano (humano o IA). Con esto alcanza para que
    // ningún centro quede pegado al anillo de agua/montaña que dibuja la
    // Vista fuera de la cuadrícula lógica, y de paso deja las bases "más
    // centradas" por defecto, tal como se pidió.
    private const int MARGEN_BORDE = 4;

    // Distancia mínima (en casillas, distancia Chebyshev) entre dos Centros
    // Urbanos cualesquiera. Si la posición simétrica ideal de una IA cae
    // demasiado cerca de otra base ya colocada, se busca la celda libre más
    // cercana que sí la respete.
    private const int DISTANCIA_MINIMA_ENTRE_CENTROS = 10;

    public Mapa Mapa { get; }
    public Partida Partida { get; }

    // true recién cuando los 4 Centros Urbanos ya quedaron colocados y la
    // partida puede empezar a jugarse de verdad. Mientras sea false,
    // Actualizar() no hace nada — esto evita, entre otras cosas, que
    // VerificarFinDePartida() declare perdedor al humano por "no tener
    // edificios" durante la fase de colocación (antes de colocar su Centro
    // Urbano, la lista de edificios está vacía, y eso cuenta como derrota).
    public bool PartidaEnCurso { get; private set; } = false;

    // (jugador, civilización) en el mismo orden en que se resolvieron las
    // civilizaciones: índice 0 = humano, 1..3 = las 3 IA.
    private readonly List<(Jugador jugador, string civilizacion)> colocacionEnOrden = new List<(Jugador, string)>();

    // Índice 0 = jugador humano; 1..3 = las 3 IA, en el mismo orden que
    // colocacionEnOrden.
    public List<ControladorMapa> ControladoresMapa { get; } = new List<ControladorMapa>();
    public List<ControladorCombate> ControladoresCombate { get; } = new List<ControladorCombate>();
    public List<ControladorEntrenamiento> ControladoresEntrenamiento { get; } = new List<ControladorEntrenamiento>();

    public ControladorPartida(string nombreJugadorHumano, string civilizacionHumano)
    {
        Mapa = new Mapa();

        // El humano ocupa una de las 4 civilizaciones; las otras 3 quedan
        // para las IA, en el mismo orden de CIVILIZACIONES.
        var civilizacionesIA = new List<string>();
        foreach (var civilizacion in CIVILIZACIONES)
            if (civilizacion != civilizacionHumano) civilizacionesIA.Add(civilizacion);

        var jugadorHumano = new Jugador(nombreJugadorHumano);
        var jugadoresIA = new List<Jugador>();
        foreach (var civilizacion in civilizacionesIA)
            jugadoresIA.Add(new Jugador($"IA {civilizacion}"));

        Partida = new Partida(jugadorHumano, jugadoresIA, Mapa);

        colocacionEnOrden.Add((jugadorHumano, civilizacionHumano));
        for (int i = 0; i < jugadoresIA.Count; i++)
            colocacionEnOrden.Add((jugadoresIA[i], civilizacionesIA[i]));

        // NOTA: ya no se colocan Centros Urbanos ni recursos iniciales aquí.
        // Eso ocurre en ColocarCentroUrbanoHumano(), llamado por la Vista
        // cuando el jugador (y a continuación las 3 IA) eligen dónde arrancar.

        // Controlador del jugador humano (sin IA propia: sus decisiones
        // vienen de la Vista, no de ControladorIA).
        var gestorEntrenamientoHumano = new GestorEntrenamiento();
        ControladoresMapa.Add(new ControladorMapa(jugadorHumano, Mapa, Partida));
        ControladoresCombate.Add(new ControladorCombate(jugadorHumano, Mapa, Partida));
        ControladoresEntrenamiento.Add(new ControladorEntrenamiento(jugadorHumano, gestorEntrenamientoHumano));

        // Una IA por cada civilización restante.
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

    // Llamado por la Vista con la celda que el jugador humano eligió con el
    // clic. Devuelve false si la celda no es válida (fuera de mapa, dentro
    // del margen del borde, u ocupada) y no cambia nada — la Vista debe
    // seguir esperando otro clic. Si devuelve true, la partida ya quedó
    // lista: los 4 Centros Urbanos y los recursos iniciales están puestos,
    // y PartidaEnCurso pasa a true.
    //
    // Todo esto corre de forma síncrona en el hilo principal de Unity (el
    // mismo que procesa el clic) — nunca dispara un Task.Run ni toca la
    // cola de resultados de ningún Gestor. Colocar el Centro Urbano inicial
    // no es una "construcción" con tiempo de espera como las que arma
    // GestorConstruccion; es el punto de partida, así que no compite por
    // los mismos candados con nada que corra en segundo plano.
    public bool ColocarCentroUrbanoHumano(int fila, int columna)
    {
        if (PartidaEnCurso) return false;
        if (!PosicionValidaParaCentro(fila, columna)) return false;

        var posicionesOcupadas = new List<(int fila, int columna)>();
        ColocarCentroEn(fila, columna, colocacionEnOrden[0], posicionesOcupadas);

        // Las 3 IA se ubican por simetría rotacional (90°, 180°, 270°)
        // respecto al centro del mapa, tomando como referencia el punto que
        // eligió el humano. Así, sea cual sea el lugar que el jugador
        // escoja, las 4 bases quedan repartidas de forma pareja en vez de
        // depender de esquinas fijas.
        int centroFila = global::Mapa.FILAS / 2;
        int centroColumna = global::Mapa.COLUMNAS / 2;
        int offsetFila = fila - centroFila;
        int offsetColumna = columna - centroColumna;

        (int fila, int columna)[] candidatosIA =
        {
            (centroFila - offsetColumna, centroColumna + offsetFila), // 90°
            (centroFila - offsetFila,    centroColumna - offsetColumna), // 180°
            (centroFila + offsetColumna, centroColumna - offsetFila), // 270°
        };

        for (int i = 0; i < candidatosIA.Length; i++)
        {
            var (fFinal, cFinal) = BuscarCeldaLibreCerca(candidatosIA[i].fila, candidatosIA[i].columna, posicionesOcupadas);
            ColocarCentroEn(fFinal, cFinal, colocacionEnOrden[i + 1], posicionesOcupadas);
        }

        foreach (var (f, c) in posicionesOcupadas)
            GenerarRecursosCercaDe(f, c, centroFila, centroColumna);

        // Un botín extra en el centro del mapa: vale la pena disputarlo
        // porque queda a distancia pareja de las 4 bases, sea cual sea la
        // orientación que haya tomado la colocación.
        Mapa.ColocarRecurso(centroFila, centroColumna, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 400 });
        Mapa.ColocarRecurso(centroFila - 1, centroColumna + 1, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 400 });
        Mapa.ColocarRecurso(centroFila + 1, centroColumna - 1, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 400 });
        Mapa.ColocarRecurso(centroFila + 1, centroColumna + 1, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 300 });

        // El resto del mapa (fuera de las bases y del botín central) queda
        // plagado de bosques, vetas de oro y rebaños de comida repartidos
        // en clusters orgánicos. Se deja un colchón de celdas libres
        // alrededor de cada Centro Urbano (RADIO_COLCHON_BASE) para que el
        // jugador tenga lugar para construir sin que un bosque le tape la
        // entrada.
        var celdasReservadas = new HashSet<(int fila, int columna)>();
        foreach (var (f, c) in posicionesOcupadas)
            for (int deltaFila = -RADIO_COLCHON_BASE; deltaFila <= RADIO_COLCHON_BASE; deltaFila++)
                for (int deltaColumna = -RADIO_COLCHON_BASE; deltaColumna <= RADIO_COLCHON_BASE; deltaColumna++)
                    celdasReservadas.Add((f + deltaFila, c + deltaColumna));

        Mapa.GenerarRecursosDispersos(celdasReservadas, MARGEN_BORDE);

        PartidaEnCurso = true;
        return true;
    }

    // Radio (en casillas) del colchón libre de bosques/minas alrededor de
    // cada Centro Urbano recién colocado.
    private const int RADIO_COLCHON_BASE = 6;

    // Celda dentro del margen del borde y libre de recurso/edificio/unidad.
    // No valida terreno (agua/montaña) porque esas franjas las dibuja la
    // Vista FUERA del rango 0..FILAS-1 / 0..COLUMNAS-1 — la cuadrícula
    // lógica del Mapa nunca las toca, así que el margen alcanza para
    // garantizar que ningún centro quede pegado a ellas.
    private bool PosicionValidaParaCentro(int fila, int columna)
    {
        if (!Mapa.EsPosicionValida(fila, columna)) return false;
        if (fila < MARGEN_BORDE || fila >= global::Mapa.FILAS - MARGEN_BORDE) return false;
        if (columna < MARGEN_BORDE || columna >= global::Mapa.COLUMNAS - MARGEN_BORDE) return false;
        return Mapa.CeldaLibre(fila, columna);
    }

    private void ColocarCentroEn(int fila, int columna, (Jugador jugador, string civilizacion) datos, List<(int fila, int columna)> posicionesOcupadas)
    {
        var (jugador, civilizacion) = datos;
        var centro = new EdificioPrincipal(civilizacion, costoOro: 0, costoMadera: 0, costoComida: 0);
        Mapa.ColocarEdificio(fila, columna, centro);
        jugador.AgregarEdificio(centro);
        posicionesOcupadas.Add((fila, columna));
    }

    // Si la posición simétrica "ideal" de una IA quedó fuera del margen,
    // ocupada, o demasiado cerca de otra base ya puesta, busca en espiral
    // (anillos de radio creciente) la celda libre más cercana que sí
    // cumpla todo. En un mapa de 80x80 con solo 4 bases, siempre encuentra
    // algo mucho antes de agotar el radio máximo.
    private (int fila, int columna) BuscarCeldaLibreCerca(int filaDeseada, int columnaDeseada, List<(int fila, int columna)> posicionesOcupadas)
    {
        filaDeseada = Recortar(filaDeseada, MARGEN_BORDE, global::Mapa.FILAS - 1 - MARGEN_BORDE);
        columnaDeseada = Recortar(columnaDeseada, MARGEN_BORDE, global::Mapa.COLUMNAS - 1 - MARGEN_BORDE);

        int radioMaximo = System.Math.Max(global::Mapa.FILAS, global::Mapa.COLUMNAS);
        for (int radio = 0; radio <= radioMaximo; radio++)
        {
            for (int deltaFila = -radio; deltaFila <= radio; deltaFila++)
            {
                for (int deltaColumna = -radio; deltaColumna <= radio; deltaColumna++)
                {
                    bool esBordeDelAnillo = System.Math.Max(System.Math.Abs(deltaFila), System.Math.Abs(deltaColumna)) == radio;
                    if (!esBordeDelAnillo) continue;

                    int fila = filaDeseada + deltaFila;
                    int columna = columnaDeseada + deltaColumna;
                    if (fila < MARGEN_BORDE || fila >= global::Mapa.FILAS - MARGEN_BORDE) continue;
                    if (columna < MARGEN_BORDE || columna >= global::Mapa.COLUMNAS - MARGEN_BORDE) continue;
                    if (!Mapa.CeldaLibre(fila, columna)) continue;
                    if (DemasiadoCerca(fila, columna, posicionesOcupadas)) continue;

                    return (fila, columna);
                }
            }
        }

        // Caso extremo (no debería ocurrir en 80x80 con 4 bases): se usa la
        // celda recortada tal cual, aunque no respete la distancia mínima.
        return (filaDeseada, columnaDeseada);
    }

    private bool DemasiadoCerca(int fila, int columna, List<(int fila, int columna)> posicionesOcupadas)
    {
        foreach (var (f, c) in posicionesOcupadas)
        {
            int distancia = System.Math.Max(System.Math.Abs(f - fila), System.Math.Abs(c - columna));
            if (distancia < DISTANCIA_MINIMA_ENTRE_CENTROS) return true;
        }
        return false;
    }

    private static int Recortar(int valor, int minimo, int maximo)
    {
        if (valor < minimo) return minimo;
        if (valor > maximo) return maximo;
        return valor;
    }

    // Mismo patrón que antes tenía Mapa.GenerarRecursosIniciales() (un poco
    // de cada recurso cerca de la base, apuntando hacia el centro del mapa
    // para no intentar colocar nada fuera de rango), pero ahora relativo a
    // la posición REAL de cada Centro Urbano en vez de una esquina fija.
    private void GenerarRecursosCercaDe(int filaBase, int columnaBase, int centroFila, int centroColumna)
    {
        int dirFila = filaBase <= centroFila ? 1 : -1;
        int dirColumna = columnaBase <= centroColumna ? 1 : -1;

        Mapa.ColocarRecurso(filaBase + dirFila * 2, columnaBase + dirColumna * 1, new Recurso { Tipo = TipoRecurso.Oro, Cantidad = 100 });
        Mapa.ColocarRecurso(filaBase + dirFila * 1, columnaBase + dirColumna * 3, new Recurso { Tipo = TipoRecurso.Madera, Cantidad = 150 });
        Mapa.ColocarRecurso(filaBase + dirFila * 3, columnaBase + dirColumna * 1, new Recurso { Tipo = TipoRecurso.Comida, Cantidad = 80 });
    }

    // Llamar UNA VEZ POR FRAME desde el Update() de Unity. Mientras la fase
    // de colocación inicial no haya terminado (PartidaEnCurso == false), no
    // hace nada: ni drena colas de resultados ni revisa fin de partida.
    // Cuando ya está en curso, vacía las colas de los 4 jugadores
    // (recolección/construcción/movimiento de cada ControladorMapa,
    // entrenamiento de cada ControladorEntrenamiento) y revisa si la
    // partida ya terminó. Devuelve true cuando termina.
    public bool Actualizar()
    {
        if (!PartidaEnCurso) return false;

        foreach (var controladorMapa in ControladoresMapa) controladorMapa.ActualizarResultados();
        foreach (var controladorEntrenamiento in ControladoresEntrenamiento) controladorEntrenamiento.ActualizarResultados();

        return ControladoresMapa[0].VerificarFinDePartida(); // cualquiera de los 4 sirve: comparten la misma Partida
    }
}