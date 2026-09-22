public class Healer : Unidad
{
    private float cantidadCuracion = 15f;

    public Healer(string civilizacion)
    {
        Vida = 70; 
        VidaMaxima = Vida; 
        Ataque = 3; 
        Defensa = 4; 
        Velocidad = 4; 
        Rango = 5;
        Civilizacion = civilizacion;
        ProbabilidadEsquivar = 0.25f; 
        ProbabilidadCritico = 0f; 
        multiplicadorCritico = 0f;
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
    }
    

    public override void Curar(Unidad objetivo)
    {
        float curacionEfectiva = objetivo.TieneCuracionReducida ? cantidadCuracion * 0.5f : cantidadCuracion;
        objetivo.RecibirCuracion(curacionEfectiva);
    }
     public AreaEfecto AreaBuff { get; private set; } = new AreaEfecto(FormaArea.Circulo, rangoLanzamiento: 4f, tamaño: 2.5f);
        

    // Agregado dentro de Healer:
    public void Buffear(Unidad objetivo)
    {
    objetivo.AgregarEfecto(new Bendicion(deltaAtaque: 5, deltaDefensa: 8, duracion: 8f));
    }
    
}