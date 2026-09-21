using System;

// Recibe las acciones de combate que el jugador humano dispara desde la
// interfaz (clic en una unidad propia + clic en un objetivo), valida que
// sean posibles, y solo entonces las traslada al Modelo (Partida).
// Por MVC: este controlador nunca toca sprites ni UI directamente, solo
// devuelve bool para que la Vista decida cómo mostrar éxito/error.
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

    // Intenta atacar desde una celda de origen hacia una celda objetivo.
    // Devuelve false (sin hacer nada) si la acción no es válida.
    public bool SolicitarAtaque(int filaOrigen, int columnaOrigen, int filaObjetivo, int columnaObjetivo)
    {
        Celda origen = mapa.ObtenerCelda(filaOrigen, columnaOrigen);
        Celda destino = mapa.ObtenerCelda(filaObjetivo, columnaObjetivo);
        if (origen?.Unidad == null || destino == null) return false;

        Unidad atacante = origen.Unidad;
        if (!miJugador.Unidades.Contains(atacante)) return false; // no controlas esa unidad

        int distancia = Distancia(filaOrigen, columnaOrigen, filaObjetivo, columnaObjetivo);
        if (distancia > atacante.Rango) return false; // fuera de rango: la Vista no debería ni dejar hacer clic, pero igual se valida aquí

        if (destino.Unidad != null)
        {
            if (miJugador.Unidades.Contains(destino.Unidad)) return false; // es aliado, no se puede
            partida.EjecutarAtaque(miJugador, atacante, destino.Unidad);
            return true;
        }

        if (destino.Edificio != null)
        {
            if (miJugador.Edificios.Contains(destino.Edificio)) return false; // es tuyo
            partida.EjecutarAtaqueAEdificio(miJugador, atacante, destino.Edificio);
            return true;
        }

        return false; // celda vacía: no hay nada que atacar
    }

    // Distancia tipo "tablero de ajedrez" (misma métrica que Mapa.UnidadesEnRadio,
    // para que rango de ataque y radio de detección de la IA sean consistentes).
    private int Distancia(int f1, int c1, int f2, int c2)
        => Math.Max(Math.Abs(f1 - f2), Math.Abs(c1 - c2));
}