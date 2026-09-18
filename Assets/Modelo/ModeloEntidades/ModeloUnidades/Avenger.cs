// Unidad defensiva exclusiva de la civilización Griegos.
public class Avenger : Unidad
{
    public int TropasALiberar = 2;

    public Avenger()
    {
        Vida = 175;
         VidaMaxima = Vida; 
         Ataque = 0; 
         Defensa = 20; 
         Velocidad = 5; 
         Rango = 0;
        Civilizacion = "Griegos";
        ProbabilidadEsquivar = 0f; 
        ProbabilidadCritico = 0f; 
        multiplicadorCritico = 0f;
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
    }

    protected override void AlMorir()
    {
        base.AlMorir(); // IMPORTANTE: dispara el evento Muerte, si no lo llamas el Controller nunca se entera
        UnityEngine.Debug.Log($"{Civilizacion}: el caballo de Troya cayó, liberando {TropasALiberar} tropas!");
    }
}
 