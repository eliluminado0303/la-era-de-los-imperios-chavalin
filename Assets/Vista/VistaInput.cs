using UnityEngine;

// Traduce los clics del jugador humano en llamadas a sus controladores
// (siempre el índice 0 en las listas de ControladorPartida). Selección en
// dos clics: primero tu unidad, luego el destino/objetivo.
public class VistaInput : MonoBehaviour
{
    public VistaMapa vistaMapa;

    void Awake()
    {
        if (vistaMapa == null) vistaMapa = FindObjectOfType<VistaMapa>();
    }

    void Update()
    {
        if (vistaMapa == null) return;
        if (vistaMapa.Partida == null) return;

        // Click derecho: cancela una construcción pendiente sin gastar el
        // click en otra cosa (mover unidad, atacar, etc.).
        if (Input.GetMouseButtonDown(1) && vistaMapa.EdificioPendienteDeColocar != null)
        {
            vistaMapa.EdificioPendienteDeColocar = null;
            Debug.Log("Construcción cancelada.");
            return;
        }
        // Tecla E: si tienes un héroe seleccionado y su habilidad está
        // lista, entra en "modo puntería" — el próximo clic decide el
        // punto donde se lanza la habilidad, en vez de mover/atacar normal.
        if (Input.GetKeyDown(KeyCode.E) && vistaMapa.FilaSeleccionada != null)
        {
            Celda celdaSeleccionada = vistaMapa.Partida.Mapa.ObtenerCelda(vistaMapa.FilaSeleccionada.Value, vistaMapa.ColumnaSeleccionada.Value);
            if (celdaSeleccionada?.Unidad is Heroe heroeSeleccionado)
            {
                if (heroeSeleccionado.PuedeUsarHabilidad())
                {
                    vistaMapa.ModoHabilidadEspecial = true;
                    Debug.Log("Modo habilidad especial: hacé click en el punto donde quieres lanzarla.");
                }
                else
                {
                    Debug.Log($"Habilidad en recarga: faltan {heroeSeleccionado.TiempoRestanteRecarga():0.0}s.");
                }
            }
        }

        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 mundo = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mundo.z = 0;
        if (!vistaMapa.MundoACelda(mundo, out int fila, out int columna)) return;

        var controladorMapaHumano = vistaMapa.Partida.ControladoresMapa[0];
        var controladorCombateHumano = vistaMapa.Partida.ControladoresCombate[0];
        Jugador jugadorHumano = controladorMapaHumano.Jugador;
        Celda celda = vistaMapa.Partida.Mapa.ObtenerCelda(fila, columna);

        // Si hay un edificio esperando ubicación (el jugador clickeó
        // "Construir Cuartel" u otro botón similar en el HUD), este click
        // decide DÓNDE se coloca. Corta acá: no participa de la selección
        // de unidades ni de aldeanos mientras haya una construcción pendiente.
        if (vistaMapa.EdificioPendienteDeColocar != null)
        {
            var edificio = vistaMapa.EdificioPendienteDeColocar;

            if (!vistaMapa.EstaExplorada(fila, columna))
            {
                Debug.Log("No se puede construir en una zona todavía no explorada.");
                return;
            }

            bool colocado = controladorMapaHumano.SolicitarConstruccion(fila, columna, edificio);
            Debug.Log(colocado
                ? $"{edificio.Nombre} en construcción en ({fila},{columna})."
                : "No se pudo construir ahí (celda ocupada, agua, o recursos insuficientes).");
            if (colocado) vistaMapa.EdificioPendienteDeColocar = null;
            return;
        }
                // Si estás en modo habilidad especial, este click decide el punto
        // de lanzamiento — corta acá, no sigue con selección normal.
        if (vistaMapa.ModoHabilidadEspecial && vistaMapa.FilaSeleccionada != null)
        {
            int filaHeroe = vistaMapa.FilaSeleccionada.Value;
            int columnaHeroe = vistaMapa.ColumnaSeleccionada.Value;
            bool lanzado = controladorCombateHumano.SolicitarHabilidadEspecial(filaHeroe, columnaHeroe, fila, columna);

            if (lanzado)
            {
                Celda celdaHeroe = vistaMapa.Partida.Mapa.ObtenerCelda(filaHeroe, columnaHeroe);
                if (celdaHeroe?.Unidad is Heroe heroeQueLanzo && heroeQueLanzo.AreaHabilidad.Forma == FormaArea.Linea)
                {
                    Vector3 origen = new Vector3(columnaHeroe * vistaMapa.tamañoCelda, -filaHeroe * vistaMapa.tamañoCelda, 0);
                    Vector3 destino = new Vector3(columna * vistaMapa.tamañoCelda, -fila * vistaMapa.tamañoCelda, 0);
                    EfectoRayo.Crear(origen, (destino - origen).normalized, heroeQueLanzo.AreaHabilidad.Tamaño, heroeQueLanzo.AreaHabilidad.Ancho, new Color(1f, 0.85f, 0.2f));
                }
                Debug.Log("¡Habilidad especial lanzada!");
            }
            else
            {
                Debug.Log("No se pudo lanzar (fuera de rango, o sin objetivos válidos ahí).");
            }

            vistaMapa.ModoHabilidadEspecial = false;
            vistaMapa.FilaSeleccionada = null;
            vistaMapa.ColumnaSeleccionada = null;
            return;
        }

        // Si hay un aldeano elegido desde la lista del HUD, este click decide
        // a qué recurso lo mandamos — corta acá y no sigue con la selección
        // normal de unidades militares (los Aldeanos no son Unidad, no
        // participan de esa lógica).
        if (vistaMapa.AldeanoSeleccionado != null)
        {
            if (celda?.Recurso != null && !celda.Recurso.EstaAgotado())
            {
                bool enviado = controladorMapaHumano.SolicitarRecoleccion(vistaMapa.AldeanoSeleccionado, fila, columna);
                Debug.Log(enviado ? $"{vistaMapa.AldeanoSeleccionado.Nombre} va a recolectar." : "No se pudo enviar al aldeano (¿ya está ocupado?).");
                if (enviado) vistaMapa.AldeanoSeleccionado = null;
            }
            else if (vistaMapa.Partida.Mapa.CeldaLibre(fila, columna) && vistaMapa.EstaExplorada(fila, columna))
            {
                // Celda vacía de tierra: el aldeano simplemente se desplaza hasta ahí.
                bool movido = controladorMapaHumano.SolicitarMovimientoAldeano(vistaMapa.AldeanoSeleccionado, fila, columna);
                Debug.Log(movido ? $"{vistaMapa.AldeanoSeleccionado.Nombre} se desplaza a ({fila},{columna})." : "No se pudo mover al aldeano.");
                if (movido) vistaMapa.AldeanoSeleccionado = null;
            }
            else
            {
                Debug.Log("Elige un árbol, una mina o un rebaño para recolectar, o una celda libre para moverlo.");
            }
            return;
        }

        if (vistaMapa.FilaSeleccionada == null)
        {
            if (celda?.Unidad != null && jugadorHumano.Unidades.Contains(celda.Unidad))
            {
                vistaMapa.FilaSeleccionada = fila;
                vistaMapa.ColumnaSeleccionada = columna;
                Debug.Log($"Unidad seleccionada en ({fila},{columna})");
            }
            return;
        }

        int fOrigen = vistaMapa.FilaSeleccionada.Value;
        int cOrigen = vistaMapa.ColumnaSeleccionada.Value;

        if (fila == fOrigen && columna == cOrigen)
        {
            vistaMapa.FilaSeleccionada = null;
            vistaMapa.ColumnaSeleccionada = null;
            return;
        }

               Celda celdaOrigen = vistaMapa.Partida.Mapa.ObtenerCelda(fOrigen, cOrigen);
        Unidad unidadOrigen = celdaOrigen?.Unidad;

        bool accionValida;

        // Healer + clic en un aliado propio = curar, no atacar.
        if (unidadOrigen is Healer && celda?.Unidad != null && jugadorHumano.Unidades.Contains(celda.Unidad))
        {
            accionValida = controladorCombateHumano.SolicitarCuracion(fOrigen, cOrigen, fila, columna, buff: false);
            if (accionValida)
            {
                Vector3 posicionDestino = new Vector3(columna * vistaMapa.tamañoCelda, -fila * vistaMapa.tamañoCelda, 0);
                EfectoCuracion.Crear(posicionDestino, new Color(0.3f, 1f, 0.4f)); // verde
            }
            else
            {
                Debug.Log("No se pudo curar (¿fuera de rango del Healer?).");
            }
        }
        else if (celda?.Unidad != null || celda?.Edificio != null || (celda?.Aldeano != null && !jugadorHumano.Aldeanos.Contains(celda.Aldeano)))
        {
            // Si el objetivo está fuera de alcance, la unidad camina hacia él y
            // ataca al llegar (ver ControladorCombate.SolicitarAtaqueOAcercarse).
            var resultadoAtaque = controladorCombateHumano.SolicitarAtaqueOAcercarse(fOrigen, cOrigen, fila, columna);
            accionValida = resultadoAtaque != ResultadoOrdenAtaque.Invalida;
            if (resultadoAtaque == ResultadoOrdenAtaque.Acercandose) Debug.Log("La unidad se acerca al objetivo para atacar.");

            // Si quien atacó es un Ranger (Rango > 1, cualquier unidad a
            // distancia en general), dispara la flecha visual. (Cuando la
            // unidad todavía se está acercando, la flecha sale después, al
            // ejecutarse el ataque: ver VistaMapa.AlAtaqueDiferido.)
            if (resultadoAtaque == ResultadoOrdenAtaque.Atacando && unidadOrigen != null && unidadOrigen.Rango > 1)
            {
                Vector3 posicionOrigen = new Vector3(cOrigen * vistaMapa.tamañoCelda, -fOrigen * vistaMapa.tamañoCelda, 0);
                Vector3 posicionDestino = new Vector3(columna * vistaMapa.tamañoCelda, -fila * vistaMapa.tamañoCelda, 0);
                Proyectil.Disparar(posicionOrigen, posicionDestino, vistaMapa.spriteFlecha);
            }
        }
        else
        {
            controladorCombateHumano.CancelarPersecucion(unidadOrigen); // una orden de mover anula un acercamiento en curso
            accionValida = controladorMapaHumano.SolicitarMovimiento(fOrigen, cOrigen, fila, columna);
            // Solo se dibuja el "doble" deslizante si la orden fue aceptada.
            if (accionValida) vistaMapa.IniciarMovimientoVisual(unidadOrigen, fOrigen, cOrigen, fila, columna);
        }

        // SIN ESTO, FilaSeleccionada/ColumnaSeleccionada se quedan apuntando
        // para siempre a la celda de ORIGEN de la última orden: el próximo
        // click ya no entra en "elegir unidad" (más abajo), sino que asume
        // que sigue habiendo una selección activa y trata cualquier click
        // futuro como si fuera el destino de esa selección vieja. Por eso
        // una unidad "solo se podía seleccionar una vez".
        if (!accionValida) Debug.Log("Acción no válida (fuera de rango, objetivo aliado, celda vacía sin nada que atacar, etc.)");

        vistaMapa.FilaSeleccionada = null;
        vistaMapa.ColumnaSeleccionada = null;
    }
}