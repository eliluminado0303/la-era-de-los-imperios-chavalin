using System;   

public class Unidad {
    public int Vida;
    public int Ataque;
    public int Defensa;
    public int Velocidad;
    public int Rango;
    public string Civilizacion;

    // cada clase elije como modificar estos stats
    protected float ProbabilidadEsquivar;
    protected float ProbabilidadCritico;
    protected float multiplicadorCritico;
    
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
            Debug.WriteLine($"{Civilizacion}: esquivó el ataque :o");
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
            Debug.WriteLine($"{Civilizacion}: hizo un golpe crítico!");
        }
        objetivo.RecibirDaño(daño);

    }
   
}