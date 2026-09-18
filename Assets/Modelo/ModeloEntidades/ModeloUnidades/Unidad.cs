using System;
using System.Collections.Generic;

public class Unidad
{
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

    private List<EfectoEstado> efectos = new List<EfectoEstado>();

    protected virtual bool Esquivar() => UnityEngine.Random.value < ProbabilidadEsquivar;
    protected virtual bool EsCritico() => UnityEngine.Random.value < ProbabilidadCritico;

    public void ModificarProbabilidadCritico(float delta) => ProbabilidadCritico += delta;

    protected virtual void RecibirDaño(float daño, bool ignorarDefensa = false)
    {
        if (Esquivar())
        {
            UnityEngine.Debug.Log($"{Civilizacion}: esquivó el ataque :o");
            return;
        }
        float defensaEfectiva = ignorarDefensa ? 0 : Defensa;
        Vida -= Math.Max(0, (int)daño - defensaEfectiva);
        if (Vida <= 0) { Vida = 0; AlMorir(); }
    }

    // Público a propósito: así cualquier habilidad especial puede usarlo sin CS1540
    public void RecibirAtaqueEspecial(float daño, bool ignorarDefensa = false)
        => RecibirDaño(daño, ignorarDefensa);

    /// <summary>Se dispara una vez cuando la unidad llega a cero de vida.</summary>
    public event Action<Unidad> Muerte;

    /// <summary>Notifica la muerte de la unidad a los sistemas suscritos.</summary>
    protected virtual void AlMorir() => Muerte?.Invoke(this);

    public virtual void Atacar(Unidad objetivo)
    {
        if (objetivo == null) throw new ArgumentNullException(nameof(objetivo));
        if (EstaAturdido) return;

        float daño = Ataque;
        if (EsCritico()) daño *= multiplicadorCritico;
        objetivo.RecibirDaño(daño);
        AplicarEfectoAlGolpear(objetivo);
    }

    /// <summary>
    /// Ejecuta el ataque básico contra todos los objetivos recibidos.
    /// Las unidades normales mantienen un ataque individual por objetivo.
    /// </summary>
    public virtual void Atacar(List<Unidad> objetivos)
    {
        if (objetivos == null) throw new ArgumentNullException(nameof(objetivos));

        foreach (var objetivo in objetivos)
        {
            if (objetivo != null)
                Atacar(objetivo);
        }
    }

    protected virtual void AplicarEfectoAlGolpear(Unidad objetivo) { }

    // Cambiado: ahora recibe una lista, para soportar habilidades en área
    public virtual void HabilidadEspecial(List<Unidad> objetivos) { }

    public void AgregarEfecto(EfectoEstado nuevo)
    {
        nuevo.AlSerAgregado(efectos);
        nuevo.OnAplicar(this);
    }

    public void ActualizarEfectos(float deltaTime)
    {
        foreach (var efecto in efectos) efecto.Actualizar(deltaTime, this);
        var expirados = efectos.FindAll(e => e.HaExpirado);
        foreach (var e in expirados) e.OnExpirar(this);
        efectos.RemoveAll(e => e.HaExpirado);
    }

    public bool EstaAturdido => efectos.Exists(e => e is Aturdimiento);
}