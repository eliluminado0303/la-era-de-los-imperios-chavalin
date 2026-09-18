using System;

public class Edificio
{
    private readonly object sync = new object();
    private float progresoConstruccion;
    private bool estaConstruido;

    public string Nombre { get; }
    public string Civilizacion { get; }
    public float Vida { get; private set; }
    public int Defensa { get; }

    // Fusionado desde la versión de tu compañera
    public int CostoOro { get; }
    public int CostoMadera { get; }
    public int CostoComida { get; }

    public bool EstaConstruido { get { lock (sync) return estaConstruido; } }
    public float ProgresoConstruccion { get { lock (sync) return progresoConstruccion; } }

    public Edificio(string nombre, string civilizacion, float vidaMaxima, int defensa,
                     int costoOro, int costoMadera, int costoComida)
    {
        Nombre = nombre;
        Civilizacion = civilizacion;
        Vida = vidaMaxima;
        Defensa = defensa;
        CostoOro = costoOro;
        CostoMadera = costoMadera;
        CostoComida = costoComida;
    }

    public void RecibirDaño(float daño)
    {
        lock (sync)
        {
            Vida -= Math.Max(0, daño - Defensa);
            if (Vida <= 0) { Vida = 0; Console.WriteLine($"{Nombre} fue destruido."); }
        }
    }

    public void AvanzarConstruccion(float porcentaje)
    {
        if (porcentaje <= 0) return;
        lock (sync)
        {
            if (estaConstruido) return;
            progresoConstruccion = Math.Min(100, progresoConstruccion + porcentaje);
            if (progresoConstruccion >= 100) { estaConstruido = true; Console.WriteLine($"{Nombre} terminado."); }
        }
    }
}