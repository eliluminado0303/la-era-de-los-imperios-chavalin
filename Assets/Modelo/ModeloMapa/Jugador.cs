using System.Collections.Generic;

public class Jugador {
    public string Nombre { get; set; }
    public Dictionary<TipoRecurso, int> Recursos { get; set; }
    public List<Edificio> Edificios { get; set; }
    public List<Unidad> Unidades { get; set; }
    public List<Aldeano> Aldeanos { get; set; }

    public Jugador(string nombre) {
        Nombre = nombre;
        Recursos = new Dictionary<TipoRecurso, int> {
            { TipoRecurso.Oro, 0 },
            { TipoRecurso.Madera, 0 },
            { TipoRecurso.Comida, 0 }
        };
        Edificios = new List<Edificio>();
        Unidades = new List<Unidad>();
        Aldeanos = new List<Aldeano>();
    }

    public void AgregarRecurso(TipoRecurso tipo, int cantidad) {
        if (cantidad <= 0) return;
        Recursos[tipo] += cantidad;
    }

    public bool TieneSuficiente(TipoRecurso tipo, int cantidad) {
        if (cantidad < 0) return false;
        return Recursos[tipo] >= cantidad;
    }

    public bool GastarRecurso(TipoRecurso tipo, int cantidad) {
        if (cantidad <= 0) return false;
        if (!TieneSuficiente(tipo, cantidad)) return false;
        Recursos[tipo] -= cantidad;
        return true;
    }

    public void AgregarEdificio(Edificio edificio) {
        if (edificio == null) return;
        Edificios.Add(edificio);
    }

    public bool RetirarEdificio(Edificio edificio) {
        if (edificio == null) return false;
        return Edificios.Remove(edificio);
    }

    public void AgregarUnidad(Unidad unidad) {
        if (unidad == null) return;
        Unidades.Add(unidad);
    }

    public void AgregarAldeano(Aldeano aldeano) {
        if (aldeano == null) return;
        Aldeanos.Add(aldeano);
    }

    public bool TieneEdificioPrincipal() {
    foreach (Edificio edificio in Edificios) {
        if (edificio is Edificio.EdificioPrincipal && edificio.Vida > 0) {
            return true;
        }
    }
    return false;
}
}