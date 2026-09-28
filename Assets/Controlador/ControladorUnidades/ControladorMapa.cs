using System.Collections.Generic;

public class ControladorMapa {

    public Jugador Jugador { get; private set; }
    public Mapa Mapa { get; private set; }
    public Partida Partida { get; private set; }

    private readonly GestorRecoleccion gestorRecoleccion;
    private readonly GestorConstruccion gestorConstruccion;
    private readonly GestorMovimiento gestorMovimiento;
    private readonly GestorArchivos gestorArchivos;
    private readonly ControladorIA controladorIA; // null si este jugador es el humano

    // mapaCompartido y partidaCompartida se crean UNA sola vez (en el punto
    // de arranque) y se pasan a las 4 instancias de ControladorMapa — así
    // los 4 jugadores ven el mismo tablero y la misma condición de victoria,
    // en vez de cada uno tener su propia copia desincronizada.
    
    public ControladorMapa(Jugador jugador, Mapa mapaCompartido, Partida partidaCompartida, ControladorIA controladorIA = null) {
        Jugador = jugador;
        Mapa = mapaCompartido;
        Partida = partidaCompartida;
        this.controladorIA = controladorIA;

        gestorRecoleccion = new GestorRecoleccion();
        gestorConstruccion = new GestorConstruccion();
        gestorMovimiento = new GestorMovimiento();
        gestorArchivos = new GestorArchivos();
    }

    public void IniciarPartida() {
        gestorArchivos.GuardarConfiguracion(Mapa, Jugador);
    }

    // ---------------------------------------------------------------
    // Recolección: el aldeano CAMINA hasta el recurso y recién ahí empieza
    // a recolectar. Mientras camina ya figura como Ocupado (así ni el HUD,
    // ni el deambular, ni la IA le dan otra orden encima).
    // ---------------------------------------------------------------

    private class OrdenRecoleccion
    {
        public int filaRecurso, columnaRecurso;
        public int filaDestino = -1, columnaDestino = -1; // la celda hacia la que salió caminando
        public int intentos;
    }

    // Órdenes de recolección en curso (solo se tocan desde el hilo principal).
    private readonly Dictionary<Aldeano, OrdenRecoleccion> ordenesRecoleccion = new Dictionary<Aldeano, OrdenRecoleccion>();

    // Cuántos desplazamientos tiene en vuelo cada aldeano. Se incrementa al
    // pedirlos y se decrementa al llegar el resultado (en ActualizarResultados).
    private readonly Dictionary<Aldeano, int> movimientosEnCurso = new Dictionary<Aldeano, int>();

    public bool SolicitarRecoleccion(Aldeano aldeano, int fila, int columna) {
        if (aldeano == null || !aldeano.EstaVivo) return false;
        if (!Jugador.Aldeanos.Contains(aldeano)) return false;

        Celda celda = Mapa.ObtenerCelda(fila, columna);
        if (celda == null || celda.Recurso == null || celda.Recurso.EstaAgotado()) return false;

        // Sin posición todavía (no se le encontró lugar al entrenarlo): no
        // hay desde dónde caminar, así que recolecta desde donde esté.
        if (aldeano.Fila < 0 || aldeano.Columna < 0)
            return gestorRecoleccion.IniciarRecoleccionDesdeMapa(aldeano, Mapa, fila, columna, Jugador);

        ordenesRecoleccion[aldeano] = new OrdenRecoleccion { filaRecurso = fila, columnaRecurso = columna };
        aldeano.Ocupado = true;

        // Si justo estaba en medio de otro desplazamiento (por ejemplo
        // deambulando), se espera a que llegue antes de salir hacia el recurso.
        if (!EstaMoviendose(aldeano)) AvanzarOrdenRecoleccion(aldeano);
        return true;
    }

    private bool EstaMoviendose(Aldeano aldeano)
        => movimientosEnCurso.TryGetValue(aldeano, out int cantidad) && cantidad > 0;

    // Un paso de la orden: si ya está junto al recurso, empieza a
    // recolectar; si no, sale caminando hacia una celda pegada al recurso.
    private void AvanzarOrdenRecoleccion(Aldeano aldeano)
    {
        if (!ordenesRecoleccion.TryGetValue(aldeano, out var orden)) return;

        Celda celda = Mapa.ObtenerCelda(orden.filaRecurso, orden.columnaRecurso);
        bool ordenValida = aldeano.EstaVivo && celda != null && celda.Recurso != null && !celda.Recurso.EstaAgotado();
        if (!ordenValida)
        {
            ordenesRecoleccion.Remove(aldeano);
            aldeano.Ocupado = false; // otro aldeano ya se llevó el recurso, o murió
            return;
        }

        int distancia = System.Math.Max(System.Math.Abs(aldeano.Fila - orden.filaRecurso), System.Math.Abs(aldeano.Columna - orden.columnaRecurso));

        // Ya está junto al recurso, o llegó a la celda que se le había
        // elegido (puede estar a 2-3 casillas si las de al lado estaban
        // ocupadas), o ya probó varias veces sin lograrlo: recolecta desde ahí.
        bool llegoAlDestino = aldeano.Fila == orden.filaDestino && aldeano.Columna == orden.columnaDestino;
        if (distancia <= 1 || llegoAlDestino || orden.intentos >= 3)
        {
            ordenesRecoleccion.Remove(aldeano);
            if (!gestorRecoleccion.IniciarRecoleccionDesdeMapa(aldeano, Mapa, orden.filaRecurso, orden.columnaRecurso, Jugador))
                aldeano.Ocupado = false;
            return;
        }

        orden.intentos++;
        BuscarCeldaJuntoAlRecurso(aldeano, orden.filaRecurso, orden.columnaRecurso, out int filaDestino, out int columnaDestino);
        orden.filaDestino = filaDestino;
        orden.columnaDestino = columnaDestino;
        IniciarDesplazamientoAldeano(aldeano, filaDestino, columnaDestino);
    }

    // Elige dónde pararse para recolectar: la celda más cercana al recurso
    // (anillos de 1 a 3 casillas) donde no haya OTRO aldeano — cada celda
    // solo dibuja a uno. Si no hay ninguna, va a la del propio recurso.
    private void BuscarCeldaJuntoAlRecurso(Aldeano aldeano, int filaRecurso, int columnaRecurso, out int fila, out int columna)
    {
        if (!Mapa.BuscarCeldaParaAldeano(filaRecurso, columnaRecurso, aldeano, 1, 3, aldeano.Fila, aldeano.Columna, out fila, out columna))
        {
            fila = filaRecurso;
            columna = columnaRecurso;
        }
    }

    // La Vista se suscribe a esto para poder dibujar al aldeano deslizándose
    // de una celda a otra durante el tiempo que tarda el desplazamiento real
    // (mismo papel que IniciarMovimientoVisual para las unidades militares).
    // Argumentos: aldeano, fila/columna de origen, fila/columna de destino y
    // duración en segundos (depende de la distancia y de su velocidad).
    public event System.Action<Aldeano, int, int, int, int, float> AldeanoEmpezoAMoverse;

    // Desplaza a un aldeano libre hasta otra celda. Devuelve false (sin
    // hacer nada) si está muerto, ocupado recolectando, todavía no tiene
    // posición, o el destino no es una celda de tierra válida.
    public bool SolicitarMovimientoAldeano(Aldeano aldeano, int filaDestino, int columnaDestino) {
        if (aldeano == null || !aldeano.EstaVivo || aldeano.Ocupado) return false;
        if (!Jugador.Aldeanos.Contains(aldeano)) return false;
        if (aldeano.Fila < 0 || aldeano.Columna < 0) return false;
        if (!Mapa.EsTierra(filaDestino, columnaDestino)) return false;
        if (aldeano.Fila == filaDestino && aldeano.Columna == columnaDestino) return false;
        var otro = Mapa.ObtenerCelda(filaDestino, columnaDestino).Aldeano;
        if (otro != null && otro != aldeano && otro.EstaVivo) return false; // ya hay otro aldeano ahí

        IniciarDesplazamientoAldeano(aldeano, filaDestino, columnaDestino);
        return true;
    }

    // Camino común (orden del jugador, deambular, ir a recolectar): calcula
    // cuánto tarda según la distancia y la Velocidad del aldeano (mínimo 1s),
    // lanza el desplazamiento y avisa a la Vista.
    private void IniciarDesplazamientoAldeano(Aldeano aldeano, int filaDestino, int columnaDestino) {
        int filaOrigen = aldeano.Fila;
        int columnaOrigen = aldeano.Columna;

        int distancia = System.Math.Max(System.Math.Abs(filaDestino - filaOrigen), System.Math.Abs(columnaDestino - columnaOrigen));
        float duracion = System.Math.Max(1f, distancia / System.Math.Max(1f, aldeano.Velocidad));

        movimientosEnCurso[aldeano] = (movimientosEnCurso.TryGetValue(aldeano, out int enCurso) ? enCurso : 0) + 1;
        gestorMovimiento.IniciarMovimientoAldeano(Mapa, aldeano, filaDestino, columnaDestino, Jugador.Nombre, (int)(duracion * 1000f));
        AldeanoEmpezoAMoverse?.Invoke(aldeano, filaOrigen, columnaOrigen, filaDestino, columnaDestino, duracion);
    }

    // Construcción de un edificio en una celda (la usan la Vista al colocar
    // y la IA al construir su Cuartel). Devuelve false si no se pudo
    // (recursos insuficientes, celda ocupada, etc.).
    public bool SolicitarConstruccion(int fila, int columna, Edificio edificio) {
        return gestorConstruccion.IniciarConstruccion(Jugador, Mapa, fila, columna, edificio);
    }

    // Movimiento de una UNIDAD militar (los aldeanos usan
    // SolicitarMovimientoAldeano, más abajo). Devuelve false si la orden se
    // rechaza al instante: la unidad ya está viajando, o el destino está
    // ocupado / es agua / lo tiene reservado otro movimiento.
    public bool SolicitarMovimiento(int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino) {
        return gestorMovimiento.IniciarMovimiento(Mapa, filaOrigen, columnaOrigen, filaDestino, columnaDestino, Jugador.Nombre);
    }

    public bool VerificarFinDePartida() {
        bool termino = Partida.VerificarGanador();
        if (termino) {
            gestorArchivos.GuardarResultadoFinal(Partida);
        }
        return termino;
    }

    public void ActualizarResultados() {
        // OJO: jugador.AgregarRecurso(...) y aldeano.Ocupado = false ya se
        // aplican DENTRO de GestorRecoleccion.RecolectarEnSegundoPlano,
        // antes de encolar este resultado — o sea que la recolección en sí
        // ya funciona aunque este bucle esté vacío. Lo que sigue es solo
        // para poder reaccionar/loguear cuándo un ciclo de recolección
        // termina, sin sumar el recurso una segunda vez.
        while (gestorRecoleccion.ResultadosPendientes.TryDequeue(out ResultadoRecoleccion resultado)) {
            gestorArchivos.RegistrarEvento(Jugador.Nombre, "Recoleccion",
                $"{resultado.NombreAldeano} entregó {resultado.Cantidad} de {resultado.TipoRecurso}.");
        }

            while (gestorConstruccion.ResultadosPendientes.TryDequeue(out ResultadoConstruccion resultado)) {
            if (resultado.Exitoso) {
                controladorIA?.ObservarEdificio(resultado.Edificio);
                if (resultado.Edificio is Casa casa) Jugador.LimitePoblacion += casa.PoblacionQueOtorga;
            }
        }

        while (gestorMovimiento.ResultadosPendientes.TryDequeue(out ResultadoMovimiento resultado)) {
            if (resultado.Aldeano != null) {
                // Terminó un desplazamiento de aldeano: si iba camino a un
                // recurso, ahora toca empezar a recolectar (o reintentar).
                var aldeano = resultado.Aldeano;
                if (movimientosEnCurso.TryGetValue(aldeano, out int enCurso)) {
                    if (enCurso <= 1) movimientosEnCurso.Remove(aldeano);
                    else movimientosEnCurso[aldeano] = enCurso - 1;
                }
                if (!EstaMoviendose(aldeano)) AvanzarOrdenRecoleccion(aldeano);
            }
            else if (resultado.Exitoso && resultado.Unidad != null) {
                controladorIA?.AlTerminarMovimiento(resultado.Unidad, resultado.Fila, resultado.Columna);
            }
        }
    }
}