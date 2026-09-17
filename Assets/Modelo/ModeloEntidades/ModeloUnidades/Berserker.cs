/// <summary>Unidad ofensiva exclusiva de la civilización Vikingos.</summary>
public class Berserker: Unidad
{
    /// <summary>Inicializa las estadísticas del berserker.</summary>
     public Berserker()
    {
        Vida = 125;
        Ataque = 14;
        Defensa = 12;
        Velocidad = 4;
        Rango = 1;
        Civilizacion = "Vikingos";
        ProbabilidadEsquivar = 0f;
        ProbabilidadCritico = 0.35f;
        multiplicadorCritico = 1.5f;
    }


    
}