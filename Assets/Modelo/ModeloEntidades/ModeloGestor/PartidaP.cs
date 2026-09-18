using System.Collections.Generic;

public partial class PartidaP
{
    public void EjecutarAtaque(Jugador jugadorAtacante, Unidad atacante, Unidad objetivo)
    {
        if (Finalizada) return;
        if (!jugadorAtacante.Unidades.Contains(atacante)) return;   // no controla esa unidad
        if (jugadorAtacante.Unidades.Contains(objetivo)) return;    // no puede atacarse a sí mismo/aliados

        atacante.Atacar(objetivo);
        VerificarGanador();
    }

    public void EjecutarAtaqueEnArea(Jugador jugadorAtacante, Unidad atacante, List<Unidad> objetivos)
    {
        if (Finalizada) return;
        if (!jugadorAtacante.Unidades.Contains(atacante)) return;

        foreach (var objetivo in objetivos)
        {
            if (jugadorAtacante.Unidades.Contains(objetivo)) continue; // se salta aliados en el área
            atacante.Atacar(objetivo);
        }
        VerificarGanador();
    }

    public void EjecutarHabilidadEspecial(Jugador jugadorAtacante, Heroe heroe, List<Unidad> objetivos)
    {
        if (Finalizada) return;
        if (!jugadorAtacante.Unidades.Contains(heroe)) return;

        heroe.HabilidadEspecial(objetivos);
        VerificarGanador();
    }

    public void EjecutarAtaqueAEdificio(Jugador jugadorAtacante, Unidad atacante, Edificio objetivo)
    {
        if (Finalizada) return;
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