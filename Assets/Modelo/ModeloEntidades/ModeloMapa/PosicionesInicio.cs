using System.Collections.Generic;

/// <summary>
/// Posiciones FIJAS de inicio: una esquina por civilización, calculadas a
/// partir del tamaño real del mapa (Mapa.FILAS x Mapa.COLUMNAS) en vez de
/// estar escritas a mano — así, si el mapa vuelve a cambiar de tamaño, las
/// 4 esquinas se recalculan solas y siguen cayendo en tierra de verdad,
/// lejos del agua perimetral (Mapa.ANCHO_AGUA_PERIMETRAL) y con espacio
/// para construir alrededor.
/// </summary>
public static class PosicionesInicio
{
    // Cuántas celdas de colchón se dejan entre el borde de agua y el Centro
    // Urbano de cada civilización (además del ancho de agua en sí). Da
    // espacio de sobra para construir sin quedar pegado a la costa.
    private const int MARGEN_DESDE_AGUA = 20;

    // (fila, columna) del Centro Urbano inicial por civilización.
    private static readonly Dictionary<string, (int fila, int columna)> centros =
        new Dictionary<string, (int fila, int columna)>
        {
            { "Sumerios", (Mapa.ANCHO_AGUA_PERIMETRAL + MARGEN_DESDE_AGUA,                       Mapa.ANCHO_AGUA_PERIMETRAL + MARGEN_DESDE_AGUA) },                       // esquina noroeste
            { "Nipones",  (Mapa.ANCHO_AGUA_PERIMETRAL + MARGEN_DESDE_AGUA,                       Mapa.COLUMNAS - 1 - Mapa.ANCHO_AGUA_PERIMETRAL - MARGEN_DESDE_AGUA) },  // esquina noreste
            { "Vikingos", (Mapa.FILAS - 1 - Mapa.ANCHO_AGUA_PERIMETRAL - MARGEN_DESDE_AGUA,       Mapa.ANCHO_AGUA_PERIMETRAL + MARGEN_DESDE_AGUA) },                       // esquina suroeste
            { "Griegos",  (Mapa.FILAS - 1 - Mapa.ANCHO_AGUA_PERIMETRAL - MARGEN_DESDE_AGUA,       Mapa.COLUMNAS - 1 - Mapa.ANCHO_AGUA_PERIMETRAL - MARGEN_DESDE_AGUA) },  // esquina sureste
        };

    /// <summary>
    /// Devuelve true si la civilización tiene esquina asignada.
    /// </summary>
    public static bool TieneEsquina(string civilizacion)
    {
        return centros.ContainsKey(civilizacion);
    }

    /// <summary>
    /// Intenta obtener la posición de inicio para una civilización.
    /// Retorna false si no existe esa civilización.
    /// </summary>
    public static bool Obtener(string civilizacion, out int fila, out int columna)
    {
        if (centros.TryGetValue(civilizacion, out var posicion))
        {
            fila = posicion.fila;
            columna = posicion.columna;
            return true;
        }
        fila = 0;
        columna = 0;
        return false;
    }
}