using System.Collections.Generic;
using System.Linq;
    
// Controla las decisiones de UNA civilización manejada por la IA (se crea
// una instancia por cada oponente IA — con 4 civilizaciones y 1 humano,
// habría 3 de estos activos a la vez).
//
// Es reactiva a propósito: no tiene un bucle propio que la haga "pensar"
// cada X segundos. En vez de eso se suscribe a los eventos de sus propias
// unidades y edificios (FueAtacada, FueAtacado, Muerte) y decide en el
// momento en que algo relevante ocurre. Esto evita crear un hilo/Task
// adicional solo para "vigilar" el estado del juego.
//
// NOTA: depende de Mapa.UnidadesEnRadio(fila, columna, radio), que es el
// método pendiente de agregar (Opción A que hablamos con tu compañera).
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
    // Es lo que permite que la IA use las mismas dos operaciones que usa el
    // jugador humano desde la Vista: SolicitarRecoleccion y SolicitarConstruccion.
    public ControladorMapa MiControladorMapa { get; set; }

   private const int RADIO_DETECCION = 3;
    private const int UMBRAL_ENEMIGOS_PARA_HABILIDAD = 3; // desde cuántos objetivos agrupados conviene gastar la recarga
    private const int ALDEANOS_OBJETIVO = 5; // suficientes para mantener el flujo de recursos sin exagerar
    private const int ALDEANOS_MINIMOS_ANTES_DE_CUARTEL = 2; // no gastar en Cuartel antes de tener con qué recolectar

    // --- Ofensiva militar: antes la IA entrenaba unidades y las dejaba
    // paradas/deambulando 2 casillas de la base para siempre. Esto hace que
    // cada tanto mande una oleada a buscar y atacar al enemigo más cercano.
    // Al llegar a este número de unidades vivas, la IA DEJA de entrenar y
    // construir y manda TODO el ejército a buscar pelea. Además de dar
    // partidas más movidas, corta el crecimiento sin fin de unidades que
    // trababa la PC. Si el ejército cae a UNIDADES_PARA_REAGRUPAR o menos,
    // vuelve a producir (si no, quedaría muerta para siempre).
    private const int LIMITE_EJERCITO = 10;
    private const int UNIDADES_PARA_REAGRUPAR = 2;
    private const int MINIMO_EJERCITO_PARA_ATACAR = 3;
    private const float INTERVALO_ECONOMIA = 1f;  // antes corría CADA frame: barría el mapa entero 60 veces por segundo por IA
    private const float INTERVALO_MILITAR = 1.5f;

    private bool enModoAtaque = false;
    public bool EnModoAtaque => enModoAtaque; // ControladorDeambulacion lo consulta para no mover al azar a las tropas que atacan
    private float tiempoHastaProximaEconomia = 0f;
    private float tiempoHastaProximaAccionMilitar = 0f;
    private static bool contraatacando = false; // evita el bucle infinito A contraataca a B, B contraataca a A...

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
    }

    // Lo llama el Controlador de movimiento de la IA cada vez que ella
    // misma termina de mover una unidad — así revisa si quedó cerca de
    // un enemigo y decide si conviene atacar.
    public void AlTerminarMovimiento(Unidad unidadMovida, int fila, int columna)
    {
        if (unidadMovida.Vida <= 0) return;

        var enemigosCercanos = BuscarUnidadesEnemigasCerca(fila, columna);
        if (enemigosCercanos.Count == 0) return;

        // Si quien se movió es un héroe con la habilidad lista, revisa si
        // conviene más usarla que dar un golpe normal: solo si hay varios
        // enemigos agrupados dentro de su propia área (si no, sería
        // desperdiciar 15s de recarga en un solo objetivo, y para eso ya
        // está el ataque normal, que no tiene costo).
        if (unidadMovida is Heroe heroe && heroe.PuedeUsarHabilidad())
        {
            var objetivosEnArea = ResolutorArea
                .ObtenerCeldasEnArea(mapa, heroe.AreaHabilidad, fila, columna, fila, columna)
                .Where(celda => celda.Unidad != null && !miJugador.Unidades.Contains(celda.Unidad))
                .Select(celda => celda.Unidad)
                .ToList();

            if (objetivosEnArea.Count >= UMBRAL_ENEMIGOS_PARA_HABILIDAD)
            {
                partida.EjecutarHabilidadEspecial(miJugador, heroe, objetivosEnArea);
                return;
            }
        }

        var objetivo = EvaluadorObjetivos.ElegirObjetivo(enemigosCercanos);
        if (objetivo != null) partida.EjecutarAtaque(miJugador, unidadMovida, objetivo);
    }

    // Reacciona de inmediato cuando atacan a una de sus unidades: si sigue
    // viva, contraataca a quien la golpeó. Así la IA nunca se queda "sin
    // hacer nada" mientras la golpean, sin necesitar un timer aparte.
    private void AlSerAtacada(Unidad victima, Unidad atacante)
    {
        if (victima.Vida <= 0) return; // ya murió, AlMorirUnidad se encarga de la limpieza

        // Sin este candado, dos unidades de IA se contraatacaban sin fin:
        // Atacar -> FueAtacada -> AlSerAtacada -> Atacar -> ... hasta el
        // StackOverflowException. Ahora solo responde el primer golpe.
        if (contraatacando) return;
        contraatacando = true;
        try { partida.EjecutarAtaque(miJugador, victima, atacante); }
        finally { contraatacando = false; }
    }

    // Si atacan un edificio y no queda ninguna unidad viva cerca para
    // defenderlo, entrena un defensor de emergencia (si el oro alcanza).
    // Es deliberadamente simple: la parte de "estrategia económica" completa
    // (cuándo construir, cuándo expandirse) queda para una segunda pasada.
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

    private List<Unidad> BuscarUnidadesEnemigasCerca(int fila, int columna)
    {
        var posiciones = mapa.UnidadesEnRadio(fila, columna, RADIO_DETECCION);
        var enemigos = new List<Unidad>();
        foreach (var (f, c) in posiciones)
        {
            Celda celda = mapa.ObtenerCelda(f, c);
            if (celda?.Unidad != null && !miJugador.Unidades.Contains(celda.Unidad))
                enemigos.Add(celda.Unidad);
        }
        return enemigos;
    }


    // Decisión económica: se llama cada vez que termina algo productivo
    // (un entrenamiento, una construcción) en vez de con un timer propio —
    // así sigue siendo reactiva, solo que ahora reacciona también al propio
    // progreso, no solo a ataques.
    //
    // Prioridad simple, a propósito (no busca ser óptima, busca ser
    // razonable y vencible):
    //   1) no quedarse sin aldeanos (economía primero)
    //   2) si la economía está cubierta, mantener algo de ejército
    // Solo toma UNA decisión por llamada, para no gastar todos los
    // recursos de golpe en una sola evaluación.
    public void EvaluarEconomia(float deltaTime = 0f)
    {
        tiempoHastaProximaEconomia -= deltaTime;
        if (tiempoHastaProximaEconomia > 0f) return;
        tiempoHastaProximaEconomia = INTERVALO_ECONOMIA;

        int vivas = miJugador.Unidades.Count(u => u.Vida > 0);
        // Sin Casas el tope de población es 10 (aldeanos + unidades), así que con
        // 5 aldeanos el ejército nunca llegaría a LIMITE_EJERCITO: por eso también
        // se ataca cuando la población está llena y hay un mínimo de tropas.
        bool poblacionLlena = miJugador.PoblacionActual >= miJugador.LimitePoblacion;
        if (!enModoAtaque && (vivas >= LIMITE_EJERCITO || (poblacionLlena && vivas >= MINIMO_EJERCITO_PARA_ATACAR))) enModoAtaque = true;
        if (enModoAtaque && vivas <= UNIDADES_PARA_REAGRUPAR) enModoAtaque = false;

        if (enModoAtaque)
        {
            AsignarTrabajoAldeanosLibres(); // los aldeanos siguen juntando, pero ya no se gasta en nada nuevo
            return;
        }

        // Antes de decidir qué entrenar o construir, siempre se revisa si
        // quedó algún Aldeano sin trabajo (recién entrenado, o porque se le
        // agotó el recurso que estaba juntando) — si no, se entrenaban
        // aldeanos "fantasma" que nunca recolectan nada.
        AsignarTrabajoAldeanosLibres();

        var centroUrbano = miJugador.Edificios.OfType<EdificioPrincipal>().FirstOrDefault(e => e.EstaConstruido);
        var cuartel = miJugador.Edificios.OfType<EdificioEntrenamiento>().FirstOrDefault(e => e.EstaConstruido);

        int aldeanosVivos = miJugador.Aldeanos.Count;
        int unidadesVivas = miJugador.Unidades.Count(u => u.Vida > 0);

        if (centroUrbano != null && aldeanosVivos < ALDEANOS_OBJETIVO)
        {
            centroUrbano.ProducirAldeano(miJugador, gestorEntrenamiento);
            return;
        }

        // Nadie construía el Cuartel de la IA: sin esto EvaluarEconomia
        // nunca podía entrenar una sola unidad militar, sin importar cuántos
        // recursos juntara.
        if (cuartel == null && aldeanosVivos >= ALDEANOS_MINIMOS_ANTES_DE_CUARTEL)
        {
            ConstruirCuartel();
            return;
        }

        if (cuartel != null && cuartel.UnidadesDisponibles.Count > 0 && unidadesVivas < aldeanosVivos)
        {
            string tipo = cuartel.UnidadesDisponibles[Aleatorio.Entero(0, cuartel.UnidadesDisponibles.Count)];
            cuartel.ProducirUnidad(miJugador, gestorEntrenamiento, tipo);
        }
    }

    // Manda a recolectar a todo Aldeano que no esté ya ocupado (recién
    // salido de entrenamiento, o libre porque el recurso que tenía
    // asignado se agotó). Cada vez elige de nuevo el tipo de recurso más
    // bajo entre los tres, así la economía de la IA no se desbalancea.
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
    // ya no queda ninguno de ese tipo en el mapa, junta cualquier otro para
    // no dejar al Aldeano sin hacer nada.
    private bool BuscarRecursoParaRecolectar(out int fila, out int columna)
    {
        TipoRecurso tipoPreferido = miJugador.Recursos
            .OrderBy(kvp => kvp.Value)
            .First().Key;

        var disponibles = mapa.BuscarRecursosDisponibles();
        if (disponibles.Count == 0) { fila = columna = 0; return false; }

        var elegido = disponibles
            .Select(pos => (pos.fila, pos.columna, tipo: mapa.ObtenerCelda(pos.fila, pos.columna).Recurso.Tipo))
            .OrderByDescending(r => r.tipo == tipoPreferido) // el tipo preferido primero, sin descartar el resto
            .ThenBy(r => DistanciaChebyshev(r.fila, r.columna, posicionBase.fila, posicionBase.columna))
            .FirstOrDefault();

        fila = elegido.fila;
        columna = elegido.columna;
        return true;
    }

    // Llamado una vez por frame (con deltaTime real). Solo actúa en modo
    // ataque (ver LIMITE_EJERCITO): cada INTERVALO_MILITAR segundos revisa
    // TODAS las unidades vivas. Cada una: si tiene un enemigo cerca lo
    // ataca (o se le acerca); si no, marcha hacia el edificio enemigo más
    // cercano y lo ataca. Se hace un solo barrido del mapa por tanda (antes
    // era uno por unidad).
    public void EvaluarMilitar(float deltaTime)
    {
        if (MiControladorMapa == null || !enModoAtaque) return;

        tiempoHastaProximaAccionMilitar -= deltaTime;
        if (tiempoHastaProximaAccionMilitar > 0f) return;
        tiempoHastaProximaAccionMilitar = INTERVALO_MILITAR;

        var posiciones = ObtenerPosicionesDeMisUnidades();
        foreach (var unidad in miJugador.Unidades.Where(u => u.Vida > 0).ToList())
        {
            if (unidad.Vida <= 0 || unidad.EstaAturdido) continue;
            if (!posiciones.TryGetValue(unidad, out var pos)) continue;
            ActuarEnCombate(unidad, pos.fila, pos.columna);
        }
    }

    private Dictionary<Unidad, (int fila, int columna)> ObtenerPosicionesDeMisUnidades()
    {
        var resultado = new Dictionary<Unidad, (int, int)>();
        var mias = new HashSet<Unidad>(miJugador.Unidades);
        for (int f = 0; f < Mapa.FILAS; f++)
            for (int c = 0; c < Mapa.COLUMNAS; c++)
            {
                var u = mapa.ObtenerCelda(f, c)?.Unidad;
                if (u != null && mias.Contains(u)) resultado[u] = (f, c);
            }
        return resultado;
    }

    private void ActuarEnCombate(Unidad unidad, int fila, int columna)
    {
        // 1) Enemigos cerca: pelear (habilidad si conviene, si no ataque; si está lejos para pegar, acercarse).
        var enemigos = BuscarUnidadesEnemigasCerca(fila, columna);
        if (enemigos.Count > 0)
        {
            var objetivo = EvaluadorObjetivos.ElegirObjetivo(enemigos);
            if (objetivo != null && BuscarPosicionEnMapa(objetivo, fila, columna, out int fo, out int co))
            {
                int distancia = ResolutorArea.Distancia(fila, columna, fo, co);
                if (unidad is Heroe heroe && heroe.PuedeUsarHabilidad() && IntentarHabilidad(heroe, fila, columna, fo, co)) return;

                if (distancia <= unidad.Rango) { AtacarUnidad(unidad, objetivo, fo, co); return; }
                AcercarseA(unidad, fila, columna, fo, co);
                return;
            }
        }

        // 2) Sin enemigos cerca: marchar al edificio enemigo más cercano y golpearlo.
        var edificio = BuscarEdificioEnemigoMasCercano(fila, columna);
        if (edificio == null) return;

        int d = ResolutorArea.Distancia(fila, columna, edificio.Fila, edificio.Columna);
        if (d <= unidad.Rango) partida.EjecutarAtaqueAEdificio(miJugador, unidad, edificio);
        else AcercarseA(unidad, fila, columna, edificio.Fila, edificio.Columna);
    }

    // Ataque básico; los de área (Godzilla, Jormungandr) golpean a todos los enemigos del círculo.
    private void AtacarUnidad(Unidad unidad, Unidad objetivo, int filaObjetivo, int columnaObjetivo)
    {
        if (unidad.AreaAtaqueBasico != null)
        {
            var objetivos = ResolutorArea
                .ObtenerCeldasEnArea(mapa, unidad.AreaAtaqueBasico, filaObjetivo, columnaObjetivo, filaObjetivo, columnaObjetivo)
                .Where(celda => celda.Unidad != null && !miJugador.Unidades.Contains(celda.Unidad))
                .Select(celda => celda.Unidad)
                .ToList();
            if (objetivos.Count > 0) { partida.EjecutarAtaqueEnArea(miJugador, unidad, objetivos); return; }
        }
        partida.EjecutarAtaque(miJugador, unidad, objetivo);
    }

    private bool IntentarHabilidad(Heroe heroe, int fila, int columna, int filaObjetivo, int columnaObjetivo)
    {
        var area = heroe.AreaHabilidad;
        bool esLinea = area.Forma == FormaArea.Linea;
        int distancia = ResolutorArea.Distancia(fila, columna, filaObjetivo, columnaObjetivo);
        if (!esLinea && distancia > area.RangoLanzamiento) return false;
        if (esLinea && distancia == 0) return false;

        var objetivos = ResolutorArea
            .ObtenerCeldasEnArea(mapa, area, fila, columna, filaObjetivo, columnaObjetivo)
            .Where(celda => celda.Unidad != null && !miJugador.Unidades.Contains(celda.Unidad))
            .Select(celda => celda.Unidad)
            .ToList();
        if (objetivos.Count < 2) return false; // con un solo enemigo se guarda la recarga

        partida.EjecutarHabilidadEspecial(miJugador, heroe, objetivos);
        return true;
    }

    // Va a una celda libre pegada al objetivo (la celda del objetivo está ocupada, no se puede entrar).
    private void AcercarseA(Unidad unidad, int fila, int columna, int filaObjetivo, int columnaObjetivo)
    {
        int mejorF = -1, mejorC = -1, mejorDist = int.MaxValue;
        for (int df = -1; df <= 1; df++)
            for (int dc = -1; dc <= 1; dc++)
            {
                if (df == 0 && dc == 0) continue;
                int f = filaObjetivo + df, c = columnaObjetivo + dc;
                if (!mapa.EsPosicionValida(f, c) || !mapa.CeldaLibre(f, c)) continue;
                int dist = ResolutorArea.Distancia(fila, columna, f, c);
                if (dist < mejorDist) { mejorDist = dist; mejorF = f; mejorC = c; }
            }
        if (mejorF < 0) return;
        MiControladorMapa.SolicitarMovimiento(fila, columna, mejorF, mejorC);
    }

    // Busca la unidad cerca de (fila, columna) en el radio de detección.
    private bool BuscarPosicionEnMapa(Unidad objetivo, int fila, int columna, out int f, out int c)
    {
        foreach (var (pf, pc) in mapa.UnidadesEnRadio(fila, columna, RADIO_DETECCION))
            if (mapa.ObtenerCelda(pf, pc)?.Unidad == objetivo) { f = pf; c = pc; return true; }
        f = c = 0;
        return false;
    }

    private Edificio BuscarEdificioEnemigoMasCercano(int filaDesde, int columnaDesde)
    {
        Edificio mejor = null;
        int mejorDistancia = int.MaxValue;
        foreach (var enemigo in OtrosJugadores())
            foreach (var edificio in enemigo.Edificios)
            {
                if (edificio.Vida <= 0 || edificio.Fila < 0) continue;
                int distancia = DistanciaChebyshev(edificio.Fila, edificio.Columna, filaDesde, columnaDesde);
                if (distancia < mejorDistancia) { mejorDistancia = distancia; mejor = edificio; }
            }
        return mejor;
    }

    private IEnumerable<Jugador> OtrosJugadores()
    {
        if (partida.JugadorHumano != miJugador) yield return partida.JugadorHumano;
        foreach (var oponente in partida.Oponentes)
            if (oponente != miJugador) yield return oponente;
    }

    // Construye el Cuartel (EdificioEntrenamiento) de esta civilización en
    // una celda libre cerca del Centro Urbano. Mismo costo y misma lista de
    // unidades que usa el jugador humano desde VistaHUD.ConstruirCuartel.
    private void ConstruirCuartel()
    {
        if (!BuscarCeldaLibreCerca(posicionBase.fila, posicionBase.columna, out int fila, out int columna)) return;

        var unidadesDisponibles = UnidadesDeCivilizacion(civilizacion);
        var cuartel = new EdificioEntrenamiento(civilizacion, costoOro: 150, costoMadera: 100, costoComida: 0, unidadesDisponibles);
        MiControladorMapa.SolicitarConstruccion(fila, columna, cuartel);
    }

    private bool BuscarCeldaLibreCerca(int filaBase, int columnaBase, out int fila, out int columna)
    {
        (int deltaFila, int deltaColumna)[] posicionesRelativas =
        {
            (2, 2), (2, -2), (-2, 2), (-2, -2), (0, 3), (3, 0), (0, -3), (-3, 0)
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
        => System.Math.Max(System.Math.Abs(f1 - f2), System.Math.Abs(c1 - c2));
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