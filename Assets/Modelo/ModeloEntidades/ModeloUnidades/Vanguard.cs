// Unidad de vanguardia configurable por civilización.
public class Vanguard: Unidad
{
    // Inicializa una vanguardia para la civilización indicada.
    public Vanguard(string civilizacion)
    {
        Vida = 110;
        VidaMaxima = Vida;
        Ataque = 12;
        Defensa = 15;
        Velocidad = 5;
        Rango = 1;
        Civilizacion = civilizacion;
        ProbabilidadEsquivar = 0.10f;
        ProbabilidadCritico = 0.25f;
        multiplicadorCritico = 1.5f;
        TicksAtaqueBasico = 2; 
        CostoOro = 30;
        CostoMadera = 15;
        CostoComida = 40;
    }

}