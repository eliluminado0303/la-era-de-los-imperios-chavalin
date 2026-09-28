using System;
using System.Collections.Generic;

// Estructura con vida, defensa, costes y progreso de construcción.
public class Edificio
{
    private readonly object sync = new object();
    private float progresoConstruccion;
    private bool estaConstruido;

    public event Action<Edificio, float> FueAtacado;

    public string Nombre { get; }
    public string Civilizacion { get; }
    public float Vida { get; private set; }
    public int Defensa { get; }

    // La setea Mapa.ColocarEdificio en el momento de construirse. -1 hasta
    // entonces. La usan los edificios "activos" (Torre de Defensa, Granja)
    // para saber dónde están sin tener que escanear todo el tablero.
    public int Fila { get; internal set; } = -1;
    public int Columna { get; internal set; } = -1;

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

    // Antes: Vida -= max(0, daño - Defensa). Como casi todas las unidades
    // pegan entre 8 y 25 y los edificios tienen Defensa de 5 a 40 (Principal
    // 30, Muro 40, Torre 20, Entrenamiento 10), la mayoría de los golpes
    // hacían 0 de daño y el edificio era indestructible. Ahora la Defensa
    // REDUCE el daño en porcentaje (nunca lo anula) y siempre entra al menos
    // 1 de daño. Para que los edificios aguanten más o menos, cambia
    // FACTOR_DEFENSA_EDIFICIO (más alto = más frágiles).
    private const float FACTOR_DEFENSA_EDIFICIO = 50f;

    public void RecibirDaño(float daño)
    {
        FueAtacado?.Invoke(this, daño);
        lock (sync)
        {
            float dañoReal = Math.Max(1f, daño * FACTOR_DEFENSA_EDIFICIO / (FACTOR_DEFENSA_EDIFICIO + Defensa));
            Vida -= dañoReal;
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

// Edificio que puede producir aldeanos una vez terminado.
public class EdificioPrincipal : Edificio
{
    public EdificioPrincipal(string civilizacion, int costoOro, int costoMadera, int costoComida)
        : base("Principal", civilizacion, vidaMaxima: 500, defensa: 30, costoOro, costoMadera, costoComida)
    {
    }

    public bool ProducirAldeano(Jugador jugador, GestorEntrenamiento gestor)
    {
        if (!EstaConstruido) return false;
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
        if (!UnidadesDisponibles.Contains(tipoUnidad)) return false;
        return gestor.EntrenarUnidad(jugador, tipoUnidad, Civilizacion);
    }
}

// Ataca sola a la primera unidad enemiga que se acerque, sin ocupar el
// turno de ninguna unidad del jugador (ver ControladorEdificiosEspeciales,
// que la revisa una vez por frame).
public class TorreDefensa : Edificio
{
    public int Ataque { get; }
    public int Rango { get; }

    public TorreDefensa(string civilizacion, int costoOro, int costoMadera, int costoComida, int ataque = 15, int rango = 4)
        : base("Torre de Defensa", civilizacion, vidaMaxima: 300, defensa: 20, costoOro, costoMadera, costoComida)
    {
        Ataque = ataque;
        Rango = rango;
    }
}

// Cada una construida sube el tope de población del jugador (ver
// ControladorMapa.ActualizarResultados, que aplica el bono al terminar
// la construcción, y Jugador.LimitePoblacion).
public class Casa : Edificio
{
    public int PoblacionQueOtorga { get; }

    public Casa(string civilizacion, int costoOro, int costoMadera, int costoComida, int poblacionQueOtorga = 5)
        : base("Casa", civilizacion, vidaMaxima: 150, defensa: 5, costoOro, costoMadera, costoComida)
    {
        PoblacionQueOtorga = poblacionQueOtorga;
    }
}

// Genera comida sola cada tanto (ver ControladorEdificiosEspeciales), sin
// necesitar que un Aldeano esté parado recolectando ahí.
public class Granja : Edificio
{
    public int ComidaPorCiclo { get; }

    public Granja(string civilizacion, int costoOro, int costoMadera, int costoComida, int comidaPorCiclo = 8)
        : base("Granja", civilizacion, vidaMaxima: 120, defensa: 0, costoOro, costoMadera, costoComida)
    {
        ComidaPorCiclo = comidaPorCiclo;
    }
}

// Pura defensa: no hace nada por sí sola, pero al ocupar la celda ya
// bloquea el paso — Mapa.MoverUnidad rechaza cualquier destino que tenga
// un Edificio, así que esto funciona sin lógica extra.
public class Muro : Edificio
{
    public Muro(string civilizacion, int costoOro, int costoMadera, int costoComida)
        : base("Muro", civilizacion, vidaMaxima: 250, defensa: 40, costoOro, costoMadera, costoComida)
    {
    }
}