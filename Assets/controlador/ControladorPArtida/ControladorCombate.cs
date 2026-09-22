using System.Collections.Generic;
using System.Linq;

// Recibe las acciones de combate que el jugador humano dispara desde la
// interfaz (clic en una unidad propia + clic en un punto objetivo), valida
// que sean posibles, y solo entonces las traslada al Modelo (Partida).
// La geometría de áreas vive en ResolutorArea (compartida con ControladorIA).
public class ControladorCombate
{
    private readonly Jugador miJugador;
    private readonly Mapa mapa;
    private readonly Partida partida;

    public ControladorCombate(Jugador miJugador, Mapa mapa, Partida partida)
    {
        this.miJugador = miJugador;
        this.mapa = mapa;
        this.partida = partida;
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

    public bool SolicitarHabilidadEspecial(int filaHeroe, int columnaHeroe, int filaObjetivo, int columnaObjetivo)
    {
        Celda origen = mapa.ObtenerCelda(filaHeroe, columnaHeroe);
        if (!(origen?.Unidad is Heroe heroe) || !miJugador.Unidades.Contains(heroe)) return false;
        if (!heroe.PuedeUsarHabilidad()) return false; // en recarga: la Vista puede usar esto para deshabilitar el botón

        int distanciaLanzamiento = ResolutorArea.Distancia(filaHeroe, columnaHeroe, filaObjetivo, columnaObjetivo);
        if (distanciaLanzamiento > heroe.AreaHabilidad.RangoLanzamiento) return false;

        var objetivos = ResolutorArea.ObtenerCeldasEnArea(mapa, heroe.AreaHabilidad, filaHeroe, columnaHeroe, filaObjetivo, columnaObjetivo)
            .Where(celda => celda.Unidad != null && !miJugador.Unidades.Contains(celda.Unidad))
            .Select(celda => celda.Unidad)
            .ToList();
        if (objetivos.Count == 0) return false;

        partida.EjecutarHabilidadEspecial(miJugador, heroe, objetivos);
        return true;
    }

    public bool SolicitarCuracion(int filaHealer, int columnaHealer, int filaObjetivo, int columnaObjetivo, bool buff)
    {
        Celda origen = mapa.ObtenerCelda(filaHealer, columnaHealer);
        if (!(origen?.Unidad is Healer healer) || !miJugador.Unidades.Contains(healer)) return false;

        int distanciaLanzamiento = ResolutorArea.Distancia(filaHealer, columnaHealer, filaObjetivo, columnaObjetivo);
        if (distanciaLanzamiento > healer.AreaBuff.RangoLanzamiento) return false;

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
        return true;
    }
}