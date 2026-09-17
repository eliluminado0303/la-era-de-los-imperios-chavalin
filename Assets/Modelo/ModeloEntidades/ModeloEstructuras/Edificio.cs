using System;

/// <summary>
/// Representa una estructura que puede recibir daño y avanzar su construcción.
/// </summary>
public class Edificio
{
    private readonly object sync = new object();
    private float progresoConstruccion;
    private bool estaConstruido;

    /// <summary>Obtiene el nombre visible del edificio.</summary>
    public string Nombre { get; }
    /// <summary>Obtiene la civilización propietaria del edificio.</summary>
    public string Civilizacion { get; }
    /// <summary>Obtiene la vida actual del edificio.</summary>
    public float Vida { get; private set; }
    /// <summary>Obtiene la defensa que reduce el daño recibido.</summary>
    public int Defensa { get; }

    /// <summary>Indica si el edificio alcanzó el 100 % de construcción.</summary>
    public bool EstaConstruido
    {
        get
        {
            lock (sync) return estaConstruido;
        }
    }

    /// <summary>Obtiene el progreso de construcción, expresado como porcentaje.</summary>
    public float ProgresoConstruccion
    {
        get
        {
            lock (sync) return progresoConstruccion;
        }
    }

    /// <summary>Crea un edificio con sus estadísticas iniciales.</summary>
    public Edificio(string nombre, string civilizacion, float vidaMaxima, int defensa)
    {
        Nombre = nombre;
        Civilizacion = civilizacion;
        Vida = vidaMaxima;
        Defensa = defensa;
    }

    /// <summary>Aplica daño reducido por la defensa del edificio.</summary>
    public void RecibirDaño(float daño)
    {
        lock (sync)
        {
            Vida -= Math.Max(0, daño - Defensa);
            if (Vida <= 0)
            {
                Vida = 0;
                Console.WriteLine($"{Nombre} fue destruido.");
            }
        }
    }

    /// <summary>Incrementa el progreso de construcción de forma segura entre hilos.</summary>
    public void AvanzarConstruccion(float porcentaje)
    {
        if (porcentaje <= 0) return;

        lock (sync)
        {
            if (estaConstruido) return;

            progresoConstruccion = Math.Min(100, progresoConstruccion + porcentaje);
            if (progresoConstruccion >= 100)
            {
                estaConstruido = true;
                Console.WriteLine($"{Nombre} terminado.");
            }
        }
    }
}