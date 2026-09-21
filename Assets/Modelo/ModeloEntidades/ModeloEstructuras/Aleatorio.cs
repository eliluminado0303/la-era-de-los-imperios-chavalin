using System;
public static class Aleatorio
{
    private static Random random = new Random();
    public static float Valor() => (float)random.NextDouble();
}