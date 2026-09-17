using System.Collections.Generic;

public class Partida
{
    public Jugador JugadorHumano { get; set; }
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

    public bool VerificarGanador()
    {
        if (JugadorSinEdificios(JugadorHumano))
        {
            Finalizada = true;
            Ganador = Oponentes.Count > 0 ? Oponentes[0] : null;
            return true;
        }

        foreach (Jugador oponente in Oponentes)
        {
            if (JugadorSinEdificios(oponente))
            {
                Finalizada = true;
                Ganador = JugadorHumano;
                return true;
            }
        }

        return false;
    }

    private bool JugadorSinEdificios(Jugador jugador)
    {
        return jugador.Edificios.Count == 0;
    }
}