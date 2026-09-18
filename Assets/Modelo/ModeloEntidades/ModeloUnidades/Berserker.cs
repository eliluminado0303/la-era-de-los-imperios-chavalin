/// <summary>Unidad ofensiva exclusiva de la civilización Vikingos.</summary>
public class Berserker : Unidad
{
    private bool furiaActiva = false;
    private int ataqueOriginal;
    private int velocidadOriginal;
    private int defensaOriginal;

    public Berserker()
    {
        Vida = 125; 
        VidaMaxima = Vida; 
        Ataque = 14; 
        Defensa = 12; 
        Velocidad = 4; 
        Rango = 1;
        Civilizacion = "Vikingos";
        ProbabilidadEsquivar = 0f; 
        ProbabilidadCritico = 0.35f; 
        multiplicadorCritico = 1.5f;

        ataqueOriginal = Ataque;
        velocidadOriginal = Velocidad;
        defensaOriginal = Defensa;
    }

    protected override void RecibirDaño(float daño, bool ignorarDefensa = false)
    {
        base.RecibirDaño(daño, ignorarDefensa);
        VerificarFuria();
    }

    private void VerificarFuria()
    {
        if (furiaActiva || Vida <= 0) return;

        if (Vida <= VidaMaxima / 2)
        {
            furiaActiva = true;
            Ataque = (int)(ataqueOriginal * 1.4f);      // +40% ataque
            Velocidad = (int)(velocidadOriginal * 1.3f); // +30% velocidad
            Defensa = (int)(defensaOriginal * 0.7f);     // -30% defensa
            UnityEngine.Debug.Log($"{Civilizacion}: ¡Berserker entra en furia!");
        }
    }
}