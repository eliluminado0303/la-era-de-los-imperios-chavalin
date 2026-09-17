/// <summary>Unidad de vanguardia configurable por civilización.</summary>
public class Vanguard: Unidad
{
    /// <summary>Inicializa una vanguardia para la civilización indicada.</summary>
    public Vanguard(string civilizacion)
    {
        Vida = 100;
        Ataque = 10;
        Defensa = 10;
        Velocidad = 5;
        Rango = 1;
        Civilizacion = civilizacion;
        ProbabilidadEsquivar = 0.10f;
        ProbabilidadCritico = 0.25f;
        multiplicadorCritico = 1.5f;
    }

}