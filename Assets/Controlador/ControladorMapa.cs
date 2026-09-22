public class ControladorMapa 
{
    private readonly GestorRecoleccion gestorRecoleccion;
    private readonly GestorConstruccion gestorConstruccion;
    private readonly GestorMovimiento gestorMovimiento;
    private readonly ControladorIA controladorIA;

    public ControladorMapa(GestorRecoleccion gr, GestorConstruccion gc, GestorMovimiento gm, ControladorIA cia) 
    {
        gestorRecoleccion = gr;
        gestorConstruccion = gc;
        gestorMovimiento = gm;
        controladorIA = cia;
    }

    public void ActualizarResultados() 
    {
        while (gestorRecoleccion.ResultadosPendientes.TryDequeue(out ResultadoRecoleccion _)) 
        {
            // Procesar recolección
        }

        while (gestorConstruccion.ResultadosPendientes.TryDequeue(out ResultadoConstruccion resultado)) 
        {
            if (resultado.Exitoso) 
            {
                controladorIA?.ObservarEdificio(resultado.Edificio);
            }
        }

        while (gestorMovimiento.ResultadosPendientes.TryDequeue(out ResultadoMovimiento resultado)) 
        {
            if (resultado.Exitoso && resultado.Unidad != null) 
            {
                controladorIA?.AlTerminarMovimiento(resultado.Unidad, resultado.Fila, resultado.Columna);
            }
        }
    }
}