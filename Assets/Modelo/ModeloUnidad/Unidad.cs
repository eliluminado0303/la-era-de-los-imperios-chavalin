using System;

public class Unidad {
    public float Vida;
    public float Ataque;
    public int Defensa;
    public float Velocidad;
    public float Rango;
    public string Civilizacion;
    public virtual void Atacar()
    {
        Console.WriteLine("ataca");
    }
}