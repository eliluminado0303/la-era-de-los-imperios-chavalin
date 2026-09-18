public class Healer : Unidad{
     /// <summary>Inicializa un defensor para la civilización indicada.</summary>
     public Healer(string civilizacion)
    {
        Vida = 100;
        Ataque = 0;
        Defensa = 20;
        Velocidad = 6;
        Rango = 1;
        Civilizacion = civilizacion;
        ProbabilidadEsquivar = 0f;
        ProbabilidadCritico = 0.10f;
        multiplicadorCritico = 1.5f;
    }

    
}