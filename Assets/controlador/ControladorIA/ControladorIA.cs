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

   private const int RADIO_DETECCION = 3;
    private const int UMBRAL_ENEMIGOS_PARA_HABILIDAD = 3; // desde cuántos objetivos agrupados conviene gastar la recarga
    private const int ALDEANOS_OBJETIVO = 5; // suficientes para mantener el flujo de recursos sin exagerar

    public ControladorIA(Jugador miJugador, string civilizacion, Mapa mapa, Partida partida, GestorEntrenamiento gestorEntrenamiento)
    {
        this.miJugador = miJugador;
        this.civilizacion = civilizacion;
        this.mapa = mapa;
        this.partida = partida;
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
        partida.EjecutarAtaque(miJugador, victima, atacante);
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
    public void EvaluarEconomia()
    {
        var centroUrbano = miJugador.Edificios.OfType<EdificioPrincipal>().FirstOrDefault(e => e.EstaConstruido);
        var cuartel = miJugador.Edificios.OfType<EdificioEntrenamiento>().FirstOrDefault(e => e.EstaConstruido);

        int aldeanosVivos = miJugador.Aldeanos.Count;
        int unidadesVivas = miJugador.Unidades.Count(u => u.Vida > 0);

        if (centroUrbano != null && aldeanosVivos < ALDEANOS_OBJETIVO)
        {
            centroUrbano.ProducirAldeano(miJugador, gestorEntrenamiento);
            return;
        }

        if (cuartel != null && cuartel.UnidadesDisponibles.Count > 0 && unidadesVivas < aldeanosVivos)
        {
            string tipo = cuartel.UnidadesDisponibles[Aleatorio.Entero(0, cuartel.UnidadesDisponibles.Count)];
            cuartel.ProducirUnidad(miJugador, gestorEntrenamiento, tipo);
        }
    }
}