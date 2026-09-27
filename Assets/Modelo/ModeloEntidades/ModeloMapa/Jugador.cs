using System.Collections.Generic;

public class Jugador 
{
    public string Nombre { get; set; }
    public Dictionary<TipoRecurso, int> Recursos { get; set; }
    public List<Edificio> Edificios { get; set; }
    public List<Unidad> Unidades { get; set; }
    public List<Aldeano> Aldeanos { get; set; }

    // Tope de unidades + aldeanos que podés tener a la vez. Sube 5 por cada
    // Casa que termines de construir (ver ControladorMapa.ActualizarResultados).
    public int LimitePoblacion { get; set; } = 10;
    public int PoblacionActual => Unidades.Count + Aldeanos.Count;

    private readonly object candado = new object();

    public Jugador(string nombre) 
    {
        Nombre = nombre;
        // Sin esto, el Centro Urbano inicial se quedaba sin forma de pagar
        // el primer Aldeano (25 Oro + 30 Comida): hacia falta un Aldeano
        // para recolectar, pero hacia falta un recurso ya recolectado para
        // pagar al primer Aldeano. Este monto alcanza para varios Aldeanos
        // mientras se llega a los recursos del mapa.
        Recursos = new Dictionary<TipoRecurso, int> {
            { TipoRecurso.Oro, 200 },
            { TipoRecurso.Madera, 100 },
            { TipoRecurso.Comida, 150 }
        };
        Edificios = new List<Edificio>();
        Unidades = new List<Unidad>();
        Aldeanos = new List<Aldeano>();
    }

    public void AgregarRecurso(TipoRecurso tipo, int cantidad) 
    {
        if (cantidad <= 0) return;
        lock (candado) 
        {
            Recursos[tipo] += cantidad;
        }
    }

    public bool TieneSuficiente(TipoRecurso tipo, int cantidad) 
    {
        if (cantidad < 0) return false;
        lock (candado) 
        {
            return Recursos[tipo] >= cantidad;
        }
    }

        // Verifica y descuenta los tres recursos como UNA sola operación bajo un
    // solo candado — así nunca se puede gastar oro y luego fallar en madera
    // (lo que dejaría al jugador con oro perdido sin construir/entrenar nada).
    public bool GastarRecursos(int oro, int madera, int comida)
    {
        if (oro < 0 || madera < 0 || comida < 0) return false;
        lock (candado)
        {
            if (Recursos[TipoRecurso.Oro] < oro) return false;
            if (Recursos[TipoRecurso.Madera] < madera) return false;
            if (Recursos[TipoRecurso.Comida] < comida) return false;

            Recursos[TipoRecurso.Oro] -= oro;
            Recursos[TipoRecurso.Madera] -= madera;
            Recursos[TipoRecurso.Comida] -= comida;
            return true;
        }
    }

    public void AgregarEdificio(Edificio edificio) 
    {
        if (edificio == null) return;
        lock (candado) 
        {
            Edificios.Add(edificio);
        }
    }

    public bool RetirarEdificio(Edificio edificio) 
    {
        if (edificio == null) return false;
        lock (candado) 
        {
            return Edificios.Remove(edificio);
        }
    }

    public void AgregarUnidad(Unidad unidad) 
    {
        if (unidad == null) return;
        lock (candado) 
        {
            Unidades.Add(unidad);
        }
    }

    public void AgregarAldeano(Aldeano aldeano) 
    {
        if (aldeano == null) return;
        lock (candado) 
        {
            Aldeanos.Add(aldeano);
        }
    }

    public bool TieneEdificioPrincipal() 
    {
        lock (candado) 
        {
            foreach (Edificio edificio in Edificios) 
            {
                if (edificio is EdificioPrincipal && edificio.Vida > 0) 
                {
                    return true;
                }
            }
            return false;
        }
    }
    
}