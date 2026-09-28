using System;

public class Aldeano

{
    public string Nombre { get; set; }
    public string Civilizacion { get; set; }
    public float Velocidad { get; set; }
    public int VelocidadRecoleccion { get; set; }
    public bool Ocupado { get; set; }
    public int CostoOro { get; set; } = 25;
    public int CostoMadera { get; set; } = 0;
    public int CostoComida { get; set; } = 30;
    // -1 significa "todavía no se colocó en el mapa" (por ejemplo, recién
    // entrenado y la Vista aún no le buscó un lugar).
    public int Fila { get; set; } = -1;
    public int Columna { get; set; } = -1;

    // ---------------------------------------------------------------
    // Vida y muerte. El Aldeano no pelea, pero sí puede ser atacado: una
    // unidad enemiga lo golpea con Unidad.Atacar(Aldeano) y, si llega a 0,
    // el aldeano muere (ver Partida.EjecutarAtaqueAAldeano).
    // ---------------------------------------------------------------
    public const int VIDA_MAXIMA_INICIAL = 30;
    public int VidaMaxima { get; private set; } = VIDA_MAXIMA_INICIAL;
    public int Defensa { get; set; } = 0;
    // Qué tipo de recurso está trabajando AHORA MISMO (ya llegó y está
// recolectando). null mientras camina o está libre. La Vista lo usa
// para elegir la animación (hacha / pico / cuchillo).
    public TipoRecurso? RecolectandoTipo { get; set; }

    // La vida se lee desde tareas en segundo plano (GestorRecoleccion
    // revisa EstaVivo en cada ciclo) y se escribe desde el hilo principal
    // (combate), así que se protege con un candado propio.
    private int vida = VIDA_MAXIMA_INICIAL;
    private readonly object candadoVida = new object();

    public int Vida
    {
        get { lock (candadoVida) { return vida; } }
    }

    public bool EstaVivo => Vida > 0;

    // Se dispara una sola vez, cuando la vida llega a 0.
    public event Action<Aldeano> Muerte;

    // Se dispara cada vez que lo golpean (aunque no muera): (yo, quien me atacó).
    public event Action<Aldeano, Unidad> FueAtacado;

    public Aldeano(string nombre, string civilizacion, float velocidad, int velocidadRecoleccion)
    {
        Nombre = nombre;
        Civilizacion = civilizacion;
        Velocidad = velocidad;
        VelocidadRecoleccion = velocidadRecoleccion;
        Ocupado = false;
    }

    // Resta el daño (menos la defensa) y avisa si murió. Devuelve true si
    // este golpe fue el que lo mató, para que quien ataca pueda reaccionar.
    public bool RecibirDaño(float daño, Unidad atacante = null)
    {
        bool murioAhora = false;
        lock (candadoVida)
        {
            if (vida <= 0) return false; // ya estaba muerto: no muere dos veces
            vida -= Math.Max(0, (int)(daño - Defensa));
            if (vida <= 0)
            {
                vida = 0;
                murioAhora = true;
            }
        }

        // Los eventos se disparan FUERA del candado para que un suscriptor
        // lento (o que vuelva a tocar al aldeano) no pueda causar un bloqueo.
        if (atacante != null) FueAtacado?.Invoke(this, atacante);
        if (murioAhora)
        {
            Ocupado = false;
            Muerte?.Invoke(this);
        }
        return murioAhora;
    }
}
