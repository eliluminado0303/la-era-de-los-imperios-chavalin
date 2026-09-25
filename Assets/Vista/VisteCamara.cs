using UnityEngine;

// Controla la cámara: flechas/WASD para desplazarse, rueda del mouse para
// acercar/alejar. No usa clic del mouse a propósito, para no interferir
// con VistaInput (que usa clic izquierdo para las acciones del juego).
public class VistaCamara : MonoBehaviour
{
    public float velocidadDesplazamiento = 10f;
    public float velocidadZoom = 5f;
    public float zoomMinimo = 3f;
    public float zoomMaximo = 20f;

    private Camera camara;

    void Awake()
    {
        camara = GetComponent<Camera>();
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D o flechas izquierda/derecha
        float vertical = Input.GetAxisRaw("Vertical");     // W/S o flechas arriba/abajo
        transform.position += new Vector3(horizontal, vertical, 0) * velocidadDesplazamiento * Time.deltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            camara.orthographicSize = Mathf.Clamp(camara.orthographicSize - scroll * velocidadZoom, zoomMinimo, zoomMaximo);
        }
    }
}