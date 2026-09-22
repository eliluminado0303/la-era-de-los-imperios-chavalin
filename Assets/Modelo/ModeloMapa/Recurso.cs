public class Recurso
{
    public TipoRecurso Tipo { get; set; }

    public int Cantidad { get; set; }

    public int Recolectar(int cantidadSolicitada)
    {
        if (cantidadSolicitada <= 0)
        {
            return 0;
        }

        int cantidadReal = System.Math.Min(cantidadSolicitada, Cantidad);

        Cantidad -= cantidadReal;

        return cantidadReal;
    }

    public bool EstaAgotado()
    {
        return Cantidad <= 0;
    }
}