public class Edificio{
    public string Nombre {get;set;}
    public int Vida {get;set;}
    public int CostoOro{get;set;}
    public int CostoMadera {get;set;} 
    public int CostoComida {get;set;}
    public bool Destruido {get;set;}
   
public void RecibirDanio(int cantidad){
    if (cantidad <= 0 || Destruido){
        return;}
    Vida -= cantidad;
    if (Vida <= 0){
        Vida = 0;
        Destruido = true;
    }
  }

}