using System;

/// <summary>Representa una unidad civil encargada de recolectar un recurso.</summary>
public class Aldeano
{
    /// <summary>Nombre identificador del aldeano.</summary>
    public string Nombre;
    /// <summary>Civilización a la que pertenece.</summary>
    public string Civilizacion;
    /// <summary>Velocidad de movimiento del aldeano.</summary>
    public float Velocidad;

    /// <summary>Nombre del recurso que recolecta.</summary>
    public string TipoRecurso;
    /// <summary>Cantidad producida en cada ciclo de recolección.</summary>
    public float CantidadPorTick;

    /// <summary>Indica si el aldeano está oculto y no puede recolectar.</summary>
    public bool EstaEscondido = false;

    /// <summary>Crea un aldeano con sus estadísticas de recolección.</summary>
    public Aldeano(string nombre, string civilizacion, float velocidad, string tipoRecurso, float cantidadPorTick)
    {
        Nombre = nombre;
        Civilizacion = civilizacion;
        Velocidad = velocidad;
        TipoRecurso = tipoRecurso;
        CantidadPorTick = cantidadPorTick;
    }

    /// <summary>Oculta al aldeano y detiene su producción.</summary>
    public void Esconder()
    {
        EstaEscondido = true;
        Console.WriteLine($"{Nombre} se escondió.");
    }

    /// <summary>Hace que el aldeano vuelva a estar disponible.</summary>
    public void Salir()
    {
        EstaEscondido = false;
    }

    /// <summary>Calcula la producción de un ciclo de recolección.</summary>
    public float RecolectarTick()
    {
        if (EstaEscondido) return 0;
        return CantidadPorTick;
    }
}