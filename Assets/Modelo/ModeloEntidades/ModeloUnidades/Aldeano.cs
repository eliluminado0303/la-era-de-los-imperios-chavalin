using System;

// Representa una unidad civil encargada de recolectar un recurso.
public class Aldeano
{
    // Nombre identificador del aldeano.
    public string Nombre;
    // Civilización a la que pertenece.
    public string Civilizacion;
    // Velocidad de movimiento del aldeano.
    public float Velocidad;
    public int CostoOro = 25;
    public int CostoMadera = 0;
    public int CostoComida = 30;

    // Nombre del recurso que recolecta.
    public string TipoRecurso;
    // Cantidad producida en cada ciclo de recolección.
    public float CantidadPorTick;

    // Indica si el aldeano está oculto y no puede recolectar.
    public bool EstaEscondido = false;

    // Crea un aldeano con sus estadísticas de recolección.
    public Aldeano(string nombre, string civilizacion, float velocidad, string tipoRecurso, float cantidadPorTick)
    {
        Nombre = nombre;
        Civilizacion = civilizacion;
        Velocidad = velocidad;
        TipoRecurso = tipoRecurso;
        CantidadPorTick = cantidadPorTick;
    }

    // Oculta al aldeano y detiene su producción.
    public void Esconder()
    {
        EstaEscondido = true;
        Console.WriteLine($"{Nombre} se escondió.");
    }

    // Hace que el aldeano vuelva a estar disponible.
    public void Salir()
    {
        EstaEscondido = false;
    }

    // Calcula la producción de un ciclo de recolección.
    public float RecolectarTick()
    {
        if (EstaEscondido) return 0;
        return CantidadPorTick;
    }
}