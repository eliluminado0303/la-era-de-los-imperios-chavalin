using System;

/// <summary>Clase base para las unidades que participan en combate.</summary>
public class Unidad {
    /// <summary>Puntos de vida actuales.</summary>
    public int Vida;
    /// <summary>Daño base de los ataques.</summary>
    public int Ataque;
    /// <summary>Defensa que reduce el daño recibido.</summary>
    public int Defensa;
    /// <summary>Velocidad de desplazamiento.</summary>
    public int Velocidad;
    /// <summary>Distancia máxima de ataque.</summary>
    public int Rango;
    /// <summary>Civilización propietaria.</summary>
    public string Civilizacion;

    /// <summary>Probabilidad de evitar un ataque.</summary>
    protected float ProbabilidadEsquivar;
    /// <summary>Probabilidad de realizar un golpe crítico.</summary>
    protected float ProbabilidadCritico;
    /// <summary>Multiplicador aplicado al daño crítico.</summary>
    protected float multiplicadorCritico;
    
    /// <summary>Determina si la unidad esquiva el ataque.</summary>
    protected virtual bool Esquivar()
    {
        return UnityEngine.Random.value < ProbabilidadEsquivar;
        
    }
    protected virtual bool EsCritico()
    {
        return UnityEngine.Random.value < ProbabilidadCritico;
    }
    protected virtual void RecibirDaño(float daño)
    {
        if (Esquivar())
        {
            UnityEngine.Debug.Log($"{Civilizacion}: esquivó el ataque :o");
            return;
        }
        Vida -= Math.Max(0, daño - Defensa);
    }
    public virtual void Atacar(Unidad objetivo)
    {
        float daño = Ataque;
        if (EsCritico())
        {
            daño *= multiplicadorCritico;
            UnityEngine.Debug.Log($"{Civilizacion}: hizo un golpe crítico!");
        }
        objetivo.RecibirDaño(daño);

    }
   
}
    /// <summary>Determina si el ataque actual es crítico.</summary>
    /// <summary>Aplica daño teniendo en cuenta esquiva y defensa.</summary>
    /// <summary>Ataca a la unidad objetivo.</summary>