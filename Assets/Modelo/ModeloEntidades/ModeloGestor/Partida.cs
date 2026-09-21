using System.Collections.Generic;

// Operaciones de combate que se ejecutan dentro de una partida.
// Nota: "Finalizada" y "VerificarGanador()" viven en la otra mitad de esta
// clase partial (Modelo7/ModeloMapa/Partida.cs). No se redeclaran aquí para
// evitar el conflicto de compilación; en su lugar, este archivo llama a
// VerificarGanador() cada vez que el combate puede haber terminado la partida.
public partial class Partida
{
    // Ejecuta un ataque individual validando que la unidad pertenezca al jugador.
    public void EjecutarAtaque(Jugador jugadorAtacante, Unidad atacante, Unidad objetivo)
    {
       
        if (!jugadorAtacante.Unidades.Contains(atacante)) return;   // no controla esa unidad
        if (jugadorAtacante.Unidades.Contains(objetivo)) return;    // no puede atacarse a sí mismo/aliados

        atacante.Atacar(objetivo);
        VerificarGanador();
    }

    // Filtra aliados y objetivos nulos antes de delegar el ataque en área a la unidad.
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
        VerificarGanador();
    }

    // Ejecuta una habilidad especial contra los objetivos seleccionados.
    public void EjecutarHabilidadEspecial(Jugador jugadorAtacante, Heroe heroe, List<Unidad> objetivos)
    {
        
        if (!jugadorAtacante.Unidades.Contains(heroe)) return;

        heroe.HabilidadEspecial(objetivos);
        VerificarGanador();
    }

    // Permite a una unidad atacar una estructura enemiga.
    public void EjecutarAtaqueAEdificio(Jugador jugadorAtacante, Unidad atacante, Edificio objetivo)
    {
      
        if (!jugadorAtacante.Unidades.Contains(atacante)) return;
        if (jugadorAtacante.Edificios.Contains(objetivo)) return;

        atacante.Atacar(objetivo);
        VerificarGanador();
    }

    // Se llama UNA vez por frame desde el Controller — nada de Task aquí
    public void ActualizarCombate(float deltaTime, List<Unidad> todasLasUnidades)
    {
        foreach (var unidad in todasLasUnidades)
        {
            unidad.ActualizarEfectos(deltaTime);
            if (unidad is Defender defensor) defensor.ActualizarEscudo(deltaTime);
        }
    }
}