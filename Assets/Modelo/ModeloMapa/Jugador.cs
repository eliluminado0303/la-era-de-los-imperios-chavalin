using System.Collections.Generic;

public class Jugador
{
    public string Nombre { get; set; }

    public Dictionary<TipoRecurso, int> Recursos { get; set; }

    public List<Edificio> Edificios { get; set; }

    public Jugador(string nombre)
    {
        Nombre = nombre;

        Recursos = new Dictionary<TipoRecurso, int>
        {
            { TipoRecurso.Oro, 0 },
            { TipoRecurso.Madera, 0 },
            { TipoRecurso.Comida, 0 }
        };

        Edificios = new List<Edificio>();
    }

    public void AgregarRecurso(TipoRecurso tipo, int cantidad)
    {
        if (cantidad <= 0)
        {
            return;
        }

        Recursos[tipo] += cantidad;
    }

    public bool TieneSuficiente(TipoRecurso tipo, int cantidad)
    {
        if (cantidad < 0)
        {
            return false;
        }

        return Recursos[tipo] >= cantidad;
    }

    public bool GastarRecurso(TipoRecurso tipo, int cantidad)
    {
        if (cantidad <= 0)
        {
            return false;
        }

        if (!TieneSuficiente(tipo, cantidad))
        {
            return false;
        }

        Recursos[tipo] -= cantidad;

        return true;
    }

    public void AgregarEdificio(Edificio edificio)
    {
        if (edificio == null)
        {
            return;
        }

        Edificios.Add(edificio);
    }

    public bool RetirarEdificio(Edificio edificio)
    {
        if (edificio == null)
        {
            return false;
        }

        return Edificios.Remove(edificio);
    }
}