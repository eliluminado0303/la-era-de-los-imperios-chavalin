// Unidad de vanguardia configurable por civilización.
public class Vanguard: Unidad
{
    // Inicializa una vanguardia para la civilización indicada.
    public Vanguard(string civilizacion)
    {
        Vida = 100;
        VidaMaxima = Vida;
        Ataque = 10;
        Defensa = 10;
        Velocidad = 5;
        Rango = 1;
        Civilizacion = civilizacion;
        ProbabilidadEsquivar = 0.10f;
        ProbabilidadCritico = 0.25f;
        multiplicadorCritico = 1.5f;
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
    }

}