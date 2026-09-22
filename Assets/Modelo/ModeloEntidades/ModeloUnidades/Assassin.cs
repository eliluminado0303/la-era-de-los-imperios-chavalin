using System;   
using System.Collections.Generic;

// Unidad ofensiva exclusiva de la civilización Nipones.
public class Assassin : Unidad
{
    private float probabilidadSangradoBasico = 0.15f;

    public Assassin()
    {
        Vida = 75; 
        VidaMaxima = Vida;
        Ataque = 18; 
        Defensa = 5; 
        Velocidad = 7; 
        Rango = 1; 
        Civilizacion = "Nipones";
        ProbabilidadEsquivar = 0.30f; 
        ProbabilidadCritico = 0.20f; 
        multiplicadorCritico = 2.2f;
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
    }

    protected override void AplicarEfectoAlGolpear(Unidad objetivo)
    {
        if (Aleatorio.Valor() < probabilidadSangradoBasico)
            objetivo.AgregarEfecto(new Sangrado(4f));
    }

   public override void HabilidadEspecial(List<Unidad> objetivos)
    {
    this.AgregarEfecto(new BuffCritico(0.25f, 5f));
    foreach (var objetivo in objetivos)
        objetivo.AgregarEfecto(new Sangrado(6f));
    }
}