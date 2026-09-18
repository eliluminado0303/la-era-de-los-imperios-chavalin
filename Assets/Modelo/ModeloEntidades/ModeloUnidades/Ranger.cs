// Unidad a distancia configurable por civilización.
public class Ranger: Unidad{
        // Inicializa un explorador para la civilización indicada.
        public Ranger(string civilizacion)
    {
        Vida = 80;
        VidaMaxima = Vida;
        Ataque = 10;
        Defensa = 7;
        Velocidad = 4;
        Rango = 8;
        Civilizacion = civilizacion;
        ProbabilidadEsquivar = 0.2f; // 20% de probabilidad de esquivar
        ProbabilidadCritico = 0.20f; // 20% de probabilidad de crítico
        multiplicadorCritico = 1.5f; // Daño crítico es el doble del daño normal
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
    }
    

  
}