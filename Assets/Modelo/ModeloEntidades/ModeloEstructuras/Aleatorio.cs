using System;

public static class Aleatorio
{
    private static Random random = new Random();
    public static float Valor() => (float)random.NextDouble();
        // Entero aleatorio en [minInclusive, maxExclusive). La IA lo usa para
    // elegir al azar qué tipo de unidad entrenar entre varias opciones.
    public static int Entero(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);
}

