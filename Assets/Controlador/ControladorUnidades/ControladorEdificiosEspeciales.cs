using System.Collections.Generic;
using System.Linq;

public class ControladorEdificiosEspeciales
{
    private const float INTERVALO_TORRE_SEGUNDOS = 1.2f;
    private const float INTERVALO_GRANJA_SEGUNDOS = 4f;

    private readonly Jugador miJugador;
    private readonly Mapa mapa;
    private readonly Dictionary<Edificio, float> tiempoAcumulado = new Dictionary<Edificio, float>();

    public ControladorEdificiosEspeciales(Jugador miJugador, Mapa mapa)
    {
        this.miJugador = miJugador;
        this.mapa = mapa;
    }

    public void Actualizar(float deltaTime)
    {
        foreach (var edificio in miJugador.Edificios.ToList())
        {
            if (!edificio.EstaConstruido || edificio.Vida <= 0 || edificio.Fila < 0) continue;

            if (edificio is TorreDefensa torre) ActualizarTorre(torre, deltaTime);
            else if (edificio is Granja granja) ActualizarGranja(granja, deltaTime);
        }
    }

    private void ActualizarTorre(TorreDefensa torre, float deltaTime)
    {
        float acumulado = Acumular(torre, deltaTime);
        if (acumulado < INTERVALO_TORRE_SEGUNDOS) return;

        var objetivo = BuscarEnemigoMasCercano(torre);
        if (objetivo == null) return;

        objetivo.RecibirAtaqueEspecial(torre.Ataque);
        tiempoAcumulado[torre] = 0;
    }

    private void ActualizarGranja(Granja granja, float deltaTime)
    {
        float acumulado = Acumular(granja, deltaTime);
        if (acumulado < INTERVALO_GRANJA_SEGUNDOS) return;

        miJugador.AgregarRecurso(TipoRecurso.Comida, granja.ComidaPorCiclo);
        tiempoAcumulado[granja] = 0;
    }

    private float Acumular(Edificio edificio, float deltaTime)
    {
        tiempoAcumulado.TryGetValue(edificio, out float acumulado);
        acumulado += deltaTime;
        tiempoAcumulado[edificio] = acumulado;
        return acumulado;
    }

    private Unidad BuscarEnemigoMasCercano(TorreDefensa torre)
    {
        Unidad mejor = null;
        int mejorDistancia = int.MaxValue;

        foreach (var (fila, columna) in mapa.UnidadesEnRadio(torre.Fila, torre.Columna, torre.Rango))
        {
            var unidad = mapa.ObtenerCelda(fila, columna)?.Unidad;
            if (unidad == null || unidad.Civilizacion == torre.Civilizacion || unidad.Vida <= 0) continue;

            int distancia = System.Math.Max(System.Math.Abs(fila - torre.Fila), System.Math.Abs(columna - torre.Columna));
            if (distancia < mejorDistancia) { mejorDistancia = distancia; mejor = unidad; }
        }
        return mejor;
    }
}