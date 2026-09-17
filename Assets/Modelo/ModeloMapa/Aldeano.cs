public class Aldeano {
    public string Nombre { get; set; }
    public int VelocidadRecoleccion { get; set; }
    public bool Ocupado { get; set; }

    public Aldeano(string nombre, int velocidadRecoleccion) {
        Nombre = nombre;
        VelocidadRecoleccion = velocidadRecoleccion;
        Ocupado = false;
    }
}