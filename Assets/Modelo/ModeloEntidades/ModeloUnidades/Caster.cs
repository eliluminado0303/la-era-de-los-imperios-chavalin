/// <summary>Unidad a distancia exclusiva de la civilización Sumerios.</summary>
public class Caster: Unidad
{
    /// <summary>Inicializa las estadísticas del lanzador.</summary>
     public Caster()
    {
        Vida = 85;
        Ataque = 12;
        Defensa = 10;
        Velocidad = 3;
        Rango = 7;
        Civilizacion = "Sumerios";
        ProbabilidadEsquivar = 0.05f;
        ProbabilidadCritico = 0.25f;
        multiplicadorCritico = 1.5f;
    }

   
}