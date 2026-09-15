using System;   
public class Assassin: Unidad
{
    public Assassin()
    {
        Vida = 75;
        Ataque = 18;
        Defensa = 5;
        Velocidad = 7;
        Rango = 1;
        Civilizacion = "Nipones";
        ProbabilidadEsquivar = 0.38f; // 38% de probabilidad de esquivar
        ProbabilidadCritico = 0.20f; // 20% de probabilidad de crítico
        multiplicadorCritico = 2.2f; // Daño crítico es el doble del daño normal
    }




   
}