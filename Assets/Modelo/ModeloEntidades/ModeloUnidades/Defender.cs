using System;

public class Defender : Unidad
{
    public float RadioTaunt = 3f; // el Controller redirige hacia acá el daño de aliados cercanos

    private float escudoActual;
    private float escudoMaximo = 40f;
    private float tiempoRecargaEscudo = 8f;
    private float tiempoParaRecarga = 0f;

    public Defender(string civilizacion)
    {
        Vida = 150; 
        VidaMaxima = Vida; 
        Ataque = 8; 
        Defensa = 22; 
        Velocidad = 4; 
        Rango = 1;
        Civilizacion = civilizacion;
        ProbabilidadEsquivar = 0f; 
        ProbabilidadCritico = 0.10f; 
        multiplicadorCritico = 1.5f;
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
        escudoActual = escudoMaximo;
    }

    protected override void RecibirDaño(float daño, bool ignorarDefensa = false)
    {
        if (escudoActual > 0)
        {
            float absorbido = Math.Min(escudoActual, daño);
            escudoActual -= absorbido;
            daño -= absorbido;
            tiempoParaRecarga = tiempoRecargaEscudo;
            if (daño <= 0) return; // el escudo absorbió todo el golpe
        }
        base.RecibirDaño(daño, ignorarDefensa);
    }

    // Llamar cada frame desde el Controller, igual que ActualizarEfectos
    public void ActualizarEscudo(float deltaTime)
    {
        if (escudoActual >= escudoMaximo) return;
        tiempoParaRecarga -= deltaTime;
        if (tiempoParaRecarga <= 0) escudoActual = escudoMaximo;
    }
}