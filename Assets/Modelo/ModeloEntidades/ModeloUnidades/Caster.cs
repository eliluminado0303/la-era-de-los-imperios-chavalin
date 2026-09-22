// Unidad a distancia exclusiva de la civilización Sumerios.
public class Caster : Unidad
{
    // Probabilidad de aplicar quemadura al impactar.
    private float probabilidadQuemadura = 0.25f;

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
        // El ataque básico del Caster usa un área circular.
        AreaAtaqueBasico = new AreaEfecto(FormaArea.Circulo, rangoLanzamiento: 0f, tamaño: 2.5f);
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;

    }

    // Aplica una quemadura de forma aleatoria después del impacto.
    protected override void AplicarEfectoAlGolpear(Unidad objetivo)
    {
        if (Aleatorio.Valor() < probabilidadQuemadura)
            objetivo.AgregarEfecto(new Quemadura(3f, 1.5f));
    }
}