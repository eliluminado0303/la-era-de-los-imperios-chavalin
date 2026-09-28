using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class Unidad
{
    // Estadísticas principales de combate y movimiento.
    public int Vida;
    // En Unidad, junto a los demás campos:
    public int VidaMaxima { get; protected set; }
    public int Ataque;
    public int Defensa;
    public int Velocidad;
    public int Rango;
    public string Civilizacion;

    protected float ProbabilidadEsquivar;
    protected float ProbabilidadCritico;
    protected float multiplicadorCritico;
    public volatile bool EnMovimiento;
    public int Fila { get; set; } = -1;
    public int Columna { get; set; } = -1;
    // En Unidad, junto a los demás campos:
    // Costes necesarios para producir esta unidad.
    public int CostoOro { get; protected set; }
    public int CostoMadera { get; protected set; }
    public int CostoComida { get; protected set; }
    // Área del ataque básico. Si es null, el ataque es de un solo objetivo.
    // Describe el área del ataque básico cuando la unidad tiene uno.
    public AreaEfecto AreaAtaqueBasico { get; protected set; }

    private List<EfectoEstado> efectos = new List<EfectoEstado>();

    protected virtual bool Esquivar() => Aleatorio.Valor() < ProbabilidadEsquivar;
    protected virtual bool EsCritico() => Aleatorio.Valor() < ProbabilidadCritico;

    public void ModificarProbabilidadCritico(float delta) => ProbabilidadCritico += delta;
        // Cantidad de "golpes" visuales que debería mostrar la animación de
    // ataque básico — la mayoría de las unidades solo necesita 1, pero
    // algunas tienen combos de varios golpes en su sprite sheet (Gilgamesh,
    // Medusa, Assassin, Vanguard). El daño total sigue siendo el mismo
    // (Ataque); esto es solo metadata para que la Vista sepa cuántas veces
    // reproducir el "impacto" durante la animación.
    public int TicksAtaqueBasico { get; protected set; } = 1;

    // Igual, pero para la habilidad especial (Gilgamesh, Medusa, Godzilla
    // tienen distinto número de golpes en su combo especial).
    public int TicksHabilidadEspecial { get; protected set; } = 1;

    // Se dispara cuando la unidad completa un ataque básico (yo, cuántos
    // ticks debe mostrar la animación) — la Vista lo usa para saber cuándo
    // y con cuántos "impactos" reproducir la animación de ataque.
    // El tercer parámetro son los objetivos golpeados — la Vista lo
    // necesita para saber hacia dónde apuntar el efecto visual (proyectil,
    // rayo, embestida...). Puede venir null (ataques contra un Edificio,
    // por ejemplo, no tienen una lista de Unidad).
    public event Action<Unidad, int, List<Unidad>> RealizoAtaque;

    // Igual que RealizoAtaque, pero para la habilidad especial.
    public event Action<Unidad, int, List<Unidad>> RealizoHabilidadEspecial;

    // Público a propósito (como NotificarAtaqueRecibido): quien ejecuta el
    // ataque/habilidad (Partida) avisa cuando ya terminó, para que la Vista
    // dispare la animación correspondiente.
    public void NotificarAtaqueRealizado(List<Unidad> objetivos = null) => RealizoAtaque?.Invoke(this, TicksAtaqueBasico, objetivos);
    public void NotificarHabilidadEspecialRealizada(List<Unidad> objetivos = null) => RealizoHabilidadEspecial?.Invoke(this, TicksHabilidadEspecial, objetivos);

    // Calcula si la unidad esquiva y aplica el daño recibido.
    protected virtual void RecibirDaño(float daño, bool ignorarDefensa = false)
    {
        if (Esquivar())
        {
            Registro.Escribir($"{Civilizacion}: esquivó el ataque :o");
            return;
        }
        float defensaEfectiva = ignorarDefensa ? 0 : Defensa;
        Vida -= Math.Max(0, (int)(daño - defensaEfectiva));
        if (Vida <= 0) { Vida = 0; AlMorir(); }
    }

    // Público a propósito: así cualquier habilidad especial puede usarlo sin CS1540
    // Permite a habilidades especiales aplicar daño desde otra unidad.
    public void RecibirAtaqueEspecial(float daño, bool ignorarDefensa = false)
        => RecibirDaño(daño, ignorarDefensa);

    // Se dispara una vez cuando la unidad llega a cero de vida.
    public event Action<Unidad> Muerte;

    // Se dispara cada vez que alguien la ataca (haya esquivado o no) — así
    // cualquier IA que controle a esta unidad puede reaccionar en el momento.
    public event Action<Unidad, Unidad> FueAtacada; // (yo, quien me atacó)

    // Público a propósito, igual que RecibirAtaqueEspecial: permite que quien
    // controla a esta unidad (la IA) sepa que está bajo ataque.
    public void NotificarAtaqueRecibido(Unidad atacante) => FueAtacada?.Invoke(this, atacante);

    // Notifica la muerte de la unidad a los sistemas suscritos.
    protected virtual void AlMorir() => Muerte?.Invoke(this);

    // Ataque normal contra un objetivo.
    public virtual void Atacar(Unidad objetivo)
    {
        if (objetivo == null) throw new ArgumentNullException(nameof(objetivo));
        if (EstaAturdido) return;

        float daño = Ataque;
        if (EsCritico()) daño *= multiplicadorCritico;
        objetivo.NotificarAtaqueRecibido(this);
        objetivo.RecibirDaño(daño);
        AplicarEfectoAlGolpear(objetivo);
        NotificarAtaqueRealizado(new List<Unidad> { objetivo });
    }

    // Ejecuta el ataque básico contra todos los objetivos recibidos.
    // Las unidades normales mantienen un ataque individual por objetivo.
    // Ataque normal contra varios objetivos; las subclases pueden especializarlo.
    public virtual void Atacar(List<Unidad> objetivos)
    {
        if (objetivos == null) throw new ArgumentNullException(nameof(objetivos));

        foreach (var objetivo in objetivos)
        {
            if (objetivo != null)
                Atacar(objetivo);
        }
    }

    // Variante de ataque contra edificios.
    public virtual void Atacar(Edificio objetivo)
    {
        if (objetivo == null) throw new ArgumentNullException(nameof(objetivo));
        if (EstaAturdido) return;

        float daño = Ataque;
        if (EsCritico()) daño *= multiplicadorCritico;
        objetivo.RecibirDaño(daño);
        NotificarAtaqueRealizado();  
    }

    // Variante de ataque contra un aldeano enemigo (no tiene esquiva ni
    // efectos de estado, solo recibe el daño y puede morir).
    public virtual void Atacar(Aldeano objetivo)
    {
        if (objetivo == null) throw new ArgumentNullException(nameof(objetivo));
        if (EstaAturdido) return;

        float daño = Ataque;
        if (EsCritico()) daño *= multiplicadorCritico;
        objetivo.RecibirDaño(daño, this);
        NotificarAtaqueRealizado();
    }

    protected virtual void AplicarEfectoAlGolpear(Unidad objetivo) { }

    // Cambiado: ahora recibe una lista, para soportar habilidades en área
    public virtual void HabilidadEspecial(List<Unidad> objetivos) { }

    public void AgregarEfecto(EfectoEstado nuevo)
    {
        nuevo.AlSerAgregado(efectos);
        nuevo.OnAplicar(this);
    }

    // Actualiza todos los efectos temporales de la unidad.
    public void ActualizarEfectos(float deltaTime)
    {
        foreach (var efecto in efectos) efecto.Actualizar(deltaTime, this);
        var expirados = efectos.FindAll(e => e.HaExpirado);
        foreach (var e in expirados) e.OnExpirar(this);
        efectos.RemoveAll(e => e.HaExpirado);
    }

    public bool EstaAturdido => efectos.Exists(e => e is Aturdimiento);
    
    // En Unidad — necesarios para que la curación tenga sentido:
    // Recupera vida sin superar la vida máxima.
    public void RecibirCuracion(float cantidad)
    {
    Vida = Math.Min(VidaMaxima, Vida + (int)cantidad);
    }

    public bool TieneCuracionReducida => efectos.Exists(e => e is Quemadura);

    // Gancho opcional, igual que HabilidadEspecial — vacío por defecto, solo Healer lo llena
    public virtual void Curar(Unidad objetivo) { }
}