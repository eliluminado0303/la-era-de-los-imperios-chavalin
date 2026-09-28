using System;
using System.Collections.Generic;
using System.Linq;

// Controla las decisiones de UNA civilización manejada por la IA (se crea
// una instancia por cada oponente IA — con 4 civilizaciones y 1 humano,
// habría 3 de estos activos a la vez).
//
// Combina dos modos:
//  - REACTIVO: se suscribe a los eventos de sus propias unidades y
//    edificios (FueAtacada, FueAtacado, Muerte) y decide en el momento en
//    que algo relevante ocurre (por ejemplo, contraatacar a quien la
//    golpeó).
//  - ACTIVO (ver ActualizarCombate): cada 1.5s escanea un radio amplio
//    alrededor de cada unidad viva buscando enemigos, y si encuentra uno
//    se mueve a rango de ataque o ataca directamente. Sin esto, con bases
//    lejos entre sí y un radio de deambulación chico, la IA casi nunca se
//    cruzaba con nadie y parecía "no atacar nunca".
//
// NOTA: depende de Mapa.UnidadesEnRadio(fila, columna, radio).
public class ControladorIA
{
    private readonly Jugador miJugador;
    private readonly string civilizacion;
    private readonly Mapa mapa;
    private readonly Partida partida;
    private readonly GestorEntrenamiento gestorEntrenamiento;
    private readonly (int fila, int columna) posicionBase;

    // Se asigna despues de construir este ControladorIA (ver ControladorPartida),
    // porque el ControladorMapa de este mismo jugador se crea un paso despues.
    public ControladorMapa MiControladorMapa { get; set; }

    private const int RADIO_DETECCION = 3;           // detección "de reacción" al terminar de moverse
    private const int RADIO_BUSQUEDA_ATAQUE = 12;     // detección "activa": busca enemigos más lejos y va a buscarlos
    private const int UMBRAL_ENEMIGOS_PARA_HABILIDAD = 1; // antes 3: casi nunca se juntaban tantos, y el héroe jamás la usaba
    private const int ALDEANOS_MINIMOS_ANTES_DE_CUARTEL = 2; // no gastar en Cuartel antes de tener con qué recolectar
    private const float PROPORCION_ALDEANOS = 0.5f;   // la mitad de la población objetivo son aldeanos, el resto ejército
    private const int MARGEN_ANTES_DE_CASA = 2;       // construye una Casa cuando falten <= 2 espacios de población

    private const int CASA_COSTO_ORO = 0, CASA_COSTO_MADERA = 30, CASA_COSTO_COMIDA = 0;

    // Evita reintentar una construcción en CADA frame mientras la anterior
    // sigue en curso: GestorConstruccion tarda 5s en aplicarla, y durante
    // ese tiempo el edificio todavía NO aparece en Jugador.Edificios — sin
    // este flag, EvaluarEconomia volvía a llamar a ConstruirCuartel/Casa
    // decenas de veces por segundo mientras esperaba (y a veces terminaba
    // levantando dos edificios de golpe si el oro alcanzaba para ambos).
    private bool cuartelSolicitado = false;
    private bool casaSolicitada = false;

    // Throttle de la búsqueda activa de enemigos: no hace falta escanear
    // el radio de cada unidad 60 veces por segundo — con 1 vez cada 1.5s
    // alcanza y sobra, y es mucho más barato.
    private DateTime ultimaBusquedaCombate = DateTime.MinValue;
    private static readonly TimeSpan INTERVALO_BUSQUEDA_COMBATE = TimeSpan.FromSeconds(1.5);

    public ControladorIA(Jugador miJugador, string civilizacion, Mapa mapa, Partida partida, GestorEntrenamiento gestorEntrenamiento, (int fila, int columna) posicionBase)
    {
        this.miJugador = miJugador;
        this.civilizacion = civilizacion;
        this.mapa = mapa;
        this.partida = partida;
        this.posicionBase = posicionBase;
        this.gestorEntrenamiento = gestorEntrenamiento;
    }

    // Se llama una vez por cada unidad nueva de esta IA (al entrenarla o
    // colocarla), para que la IA empiece a "escuchar" lo que le pasa.
    public void ObservarUnidad(Unidad unidad)
    {
        unidad.FueAtacada += AlSerAtacada;
        unidad.Muerte += AlMorirUnidad;
    }

    // Se llama una vez por cada edificio nuevo de esta IA.
    public void ObservarEdificio(Edificio edificio)
    {
        edificio.FueAtacado += AlSerAtacadoEdificio;

        // El edificio ya quedó colocado de verdad: si era el que
        // estábamos esperando, liberamos el flag. Para Cuartel no hace
        // falta pedir un segundo, pero no hace daño liberarlo igual; para
        // Casa SÍ importa, porque la IA puede (y debe) pedir varias a lo
        // largo de la partida a medida que la población vuelve a acercarse
        // al límite.
        if (edificio is EdificioEntrenamiento) cuartelSolicitado = false;
        if (edificio is Casa) casaSolicitada = false;
    }

    // Lo llama el Controlador de movimiento de la IA cada vez que ella
    // misma termina de mover una unidad — así revisa si quedó cerca de
    // un enemigo y decide si conviene atacar (o usar la habilidad).
    public void AlTerminarMovimiento(Unidad unidadMovida, int fila, int columna)
    {
        if (unidadMovida.Vida <= 0) return;

        var enemigosCercanos = BuscarUnidadesEnemigasCerca(fila, columna, RADIO_DETECCION);
        if (enemigosCercanos.Count == 0) return;

        if (IntentarUsarHabilidad(unidadMovida, fila, columna, enemigosCercanos)) return;

        var objetivo = EvaluadorObjetivos.ElegirObjetivo(enemigosCercanos);
        if (objetivo != null) partida.EjecutarAtaque(miJugador, unidadMovida, objetivo);
    }

    // Reacciona de inmediato cuando atacan a una de sus unidades: si sigue
    // viva, contraataca a quien la golpeó.
    private void AlSerAtacada(Unidad victima, Unidad atacante)
    {
        if (victima.Vida <= 0) return;
        partida.EjecutarAtaque(miJugador, victima, atacante);
    }

    // Si atacan un edificio y no queda ninguna unidad viva cerca para
    // defenderlo, entrena un defensor de emergencia (si el oro alcanza).
    private void AlSerAtacadoEdificio(Edificio edificio, float daño)
    {
        if (edificio.Vida <= 0) return;
        bool tieneDefensores = miJugador.Unidades.Any(u => u.Vida > 0);
        if (!tieneDefensores)
        {
            gestorEntrenamiento.EntrenarUnidad(miJugador, "Vanguard", civilizacion);
        }
    }

    private void AlMorirUnidad(Unidad unidad)
    {
        unidad.Muerte -= AlMorirUnidad; // se desuscribe: ya no queda nada que escuchar en esta unidad
    }

    private List<Unidad> BuscarUnidadesEnemigasCerca(int fila, int columna, int radio)
    {
        var posiciones = mapa.UnidadesEnRadio(fila, columna, radio);
        var enemigos = new List<Unidad>();
        foreach (var (f, c) in posiciones)
        {
            Celda celda = mapa.ObtenerCelda(f, c);
            if (celda?.Unidad != null && !miJugador.Unidades.Contains(celda.Unidad) && celda.Unidad.Vida > 0)
                enemigos.Add(celda.Unidad);
        }
        return enemigos;
    }

    // ---------------------------------------------------------------
    // Búsqueda ACTIVA de combate. Antes la IA solo atacaba si la
    // atacaban primero o si una unidad terminaba de deambular justo al
    // lado de un enemigo — con bases lejos entre sí y un radio de
    // deambulación de apenas 2 celdas, eso casi nunca pasaba. Esto se
    // llama cada 1.5s (con throttle propio, así que se puede invocar
    // desde el Update de cada frame sin culpa) y, para cada unidad viva,
    // busca enemigos en un radio bastante más grande: si encuentra uno,
    // ataca (si ya está en rango) o se mueve directo a una celda dentro
    // de su rango de ataque. El mismo SolicitarMovimiento que usa el
    // resto del juego no exige que el destino sea adyacente (ya funciona
    // así para el jugador humano — un click en cualquier celda libre
    // mueve ahí en ~1s), así que no hace falta simular el camino paso a
    // paso.
    // ---------------------------------------------------------------

    public void ActualizarCombate()
    {
        if (DateTime.UtcNow - ultimaBusquedaCombate < INTERVALO_BUSQUEDA_COMBATE) return;
        ultimaBusquedaCombate = DateTime.UtcNow;

        if (MiControladorMapa == null) return; // todavía no se terminó de armar este jugador

        foreach (var unidad in miJugador.Unidades.ToList())
        {
            if (unidad.Vida <= 0 || unidad.Fila < 0) continue;

            var enemigosCercanos = BuscarUnidadesEnemigasCerca(unidad.Fila, unidad.Columna, RADIO_BUSQUEDA_ATAQUE);
            if (enemigosCercanos.Count == 0) continue;

            if (IntentarUsarHabilidad(unidad, unidad.Fila, unidad.Columna, enemigosCercanos)) continue;

            var objetivo = EvaluadorObjetivos.ElegirObjetivo(enemigosCercanos);
            if (objetivo == null) continue;

            int distancia = ResolutorArea.Distancia(unidad.Fila, unidad.Columna, objetivo.Fila, objetivo.Columna);
            if (distancia <= unidad.Rango)
            {
                partida.EjecutarAtaque(miJugador, unidad, objetivo);
            }
            else if (BuscarCeldaEnRangoDeAtaque(unidad, objetivo, out int fila, out int columna))
            {
                MiControladorMapa.SolicitarMovimiento(unidad.Fila, unidad.Columna, fila, columna);
            }
        }
    }

    // Si "unidad" es un Héroe con la habilidad lista y algún enemigo
    // cercano cae dentro de su alcance, la usa y devuelve true. Devuelve
    // false si no aplica (no es héroe, está en recarga, o nada al
    // alcance) para que quien llama siga con un ataque normal.
    //
    // Antes esto centraba el área SOBRE LA PROPIA POSICIÓN del héroe
    // (origen == objetivo): para las habilidades en Línea (Gilgamesh,
    // Godzilla) eso da una línea de longitud cero, que nunca encuentra
    // nada — el héroe de la IA jamás podía usarla. Ahora se apunta de
    // verdad hacia la posición de cada enemigo cercano.
    private bool IntentarUsarHabilidad(Unidad unidad, int fila, int columna, List<Unidad> enemigosCercanos)
    {
        if (!(unidad is Heroe heroe) || !heroe.PuedeUsarHabilidad()) return false;

        foreach (var enemigo in enemigosCercanos.OrderByDescending(e => e.Vida))
        {
            int distanciaAlPunto = ResolutorArea.Distancia(fila, columna, enemigo.Fila, enemigo.Columna);
            if (!DentroDelAlcanceDeHabilidad(heroe, distanciaAlPunto)) continue;

            var objetivosEnArea = ResolutorArea
                .ObtenerCeldasEnArea(mapa, heroe.AreaHabilidad, fila, columna, enemigo.Fila, enemigo.Columna)
                .Where(celda => celda.Unidad != null && !miJugador.Unidades.Contains(celda.Unidad) && celda.Unidad.Vida > 0)
                .Select(celda => celda.Unidad)
                .ToList();

            // UMBRAL_ENEMIGOS_PARA_HABILIDAD ahora es 1: con un cooldown de
            // 15s no hace falta esperar a un grupo de 3+ para que valga la
            // pena (eso era tan estricto que en la práctica nunca pasaba).
            if (objetivosEnArea.Count < UMBRAL_ENEMIGOS_PARA_HABILIDAD) continue;

            partida.EjecutarHabilidadEspecial(miJugador, heroe, objetivosEnArea);
            return true;
        }

        return false;
    }

    // Las habilidades en Línea (Gilgamesh, Godzilla) salen DESDE el propio
    // héroe hacia el punto elegido — lo que importa ahí es que el objetivo
    // no esté más lejos que el LARGO de la línea (Tamaño), no su
    // "RangoLanzamiento" (que para estas es 0 a propósito, ver el
    // comentario en FormaArea.cs: "irrelevante en línea, que sale de sí
    // mismo"). Las de Círculo (Jormungandr, Medusa) sí usan
    // RangoLanzamiento: es literalmente cuán lejos de sí mismo pueden
    // plantar el centro del área.
    private bool DentroDelAlcanceDeHabilidad(Heroe heroe, int distanciaAlPunto)
    {
        float alcance = heroe.AreaHabilidad.Forma == FormaArea.Linea
            ? heroe.AreaHabilidad.Tamaño
            : heroe.AreaHabilidad.RangoLanzamiento;
        return distanciaAlPunto <= alcance;
    }

    // Busca, entre las celdas libres alrededor del objetivo a una
    // distancia <= Rango, la más cercana a la posición actual de la
    // unidad — ahí es donde se mueve para poder atacar apenas termine el
    // desplazamiento (ver AlTerminarMovimiento).
    private bool BuscarCeldaEnRangoDeAtaque(Unidad unidad, Unidad objetivo, out int fila, out int columna)
    {
        int rango = Math.Max(1, unidad.Rango);
        (int fila, int columna)? mejor = null;
        int mejorDistancia = int.MaxValue;

        for (int df = -rango; df <= rango; df++)
        {
            for (int dc = -rango; dc <= rango; dc++)
            {
                if (Math.Max(Math.Abs(df), Math.Abs(dc)) > rango) continue;

                int f = objetivo.Fila + df;
                int c = objetivo.Columna + dc;
                if (!mapa.EsPosicionValida(f, c) || !mapa.CeldaLibre(f, c)) continue;

                int distanciaAUnidad = DistanciaChebyshev(f, c, unidad.Fila, unidad.Columna);
                if (distanciaAUnidad < mejorDistancia)
                {
                    mejorDistancia = distanciaAUnidad;
                    mejor = (f, c);
                }
            }
        }

        if (mejor == null) { fila = columna = 0; return false; }
        (fila, columna) = mejor.Value;
        return true;
    }

    // ---------------------------------------------------------------
    // Economía: aldeanos, Cuartel, unidades militares y (nuevo) Casas.
    // ---------------------------------------------------------------
    //
    // Prioridad simple, a propósito (no busca ser óptima, busca ser
    // razonable y vencible):
    //   1) no quedarse sin aldeanos (economía primero)
    //   2) construir el Cuartel apenas se pueda
    //   3) no chocar contra el límite de población: construir Casas
    //   4) si la economía y la vivienda están cubiertas, mantener ejército
    // Solo toma UNA decisión por llamada, para no gastar todos los
    // recursos de golpe en una sola evaluación.
    public void EvaluarEconomia()
    {
        AsignarTrabajoAldeanosLibres();

        var centroUrbano = miJugador.Edificios.OfType<EdificioPrincipal>().FirstOrDefault(e => e.EstaConstruido);
        var cuartel = miJugador.Edificios.OfType<EdificioEntrenamiento>().FirstOrDefault(e => e.EstaConstruido);

        int aldeanosVivos = miJugador.Aldeanos.Count;
        int unidadesVivas = miJugador.Unidades.Count(u => u.Vida > 0);

        // El objetivo de aldeanos/tropas ya no es un número fijo: crece
        // con cada Casa que se construye, para que la economía siga
        // expandiéndose en vez de quedarse pegada en 5 aldeanos + 5
        // unidades para siempre (que era, en la práctica, "la IA solo
        // construye una vez: el Cuartel, y nunca nada más").
        int poblacionObjetivo = Math.Max(ALDEANOS_MINIMOS_ANTES_DE_CUARTEL, (int)(miJugador.LimitePoblacion * PROPORCION_ALDEANOS));

        if (centroUrbano != null && aldeanosVivos < poblacionObjetivo)
        {
            centroUrbano.ProducirAldeano(miJugador, gestorEntrenamiento);
            return;
        }

        if (cuartel == null)
        {
            if (!cuartelSolicitado && aldeanosVivos >= ALDEANOS_MINIMOS_ANTES_DE_CUARTEL)
                ConstruirCuartel();
            return;
        }

        // Si la población está por chocar con el límite y no hay ya una
        // Casa en camino, construye una — sin esto la IA se quedaba
        // estancada apenas llegaba a 10 de población, sin importar cuántos
        // recursos le sobraran.
        if (!casaSolicitada && miJugador.PoblacionActual >= miJugador.LimitePoblacion - MARGEN_ANTES_DE_CASA)
        {
            ConstruirCasa();
            return;
        }

        if (cuartel.UnidadesDisponibles.Count > 0 && unidadesVivas < poblacionObjetivo)
        {
            string tipo = cuartel.UnidadesDisponibles[Aleatorio.Entero(0, cuartel.UnidadesDisponibles.Count)];
            cuartel.ProducirUnidad(miJugador, gestorEntrenamiento, tipo);
        }
    }

    // Manda a recolectar a todo Aldeano que no esté ya ocupado.
    private void AsignarTrabajoAldeanosLibres()
    {
        if (MiControladorMapa == null) return; // todavía no se terminó de armar este jugador

        foreach (var aldeano in miJugador.Aldeanos)
        {
            if (aldeano.Ocupado) continue;

            if (BuscarRecursoParaRecolectar(out int fila, out int columna))
                MiControladorMapa.SolicitarRecoleccion(aldeano, fila, columna);
        }
    }

    // Prioriza el tipo de recurso del que menos stock tiene el jugador; si
    // ya no queda ninguno de ese tipo en el mapa, junta cualquier otro.
    private bool BuscarRecursoParaRecolectar(out int fila, out int columna)
    {
        TipoRecurso tipoPreferido = miJugador.Recursos
            .OrderBy(kvp => kvp.Value)
            .First().Key;

        var disponibles = mapa.BuscarRecursosDisponibles();
        if (disponibles.Count == 0) { fila = columna = 0; return false; }

        var elegido = disponibles
            .Select(pos => (pos.fila, pos.columna, tipo: mapa.ObtenerCelda(pos.fila, pos.columna).Recurso.Tipo))
            .OrderByDescending(r => r.tipo == tipoPreferido)
            .ThenBy(r => DistanciaChebyshev(r.fila, r.columna, posicionBase.fila, posicionBase.columna))
            .FirstOrDefault();

        fila = elegido.fila;
        columna = elegido.columna;
        return true;
    }

    // Construye el Cuartel (EdificioEntrenamiento) de esta civilización en
    // una celda libre cerca del Centro Urbano.
    private void ConstruirCuartel()
    {
        if (!BuscarCeldaLibreCerca(posicionBase.fila, posicionBase.columna, out int fila, out int columna)) return;

        var unidadesDisponibles = UnidadesDeCivilizacion(civilizacion);
        var cuartel = new EdificioEntrenamiento(civilizacion, costoOro: 150, costoMadera: 100, costoComida: 0, unidadesDisponibles);

        cuartelSolicitado = true;
        if (!MiControladorMapa.SolicitarConstruccion(fila, columna, cuartel))
            cuartelSolicitado = false; // no se pudo (sin recursos, o celda ocupada): reintentar en la próxima llamada
    }

    // Construye una Casa cerca del Centro Urbano para subir el límite de
    // población (mismo costo que usa VistaHUD para el jugador humano).
    private void ConstruirCasa()
    {
        if (!BuscarCeldaLibreCerca(posicionBase.fila, posicionBase.columna, out int fila, out int columna)) return;

        var casa = new Casa(civilizacion, CASA_COSTO_ORO, CASA_COSTO_MADERA, CASA_COSTO_COMIDA);

        casaSolicitada = true;
        if (!MiControladorMapa.SolicitarConstruccion(fila, columna, casa))
            casaSolicitada = false;
    }

    private bool BuscarCeldaLibreCerca(int filaBase, int columnaBase, out int fila, out int columna)
    {
        (int deltaFila, int deltaColumna)[] posicionesRelativas =
        {
            (2, 2), (2, -2), (-2, 2), (-2, -2), (0, 3), (3, 0), (0, -3), (-3, 0),
            (4, 4), (4, -4), (-4, 4), (-4, -4), (0, 5), (5, 0), (0, -5), (-5, 0),
        };

        foreach (var (deltaFila, deltaColumna) in posicionesRelativas)
        {
            int f = filaBase + deltaFila;
            int c = columnaBase + deltaColumna;
            if (mapa.EsPosicionValida(f, c) && mapa.CeldaLibre(f, c)) { fila = f; columna = c; return true; }
        }

        fila = columna = 0;
        return false;
    }

    private static int DistanciaChebyshev(int f1, int c1, int f2, int c2)
        => Math.Max(Math.Abs(f1 - f2), Math.Abs(c1 - c2));

    private List<string> UnidadesDeCivilizacion(string civilizacion)
    {
        string heroe, exclusiva;
        switch (civilizacion)
        {
            case "Sumerios": heroe = "Gilgamesh";    exclusiva = "Caster";    break;
            case "Nipones":  heroe = "Godzilla";     exclusiva = "Assassin";  break;
            case "Griegos":  heroe = "Medusa";       exclusiva = "Avenger";   break;
            default:         heroe = "Jormungandr";  exclusiva = "Berserker"; break; // Vikingos
        }

        return new List<string> { heroe, exclusiva, "Defender", "Vanguard", "Ranger", "Healer", "NecoArc" };
    }
}