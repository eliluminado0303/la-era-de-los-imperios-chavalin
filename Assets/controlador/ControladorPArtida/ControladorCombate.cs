using System;
using System.Collections.Generic;
using System.Linq;

// Qué pasó con una orden de atacar hecha desde el mapa.
public enum ResultadoOrdenAtaque
{
    Invalida,     // objetivo aliado / inexistente / no se puede
    Atacando,     // estaba en rango: el golpe ya se ejecutó
    Acercandose   // estaba lejos: la unidad camina hacia el objetivo y ataca al llegar
}

// Recibe las acciones de combate que el jugador humano dispara desde la
// interfaz (clic en una unidad propia + clic en un punto objetivo), valida
// que sean posibles, y solo entonces las traslada al Modelo (Partida).
// La geometría de áreas vive en ResolutorArea (compartida con ControladorIA).
public class ControladorCombate
{
    private readonly Jugador miJugador;
    private readonly Mapa mapa;
    private readonly Partida partida;
    private readonly ControladorMapa controladorMapa; // solo el humano lo necesita (para acercarse al objetivo); null en las IA

    public ControladorCombate(Jugador miJugador, Mapa mapa, Partida partida, ControladorMapa controladorMapa = null)
    {
        this.miJugador = miJugador;
        this.mapa = mapa;
        this.partida = partida;
        this.controladorMapa = controladorMapa;
    }

    // ---------------------------------------------------------------
    // Acercarse y atacar: si el objetivo está fuera del alcance, la unidad
    // camina hasta una celda desde la que pueda pegarle y ataca al llegar
    // (un golpe, igual que un clic de ataque normal). Si el objetivo se
    // mueve mientras tanto, se reintenta el acercamiento unas pocas veces.
    // ---------------------------------------------------------------

    private class OrdenPersecucion
    {
        public Unidad atacante;
        public Unidad objetivoUnidad;
        public Edificio objetivoEdificio;
        public Aldeano objetivoAldeano;
        public float espera;   // lo que falta para que el desplazamiento (~1s) ya se haya aplicado
        public int reintentos;
    }

    private readonly List<OrdenPersecucion> persecuciones = new List<OrdenPersecucion>();
    private const float ESPERA_DESPLAZAMIENTO_SEGUNDOS = 1.5f; // GestorMovimiento tarda 1s; se deja margen
    private const int MAX_REINTENTOS = 5;

    // La Vista se suscribe para dibujar el deslizamiento de la unidad
    // (mismo papel que IniciarMovimientoVisual cuando el jugador la mueve).
    public event Action<Unidad, int, int, int, int> UnidadSeAcerca;

    // Se dispara cuando el ataque "diferido" (el de después de caminar) se
    // ejecutó: (atacante, fila y columna del objetivo). La Vista lo usa
    // para el proyectil de las unidades a distancia.
    public event Action<Unidad, int, int> AtaqueDiferidoRealizado;

    public bool TienePersecucion(Unidad unidad) => persecuciones.Exists(p => p.atacante == unidad);

    public void CancelarPersecucion(Unidad unidad)
    {
        if (unidad != null) persecuciones.RemoveAll(p => p.atacante == unidad);
    }

    public ResultadoOrdenAtaque SolicitarAtaqueOAcercarse(int filaOrigen, int columnaOrigen, int filaObjetivo, int columnaObjetivo)
    {
        Celda origen = mapa.ObtenerCelda(filaOrigen, columnaOrigen);
        Celda destino = mapa.ObtenerCelda(filaObjetivo, columnaObjetivo);
        if (origen?.Unidad == null || destino == null) return ResultadoOrdenAtaque.Invalida;

        Unidad atacante = origen.Unidad;
        if (!miJugador.Unidades.Contains(atacante)) return ResultadoOrdenAtaque.Invalida;
        if (!ObtenerObjetivoEnemigo(destino, out var unidadObj, out var edificioObj, out var aldeanoObj)) return ResultadoOrdenAtaque.Invalida;

        // En rango: ataque directo, como siempre.
        if (SolicitarAtaque(filaOrigen, columnaOrigen, filaObjetivo, columnaObjetivo)) return ResultadoOrdenAtaque.Atacando;

        if (controladorMapa == null || atacante.EstaAturdido) return ResultadoOrdenAtaque.Invalida;

        CancelarPersecucion(atacante);
        var orden = new OrdenPersecucion { atacante = atacante, objetivoUnidad = unidadObj, objetivoEdificio = edificioObj, objetivoAldeano = aldeanoObj };
        if (!Acercarse(orden)) return ResultadoOrdenAtaque.Invalida;

        persecuciones.Add(orden);
        return ResultadoOrdenAtaque.Acercandose;
    }

    // Llamar una vez por frame (lo hace ControladorPartida.Actualizar).
    public void Actualizar(float deltaTime)
    {
        if (persecuciones.Count == 0) return;

        foreach (var orden in persecuciones.ToList())
        {
            if (!miJugador.Unidades.Contains(orden.atacante) || orden.atacante.Vida <= 0 || ObjetivoMuerto(orden))
            {
                persecuciones.Remove(orden);
                continue;
            }

            orden.espera -= deltaTime;
            if (orden.espera > 0) continue;

            ObtenerPosicionObjetivo(orden, out int filaObj, out int columnaObj);
            int distancia = ResolutorArea.Distancia(orden.atacante.Fila, orden.atacante.Columna, filaObj, columnaObj);

            if (distancia <= orden.atacante.Rango)
            {
                EjecutarAtaqueDiferido(orden, filaObj, columnaObj);
                persecuciones.Remove(orden);
            }
            else if (++orden.reintentos > MAX_REINTENTOS || !Acercarse(orden))
            {
                persecuciones.Remove(orden); // el objetivo se movió demasiado o no hay lugar para pararse
            }
        }
    }

    private bool Acercarse(OrdenPersecucion orden)
    {
        if (ObjetivoMuerto(orden)) return false;
        Unidad atacante = orden.atacante;
        if (atacante.Fila < 0 || atacante.Columna < 0) return false;

        ObtenerPosicionObjetivo(orden, out int filaObj, out int columnaObj);
        if (filaObj < 0 || columnaObj < 0) return false;

        if (!mapa.BuscarCeldaLibreEnRango(filaObj, columnaObj, atacante.Rango, atacante.Fila, atacante.Columna, out int filaDestino, out int columnaDestino))
            return false;

        int filaOrigen = atacante.Fila, columnaOrigen = atacante.Columna;
        // Si la orden de mover se rechazó (ya viaja, o el destino está reservado),
        // no se avisa a la Vista: si no, dibujaría un "doble" deslizándose de una
        // unidad que en realidad no se movió.
        if (!controladorMapa.SolicitarMovimiento(filaOrigen, columnaOrigen, filaDestino, columnaDestino)) return false;
        UnidadSeAcerca?.Invoke(atacante, filaOrigen, columnaOrigen, filaDestino, columnaDestino);
        orden.espera = ESPERA_DESPLAZAMIENTO_SEGUNDOS;
        return true;
    }

    private void EjecutarAtaqueDiferido(OrdenPersecucion orden, int filaObj, int columnaObj)
    {
        if (orden.objetivoUnidad != null) partida.EjecutarAtaque(miJugador, orden.atacante, orden.objetivoUnidad);
        else if (orden.objetivoEdificio != null) partida.EjecutarAtaqueAEdificio(miJugador, orden.atacante, orden.objetivoEdificio);
        else if (orden.objetivoAldeano != null) partida.EjecutarAtaqueAAldeano(miJugador, orden.atacante, orden.objetivoAldeano);
        AtaqueDiferidoRealizado?.Invoke(orden.atacante, filaObj, columnaObj);
    }

    private static bool ObjetivoMuerto(OrdenPersecucion orden)
    {
        if (orden.objetivoUnidad != null) return orden.objetivoUnidad.Vida <= 0;
        if (orden.objetivoEdificio != null) return orden.objetivoEdificio.Vida <= 0;
        if (orden.objetivoAldeano != null) return !orden.objetivoAldeano.EstaVivo;
        return true;
    }

    private static void ObtenerPosicionObjetivo(OrdenPersecucion orden, out int fila, out int columna)
    {
        if (orden.objetivoUnidad != null) { fila = orden.objetivoUnidad.Fila; columna = orden.objetivoUnidad.Columna; }
        else if (orden.objetivoEdificio != null) { fila = orden.objetivoEdificio.Fila; columna = orden.objetivoEdificio.Columna; }
        else if (orden.objetivoAldeano != null) { fila = orden.objetivoAldeano.Fila; columna = orden.objetivoAldeano.Columna; }
        else { fila = columna = -1; }
    }

    // Mismo orden de prioridad que SolicitarAtaque: unidad > edificio > aldeano.
    private bool ObtenerObjetivoEnemigo(Celda destino, out Unidad unidad, out Edificio edificio, out Aldeano aldeano)
    {
        unidad = null; edificio = null; aldeano = null;

        if (destino.Unidad != null)
        {
            if (miJugador.Unidades.Contains(destino.Unidad) || destino.Unidad.Vida <= 0) return false;
            unidad = destino.Unidad;
            return true;
        }
        if (destino.Edificio != null)
        {
            if (miJugador.Edificios.Contains(destino.Edificio) || destino.Edificio.Vida <= 0) return false;
            edificio = destino.Edificio;
            return true;
        }
        if (destino.Aldeano != null && destino.Aldeano.EstaVivo)
        {
            if (miJugador.Aldeanos.Contains(destino.Aldeano)) return false;
            aldeano = destino.Aldeano;
            return true;
        }
        return false;
    }

    public bool SolicitarAtaque(int filaOrigen, int columnaOrigen, int filaObjetivo, int columnaObjetivo)
    {
        Celda origen = mapa.ObtenerCelda(filaOrigen, columnaOrigen);
        Celda destino = mapa.ObtenerCelda(filaObjetivo, columnaObjetivo);
        if (origen?.Unidad == null || destino == null) return false;

        Unidad atacante = origen.Unidad;
        if (!miJugador.Unidades.Contains(atacante)) return false;

        int distancia = ResolutorArea.Distancia(filaOrigen, columnaOrigen, filaObjetivo, columnaObjetivo);
        if (distancia > atacante.Rango) return false;

        if (destino.Unidad != null)
        {
            if (miJugador.Unidades.Contains(destino.Unidad)) return false;
            partida.EjecutarAtaque(miJugador, atacante, destino.Unidad);
            return true;
        }

        if (destino.Edificio != null)
        {
            if (miJugador.Edificios.Contains(destino.Edificio)) return false;
            partida.EjecutarAtaqueAEdificio(miJugador, atacante, destino.Edificio);
            return true;
        }

        // Un aldeano enemigo también puede ser atacado (y morir). Va al
        // final: si comparte celda con una unidad o un edificio, esos
        // tienen prioridad como objetivo.
        if (destino.Aldeano != null && destino.Aldeano.EstaVivo)
        {
            if (miJugador.Aldeanos.Contains(destino.Aldeano)) return false;
            partida.EjecutarAtaqueAAldeano(miJugador, atacante, destino.Aldeano);
            return true;
        }

        return false;
    }

    public bool SolicitarAtaqueEnArea(int filaOrigen, int columnaOrigen, int filaObjetivo, int columnaObjetivo)
    {
        Celda origen = mapa.ObtenerCelda(filaOrigen, columnaOrigen);
        if (origen?.Unidad == null || !miJugador.Unidades.Contains(origen.Unidad)) return false;

        Unidad atacante = origen.Unidad;
        if (atacante.AreaAtaqueBasico == null) return false;

        int distanciaLanzamiento = ResolutorArea.Distancia(filaOrigen, columnaOrigen, filaObjetivo, columnaObjetivo);
        if (distanciaLanzamiento > atacante.Rango) return false;

        var objetivos = ResolutorArea.ObtenerCeldasEnArea(mapa, atacante.AreaAtaqueBasico, filaOrigen, columnaOrigen, filaObjetivo, columnaObjetivo)
            .Where(celda => celda.Unidad != null && !miJugador.Unidades.Contains(celda.Unidad))
            .Select(celda => celda.Unidad)
            .ToList();
        if (objetivos.Count == 0) return false;

        partida.EjecutarAtaqueEnArea(miJugador, atacante, objetivos);
        return true;
    }

    // Habilidad activa de cualquier unidad (héroe, Assassin, Defender...).
    // Las que se lanzan "sobre sí mismas" ignoran el punto clicado y se
    // centran en la propia unidad. Los parámetros conservan el nombre viejo
    // "heroe" por compatibilidad con quien ya lo llama.
    public bool SolicitarHabilidadEspecial(int filaHeroe, int columnaHeroe, int filaObjetivo, int columnaObjetivo)
    {
        Celda origen = mapa.ObtenerCelda(filaHeroe, columnaHeroe);
        Unidad unidad = origen?.Unidad;
        if (unidad == null || !miJugador.Unidades.Contains(unidad)) return false;
        if (!unidad.TieneHabilidadActiva || unidad.AreaHabilidad == null) return false;
        if (!unidad.PuedeUsarHabilidad()) return false; // en recarga: la Vista puede usar esto para deshabilitar el botón

        if (unidad.HabilidadEsSobreSiMismo)
        {
            filaObjetivo = filaHeroe;
            columnaObjetivo = columnaHeroe;
        }

        int distanciaLanzamiento = ResolutorArea.Distancia(filaHeroe, columnaHeroe, filaObjetivo, columnaObjetivo);

        // Para habilidades en Línea (Gilgamesh, Godzilla) RangoLanzamiento es 0
        // (la línea sale del propio héroe): el alcance real es el LARGO de la
        // línea (Tamaño). Antes solo se podían lanzar clickeando al propio héroe.
        float alcance = unidad.AreaHabilidad.Forma == FormaArea.Linea
            ? unidad.AreaHabilidad.Tamaño
            : unidad.AreaHabilidad.RangoLanzamiento;
        if (distanciaLanzamiento > alcance) return false;

        bool aliados = unidad.HabilidadAfectaAliados;
        var objetivos = ResolutorArea.ObtenerCeldasEnArea(mapa, unidad.AreaHabilidad, filaHeroe, columnaHeroe, filaObjetivo, columnaObjetivo)
            .Where(celda => celda.Unidad != null && celda.Unidad != unidad && celda.Unidad.Vida > 0
                            && (aliados ? miJugador.Unidades.Contains(celda.Unidad) : !miJugador.Unidades.Contains(celda.Unidad)))
            .Select(celda => celda.Unidad)
            .ToList();

        // Una habilidad de apoyo sobre sí mismo (Defender) sirve aunque esté solo;
        // las ofensivas necesitan al menos un enemigo en el área.
        bool sirveSinObjetivos = aliados && unidad.HabilidadEsSobreSiMismo;
        if (objetivos.Count == 0 && !sirveSinObjetivos) return false;

        partida.EjecutarHabilidadEspecial(miJugador, unidad, objetivos);
        return true;
    }

    public bool SolicitarCuracion(int filaHealer, int columnaHealer, int filaObjetivo, int columnaObjetivo, bool buff)
    {
        Celda origen = mapa.ObtenerCelda(filaHealer, columnaHealer);
        if (!(origen?.Unidad is Healer healer) || !miJugador.Unidades.Contains(healer)) return false;

        int distanciaLanzamiento = ResolutorArea.Distancia(filaHealer, columnaHealer, filaObjetivo, columnaObjetivo);
        if (distanciaLanzamiento > healer.AreaBuff.RangoLanzamiento) return false;
        if (buff && !healer.PuedeUsarHabilidad()) return false; // la bendición tiene recarga; curar no

        var aliados = ResolutorArea.ObtenerCeldasEnArea(mapa, healer.AreaBuff, filaHealer, columnaHealer, filaObjetivo, columnaObjetivo)
            .Where(celda => celda.Unidad != null && miJugador.Unidades.Contains(celda.Unidad))
            .Select(celda => celda.Unidad)
            .ToList();
        if (aliados.Count == 0) return false;

        foreach (var aliado in aliados)
        {
            if (buff) healer.Buffear(aliado);
            else healer.Curar(aliado);
        }
        if (buff) healer.RegistrarUsoHabilidad();
        return true;
    }
}