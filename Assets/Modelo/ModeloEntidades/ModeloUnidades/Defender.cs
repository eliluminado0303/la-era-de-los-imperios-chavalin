/// <summary>Unidad defensiva configurable por civilización.</summary>
public class Defender: Unidad{
     /// <summary>Inicializa un defensor para la civilización indicada.</summary>
     public Defender(string civilizacion)
    {
        Vida = 150;
        Ataque = 8;
        Defensa = 22;
        Velocidad = 4;
        Rango = 1;
        Civilizacion = civilizacion;
        ProbabilidadEsquivar = 0f;
        ProbabilidadCritico = 0.10f;
        multiplicadorCritico = 1.5f;
    }

    
}