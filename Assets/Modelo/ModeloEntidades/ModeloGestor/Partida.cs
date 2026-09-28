using System;
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

    // Sirve para cualquier unidad con habilidad activa (héroes, Assassin,
    // Defender...). "objetivos" son enemigos o aliados según la habilidad.
    public void EjecutarHabilidadEspecial(Jugador jugadorAtacante, Unidad unidad, List<Unidad> objetivos)
    {
        if (!jugadorAtacante.Unidades.Contains(unidad)) return;
        if (!unidad.TieneHabilidadActiva) return;
        if (!unidad.PuedeUsarHabilidad()) return;

        unidad.HabilidadEspecial(objetivos);
        unidad.RegistrarUsoHabilidad();
        unidad.NotificarHabilidadEspecialRealizada(objetivos);
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
        var muertas = jugador.Unidades.FindAll(u => u.Vida <= 0);
        foreach (var unidad in muertas)
            Mapa?.RetirarUnidad(unidad);
        jugador.Unidades.RemoveAll(u => u.Vida <= 0);

        // Ya fuera de la lista (así no se procesa dos veces): el caballo de
        // Troya suelta sus tropas donde cayó.
        foreach (var unidad in muertas)
            if (unidad is Avenger caballo) LiberarTropasDelCaballo(jugador, caballo);
    }

    // Se dispara cuando una unidad aparece en combate (no por entrenamiento),
    // para que quien controla a ese jugador (la IA) pueda "escucharla".
    public event Action<Jugador, Unidad> UnidadLiberada;

    // Habilidad pasiva del Avenger: al morir libera TropasALiberar Vanguards
    // en las celdas libres más cercanas. No cuentan contra el límite de
    // población (salieron "gratis" de dentro del caballo).
    private void LiberarTropasDelCaballo(Jugador jugador, Avenger caballo)
    {
        if (Mapa == null || caballo.Fila < 0) return;

        for (int i = 0; i < caballo.TropasALiberar; i++)
        {
            if (!Mapa.BuscarCeldaLibreEnRango(caballo.Fila, caballo.Columna, 3, caballo.Fila, caballo.Columna, out int fila, out int columna))
                break; // no hay lugar

            Unidad soldado = FabricaUnidades.Crear("Vanguard", caballo.Civilizacion);
            if (!Mapa.ColocarUnidad(fila, columna, soldado)) continue;

            jugador.AgregarUnidad(soldado);
            UnidadLiberada?.Invoke(jugador, soldado);
        }
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