using System.Collections.Generic;

public abstract class EfectoEstado
{
    public string Nombre { get; protected set; }
    public float DuracionRestante { get; protected set; }
    protected float intervaloTick;
    protected float tiempoParaProximoTick;

    protected EfectoEstado(string nombre, float duracion, float intervaloTick)
    {
        Nombre = nombre;
        DuracionRestante = duracion;
        this.intervaloTick = intervaloTick;
        tiempoParaProximoTick = intervaloTick;
    }

    public bool HaExpirado => DuracionRestante <= 0;

    public void Actualizar(float deltaTime, Unidad objetivo)
    {
        DuracionRestante -= deltaTime;
        tiempoParaProximoTick -= deltaTime;
        if (tiempoParaProximoTick <= 0)
        {
            AplicarTick(objetivo);
            tiempoParaProximoTick = intervaloTick;
        }
    }

    public void RefrescarDuracion(float nuevaDuracion) => DuracionRestante = nuevaDuracion;

    protected abstract void AplicarTick(Unidad objetivo);

    public virtual void AlSerAgregado(System.Collections.Generic.List<EfectoEstado> listaActual)
        => listaActual.Add(this);

    // Nuevos: se llaman UNA vez, al entrar y al salir de la lista de efectos
    public virtual void OnAplicar(Unidad objetivo) { }
    public virtual void OnExpirar(Unidad objetivo) { }
}
public class Sangrado : EfectoEstado
{
    private float dañoPorTick;

    public Sangrado(float dañoPorTick, float duracion = 6f, float intervalo = 1f)
        : base("Sangrado", duracion, intervalo)
    {
        this.dañoPorTick = dañoPorTick;
    }

    protected override void AplicarTick(Unidad objetivo) => objetivo.RecibirDaño(dañoPorTick);
    // No sobrescribe AlSerAgregado -> hereda el comportamiento de apilarse.
}

public class Veneno : EfectoEstado
{
    private float porcentajeVidaActual;

    public Veneno(float porcentajeVidaActual, float duracion = 5f, float intervalo = 1f)
        : base("Veneno", duracion, intervalo)
    {
        this.porcentajeVidaActual = porcentajeVidaActual;
    }

    protected override void AplicarTick(Unidad objetivo)
        => objetivo.RecibirDaño(objetivo.Vida * porcentajeVidaActual);

    public override void AlSerAgregado(List<EfectoEstado> listaActual)
    {
        var existente = listaActual.Find(e => e is Veneno);
        if (existente != null) existente.RefrescarDuracion(DuracionRestante);
        else listaActual.Add(this);
    }
}

public class Quemadura : EfectoEstado
{
    private float dañoBase;
    private float incrementoPorTick;
    private int ticksAplicados = 0;

    public Quemadura(float dañoBase, float incrementoPorTick, float duracion = 5f, float intervalo = 1f)
        : base("Quemadura", duracion, intervalo)
    {
        this.dañoBase = dañoBase;
        this.incrementoPorTick = incrementoPorTick;
    }

    protected override void AplicarTick(Unidad objetivo)
    {
        objetivo.RecibirDaño(dañoBase + (incrementoPorTick * ticksAplicados));
        ticksAplicados++;
    }

    public override void AlSerAgregado(List<EfectoEstado> listaActual)
    {
        var existente = listaActual.Find(e => e is Quemadura) as Quemadura;
        if (existente != null) existente.RefrescarDuracion(DuracionRestante);
        else listaActual.Add(this);
    }
}

public class Aturdimiento : EfectoEstado
{
    public Aturdimiento(float duracion) : base("Aturdimiento", duracion, duracion) { }
    protected override void AplicarTick(Unidad objetivo) { } // no hace nada por tick, solo "estar presente"
}

public class ReduccionDefensa : EfectoEstado
{
    private int cantidad;
    public ReduccionDefensa(int cantidad, float duracion) : base("ReduccionDefensa", duracion, duracion)
    {
        this.cantidad = cantidad;
    }
    protected override void AplicarTick(Unidad objetivo) { }
    public override void OnAplicar(Unidad objetivo) => objetivo.Defensa -= cantidad;
    public override void OnExpirar(Unidad objetivo) => objetivo.Defensa += cantidad;
}

public class BuffCritico : EfectoEstado
{
    private float delta;
    public BuffCritico(float delta, float duracion) : base("BuffCritico", duracion, duracion)
    {
        this.delta = delta;
    }
    protected override void AplicarTick(Unidad objetivo) { }
    public override void OnAplicar(Unidad objetivo) => objetivo.ModificarProbabilidadCritico(delta);
    public override void OnExpirar(Unidad objetivo) => objetivo.ModificarProbabilidadCritico(-delta);
}

public class Ralentizado : EfectoEstado
{
    private int reduccion;
    public Ralentizado(float porcentaje, float duracion) : base("Ralentizado", duracion, duracion)
    {
        reduccion = 0; // se calcula en OnAplicar, ver abajo
        this.porcentaje = porcentaje;
    }
    private float porcentaje;
    private int cantidadReal;

    protected override void AplicarTick(Unidad objetivo) { }
    public override void OnAplicar(Unidad objetivo)
    {
        cantidadReal = (int)(objetivo.Velocidad * porcentaje);
        objetivo.Velocidad -= cantidadReal;
    }
    public override void OnExpirar(Unidad objetivo) => objetivo.Velocidad += cantidadReal;

    public class Bendicion : EfectoEstado
{
    private int deltaAtaque;
    private int deltaDefensa;

    public Bendicion(int deltaAtaque, int deltaDefensa, float duracion)
        : base("Bendicion", duracion, duracion)
    {
        this.deltaAtaque = deltaAtaque;
        this.deltaDefensa = deltaDefensa;
    }

    protected override void AplicarTick(Unidad objetivo) { }

    public override void OnAplicar(Unidad objetivo)
    {
        objetivo.Ataque += deltaAtaque;
        objetivo.Defensa += deltaDefensa;
    }

    public override void OnExpirar(Unidad objetivo)
    {
        objetivo.Ataque -= deltaAtaque;
        objetivo.Defensa -= deltaDefensa;
    }
}
}
