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
        heroe.NotificarHabilidadEspecialRealizada(objetivos);
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

    public void EjecutarAtaqueAAldeano(Jugador jugadorAtacante, Unidad atacante, Aldeano objetivo)
    {
        if (!jugadorAtacante.Unidades.Contains(atacante)) return;
        if (jugadorAtacante.Aldeanos.Contains(objetivo)) return;

        atacante.Atacar(objetivo);
        LimpiarUnidadesMuertas();
        VerificarGanador();
    }

    // Versión sin argumentos: junta las unidades de todos los jugadores,
    // actualiza sus efectos y limpia a las que murieron por daño con el
    // tiempo (veneno, sangrado...). Llamar UNA vez por frame.
    public void ActualizarCombate(float deltaTime)
    {
        var todas = new List<Unidad>();
        if (JugadorHumano != null) todas.AddRange(JugadorHumano.Unidades);
        if (Oponentes != null) foreach (var o in Oponentes) todas.AddRange(o.Unidades);
        ActualizarCombate(deltaTime, todas);
    }

    public void ActualizarCombate(float deltaTime, List<Unidad> todasLasUnidades)
    {
        foreach (var unidad in todasLasUnidades)
        {
            if (unidad.Vida <= 0) continue; // ya muerta: no seguir aplicándole efectos
            unidad.ActualizarEfectos(deltaTime);
            if (unidad is Defender defensor) defensor.ActualizarEscudo(deltaTime);
        }

        // Una unidad puede morir por un efecto (Sangrado, Veneno, Quemadura)
        // sin que nadie haya hecho una acción de combate: hay que sacarla de
        // las listas y de su celda igual que en los ataques normales.
        LimpiarUnidadesMuertas();
    }

    // Saca de las listas de cada jugador (humano y cada oponente) las
    // unidades que ya llegaron a 0 de vida. Se llama después de CUALQUIER
    // acción de combate, así que aplica igual al jugador humano y a
    // cualquier IA, sin que cada uno tenga que acordarse de limpiar su
    // propia lista por separado.
    private void LimpiarUnidadesMuertas()
    {
        LimpiarUnidadesMuertasDe(JugadorHumano);
        LimpiarAldeanosMuertos(JugadorHumano);
        LimpiarEdificiosDestruidos(JugadorHumano);
        if (Oponentes != null)
        {
            foreach (var oponente in Oponentes)
            {
                LimpiarUnidadesMuertasDe(oponente);
                LimpiarAldeanosMuertos(oponente);
                LimpiarEdificiosDestruidos(oponente);
            }
        }
    }

    // Las unidades muertas salen de la lista del jugador Y de su celda en
    // el Mapa: antes solo salían de la lista, y la celda seguía con
    // Celda.Unidad apuntando a una unidad muerta (bloqueaba el paso, y
    // seguía siendo "objetivo" de ataques).
    private void LimpiarUnidadesMuertasDe(Jugador jugador)
    {
        if (jugador == null) return;
        foreach (var unidad in jugador.Unidades.FindAll(u => u.Vida <= 0))
            Mapa?.RetirarUnidad(unidad);
        jugador.Unidades.RemoveAll(u => u.Vida <= 0);
    }

    // Los edificios destruidos (Vida 0) salen de la lista del jugador Y de
    // su celda. Antes se quedaban para siempre en ambos lados: la celda
    // seguía bloqueada, y como VerificarGanador pide "0 edificios" para
    // eliminar a un jugador, la partida nunca podía terminar.
    private void LimpiarEdificiosDestruidos(Jugador jugador)
    {
        if (jugador == null) return;
        foreach (var edificio in jugador.RetirarEdificiosDestruidos())
            Mapa?.RetirarEdificio(edificio);
    }

    // Los aldeanos muertos salen de la lista del jugador Y de su celda en
    // el Mapa (así no queda un aldeano fantasma dibujado ni contado en la
    // población, y el HUD/IA dejan de verlo).
    private void LimpiarAldeanosMuertos(Jugador jugador)
    {
        if (jugador == null) return;
        foreach (var aldeano in jugador.Aldeanos.FindAll(a => !a.EstaVivo))
        {
            Mapa?.RetirarAldeano(aldeano);
            jugador.RetirarAldeano(aldeano);
        }
    }
}