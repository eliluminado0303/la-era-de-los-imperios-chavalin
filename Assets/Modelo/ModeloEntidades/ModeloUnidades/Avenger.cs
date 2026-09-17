/// <summary>Unidad defensiva exclusiva de la civilización Griegos.</summary>
public class Avenger : Unidad
{
    /// <summary>Inicializa las estadísticas del vengador.</summary>
    public Avenger()
    {
        Vida = 175;
        Ataque = 0;
        Defensa = 20;
        Velocidad = 5;
        Rango = 0;
        Civilizacion = "Griegos";
        ProbabilidadEsquivar = 0f;
        ProbabilidadCritico = 0f;
        multiplicadorCritico = 0f;
    }
}


 