// Unidad a distancia exclusiva de la civilización Sumerios.
public class Caster : Unidad
{
    private float probabilidadQuemadura = 0.25f;

    // Radio de área para el básico. 0 = ataque normal de un solo objetivo.
    public AreaEfecto AreaAtaqueBasico { get; protected set; } // null = ataque normal de un solo objetivo
    

    public Caster()
    {
        Vida = 85; 
        VidaMaxima = Vida; 
        Ataque = 12; 
        Defensa = 10; 
        Velocidad = 3; 
        Rango = 7;
        Civilizacion = "Sumerios";
        ProbabilidadEsquivar = 0.05f; 
        ProbabilidadCritico = 0.25f; 
        multiplicadorCritico = 1.5f;
        AreaAtaqueBasico = new AreaEfecto(FormaArea.Circulo, rangoLanzamiento: 0f, tamaño: 2.5f);
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;

    }

    protected override void AplicarEfectoAlGolpear(Unidad objetivo)
    {
        if (UnityEngine.Random.value < probabilidadQuemadura)
            objetivo.AgregarEfecto(new Quemadura(3f, 1.5f));
    }
}