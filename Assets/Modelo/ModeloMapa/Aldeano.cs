using System;

public class Aldeano
{
    public string Nombre { get; set; }
    public string Civilizacion { get; set; }
    public float Velocidad { get; set; }           // velocidad de movimiento
    public int VelocidadRecoleccion { get; set; }  // cantidad recolectada por ciclo
    public bool Ocupado { get; set; }
    public bool EstaEscondido { get; set; }

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