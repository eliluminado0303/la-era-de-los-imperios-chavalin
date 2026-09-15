public class Ranger: Unidad{
        public Ranger()
    {
        Vida = 80;
        Ataque = 10;
        Defensa = 7;
        Velocidad = 4;
        Rango = 8;
        Civilizacion = "{Civilizacion}";
        ProbabilidadEsquivar = 0.2f; // 20% de probabilidad de esquivar
        ProbabilidadCritico = 0.20f; // 20% de probabilidad de crítico
        multiplicadorCritico = 1.5f; // Daño crítico es el doble del daño normal
    }
    

  
}