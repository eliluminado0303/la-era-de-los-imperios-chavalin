using System.Collections.Generic;

// Operaciones de combate que se ejecutan dentro de una partida.
// Nota: "Finalizada" y "VerificarGanador()" viven en la otra mitad de esta
// clase partial (Modelo7/ModeloMapa/Partida.cs). No se redeclaran aquí para
// evitar el conflicto de compilación; en su lugar, este archivo llama a
// VerificarGanador() cada vez que el combate puede haber terminado la partida.
public partial class Partida
{
    public void EjecutarAtaque(Jugador jugadorAtacante, Unidad atacante, Unidad objetivo)
    {
       
        if (!jugadorAtacante.Unidades.Contains(atacante)) return;
        if (jugadorAtacante.Unidades.Contains(objetivo)) return;

        atacante.Atacar(objetivo);
        LimpiarUnidadesMuertas();
        VerificarGanador();
    }

    public void EjecutarAtaqueEnArea(Jugador jugadorAtacante, Unidad atacante, List<Unidad> objetivos)
    {
        
        if (!jugadorAtacante.Unidades.Contains(atacante)) return;

        var objetivosValidos = new List<Unidad>();
        foreach (var objetivo in objetivos)
        {
            if (objetivo == null || jugadorAtacante.Unidades.Contains(objetivo)) continue;
            objetivosValidos.Add(objetivo);
        }

        atacante.Atacar(objetivosValidos);
        LimpiarUnidadesMuertas();
        VerificarGanador();
    }

    public void EjecutarHabilidadEspecial(Jugador jugadorAtacante, Heroe heroe, List<Unidad> objetivos)
    {
        
        if (!jugadorAtacante.Unidades.Contains(heroe)) return;
        if (!heroe.PuedeUsarHabilidad()) return;

        heroe.HabilidadEspecial(objetivos);
        heroe.RegistrarUsoHabilidad();
        LimpiarUnidadesMuertas();
        VerificarGanador();
    }

    public void EjecutarAtaqueAEdificio(Jugador jugadorAtacante, Unidad atacante, Edificio objetivo)
    {
      
        if (!jugadorAtacante.Unidades.Contains(atacante)) return;
        if (jugadorAtacante.Edificios.Contains(objetivo)) return;

        atacante.Atacar(objetivo);
        LimpiarUnidadesMuertas();
        VerificarGanador();
    }

    public void ActualizarCombate(float deltaTime, List<Unidad> todasLasUnidades)
    {
        foreach (var unidad in todasLasUnidades)
        {
            unidad.ActualizarEfectos(deltaTime);
            if (unidad is Defender defensor) defensor.ActualizarEscudo(deltaTime);
        }
    }

    // Saca de las listas de cada jugador (humano y cada oponente) las
    // unidades que ya llegaron a 0 de vida. Se llama después de CUALQUIER
    // acción de combate, así que aplica igual al jugador humano y a
    // cualquier IA, sin que cada uno tenga que acordarse de limpiar su
    // propia lista por separado.
    private void LimpiarUnidadesMuertas()
    {
        JugadorHumano?.Unidades.RemoveAll(u => u.Vida <= 0);
        if (Oponentes != null)
        {
            foreach (var oponente in Oponentes)
                oponente.Unidades.RemoveAll(u => u.Vida <= 0);
        }
    }
}