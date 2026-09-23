using System.Collections.Generic;

public partial class Partida
{
    public  Jugador JugadorHumano { get; set; }
    public List<Jugador> Oponentes { get; set; }
    public Mapa Mapa { get; set; }
    public bool Finalizada { get; set; }
    public Jugador Ganador { get; set; }

    public Partida(Jugador jugadorHumano, List<Jugador> oponentes, Mapa mapa)
    {
        JugadorHumano = jugadorHumano;
        Oponentes = oponentes;
        Mapa = mapa;
        Finalizada = false;
        Ganador = null;
    }

    // El humano pierde si se queda sin edificios: gana el primer oponente
    // vivo (no importa cuál exactamente, ya perdió contra "las IA" en general).
    // Pero el humano solo gana cuando TODOS los oponentes están eliminados —
    // antes esto terminaba el juego apenas UNA de las 3 IA perdía sus
    // edificios, lo cual estaba mal para 1 humano vs 3 IA.
    public bool VerificarGanador()
    {
        if (JugadorSinEdificios(JugadorHumano))
        {
            Finalizada = true;
            Ganador = PrimerOponenteVivo();
            return true;
        }

        bool todosLosOponentesEliminados = true;
        foreach (Jugador oponente in Oponentes)
        {
            if (!JugadorSinEdificios(oponente))
            {
                todosLosOponentesEliminados = false;
                break;
            }
        }

        if (todosLosOponentesEliminados && Oponentes.Count > 0)
        {
            Finalizada = true;
            Ganador = JugadorHumano;
            return true;
        }

        return false;
    }

    private Jugador PrimerOponenteVivo()
    {
        foreach (Jugador oponente in Oponentes)
            if (!JugadorSinEdificios(oponente)) return oponente;
        return Oponentes.Count > 0 ? Oponentes[0] : null;
    }

    private bool JugadorSinEdificios(Jugador jugador)
    {
        return jugador.Edificios.Count == 0;
    }
}