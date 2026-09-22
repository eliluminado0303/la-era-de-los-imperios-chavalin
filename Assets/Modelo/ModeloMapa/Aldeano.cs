using System;

public class Aldeano
{
    public string Nombre { get; set; }
    public string Civilizacion { get; set; }
    public float Velocidad { get; set; }
    public int VelocidadRecoleccion { get; set; }
    public bool Ocupado { get; set; }
    public bool EstaEscondido { get; set; }
    public int CostoOro { get; set; } = 25;
    public int CostoMadera { get; set; } = 0;
    public int CostoComida { get; set; } = 30;

    public Aldeano(string nombre, string civilizacion, float velocidad, int velocidadRecoleccion)
    {
        Nombre = nombre;
        Civilizacion = civilizacion;
        Velocidad = velocidad;
        VelocidadRecoleccion = velocidadRecoleccion;
        Ocupado = false;
        EstaEscondido = false;
    }

    public void Esconder() { EstaEscondido = true; Console.WriteLine($"{Nombre} se escondió."); }
    public void Salir() { EstaEscondido = false; }
}