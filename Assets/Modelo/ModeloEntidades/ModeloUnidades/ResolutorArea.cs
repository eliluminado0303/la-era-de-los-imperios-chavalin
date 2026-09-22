using System;
using System.Collections.Generic;

// Traduce un AreaEfecto (forma + tamaño) hacia una celda objetivo, en la
// lista real de celdas del Mapa que caen dentro. Compartido entre
// ControladorCombate (jugador humano) y ControladorIA, para no repetir
// la misma geometría dos veces.
public static class ResolutorArea
{
    public static List<Celda> ObtenerCeldasEnArea(Mapa mapa, AreaEfecto area, int filaOrigen, int columnaOrigen, int filaObjetivo, int columnaObjetivo)
    {
        var celdas = new List<Celda>();

        if (area.Forma == FormaArea.Circulo)
        {
            foreach (var (f, c) in mapa.UnidadesEnRadio(filaObjetivo, columnaObjetivo, (int)area.Tamaño))
            {
                Celda celda = mapa.ObtenerCelda(f, c);
                if (celda != null) celdas.Add(celda);
            }
            return celdas;
        }

        // Línea: se proyecta cada celda candidata sobre la dirección origen->objetivo.
        float dx = columnaObjetivo - columnaOrigen;
        float dy = filaObjetivo - filaOrigen;
        float longitudDireccion = (float)Math.Sqrt(dx * dx + dy * dy);
        if (longitudDireccion < 0.001f) return celdas; // origen y objetivo son la misma celda
        dx /= longitudDireccion;
        dy /= longitudDireccion;

        int alcanceMaximo = (int)Math.Ceiling(area.Tamaño);
        for (int f = filaOrigen - alcanceMaximo; f <= filaOrigen + alcanceMaximo; f++)
        {
            for (int c = columnaOrigen - alcanceMaximo; c <= columnaOrigen + alcanceMaximo; c++)
            {
                float pf = f - filaOrigen, pc = c - columnaOrigen;
                float proyeccion = pc * dx + pf * dy;
                float perpendicular = Math.Abs(pc * dy - pf * dx);
                if (proyeccion >= 0 && proyeccion <= area.Tamaño && perpendicular <= area.Ancho / 2f)
                {
                    Celda celda = mapa.ObtenerCelda(f, c);
                    if (celda != null) celdas.Add(celda);
                }
            }
        }
        return celdas;
    }

    // Distancia tipo "tablero de ajedrez" — compartida también, para que
    // rango de ataque, radio de detección y lanzamiento de habilidades
    // midan "cerca" siempre de la misma forma.
    public static int Distancia(int f1, int c1, int f2, int c2)
        => Math.Max(Math.Abs(f1 - f2), Math.Abs(c1 - c2));
}