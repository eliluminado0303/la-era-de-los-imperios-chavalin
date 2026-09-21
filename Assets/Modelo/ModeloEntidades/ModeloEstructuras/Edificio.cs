using System;
using System.Collections.Generic;

// Estructura con vida, defensa, costes y progreso de construcción.
public class Edificio
{
    // Protege el estado que puede consultarse o actualizarse desde tareas.
    private readonly object sync = new object();
    private float progresoConstruccion;
    private bool estaConstruido;

    public string Nombre { get; }
    public string Civilizacion { get; }
    public float Vida { get; private set; }
    public int Defensa { get; }

    // Fusionado desde la versión de tu compañera
    // Coste necesario para construir el edificio.
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

    // Aplica daño después de descontar la defensa.
    public void RecibirDaño(float daño)
    {
        lock (sync)
        {
            Vida -= Math.Max(0, daño - Defensa);
            if (Vida <= 0) { Vida = 0; Console.WriteLine($"{Nombre} fue destruido."); }
        }
    }

    // Avanza el porcentaje de construcción hasta completarlo.
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
    
    // Edificio que puede producir aldeanos una vez terminado.
    public class EdificioPrincipal : Edificio
    {
    public EdificioPrincipal(string civilizacion, int costoOro, int costoMadera, int costoComida)
        : base("Principal", civilizacion, vidaMaxima: 500, defensa: 30, costoOro, costoMadera, costoComida)
    {
    }

    public bool ProducirAldeano(Jugador jugador, GestorEntrenamiento gestor)
    {
        if (!EstaConstruido) return false; // no puede producir nada mientras se sigue construyendo
        return gestor.EntrenarAldeano(jugador, Civilizacion);
    }
    }

    // Edificio que limita qué tipos de unidades puede producir.
    public class EdificioEntrenamiento : Edificio
    {
    public List<string> UnidadesDisponibles { get; private set; }

    public EdificioEntrenamiento(string civilizacion, int costoOro, int costoMadera, int costoComida, List<string> unidadesDisponibles)
        : base("Entrenamiento", civilizacion, vidaMaxima: 200, defensa: 10, costoOro, costoMadera, costoComida)
    {
        UnidadesDisponibles = unidadesDisponibles;
    }

    public bool ProducirUnidad(Jugador jugador, GestorEntrenamiento gestor, string tipoUnidad)
    {
        if (!EstaConstruido) return false;
        if (!UnidadesDisponibles.Contains(tipoUnidad)) return false; // solo entrena lo que tiene habilitado
        return gestor.EntrenarUnidad(jugador, tipoUnidad, Civilizacion);
    }
    }
