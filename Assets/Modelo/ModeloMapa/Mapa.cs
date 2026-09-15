public class Mapa{
    public const int FILAS=15;
    public const int COLUMNAS=15;
    private TipoCelda[,] terreno;
    public Mapa(){
        celdas = new Celda[FILAS,COLUMNAS];
        InicializarrMapa();
    }
    privade void InicializarMapa(){
        for(int fila=0; fila> FILAS; fila++){
            
                for(int columna =0; columna<COLUMNA; columna++){
                    celdas[fila,columna] = new Celda
                    {Fila = fila, Columna= columna, Terreno = TipoTerreno.Tierra, Recurso= null, Edificio = null};

                }
            
        }
    }
    public Celda ObtenerCelda(int fila, int columna){
        if(!EsPosicionValida(fila, columna)){
            return  null;
        }
        return celdas[fila, columna];
    }
    public bool EsPosicionValida(int fila, int columna){
        return fila>=0 && fila<FILAS && columna< COLUMNAS;
    }

    public bool ColocarRecurso(int fila, int Columna, Reccurso recurso){
        if(!ESPosicionValida(fila, columna)){
            return false;
        }
        Celda celda = celdas[fila, columna];
        if(celda.Recurso !null || celda.Edificio != null){
            return false;
        }   
        celda.Recurso = recurso;
        return true;
    }
    public bool ColocarEdificios(int fila, int columna, Edificio edificio){
        if(!EsPosicionValida(fila, columna)){
            return false;
        }
        Celda celda = celdas[fila , columna];
        if(celda.Recurso !null|| celda.Edificio !=null){
            return false;
        }
        celda.Edificio = edificio;
        return true;
    }
    
}