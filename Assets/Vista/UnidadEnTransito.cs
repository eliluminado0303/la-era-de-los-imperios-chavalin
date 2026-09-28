using UnityEngine;
using System.Collections;

// El "doble deslizante": un sprite temporal que se desliza de una celda a
// otra durante el tiempo que GestorMovimiento tarda en aplicar el
// movimiento real. Reutiliza el Animator de "Caminar" de la unidad si
// existe, para que se vea consistente con el resto de sus animaciones.
public class UnidadEnTransito : MonoBehaviour
{
    public static void Crear(Vector3 origen, Vector3 destino, RuntimeAnimatorController animatorCaminar, Sprite spriteRespaldo, Color color, float duracion, int ordenDibujo = 4)
    {
        var go = new GameObject("UnidadEnTransito");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = color;
        sr.sortingOrder = ordenDibujo; // 4 = como una unidad real; los aldeanos pasan 3 (van por debajo de la niebla, que usa 4)
        go.transform.position = origen;

        if (animatorCaminar != null)
        {
            var animator = go.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = animatorCaminar;
        }
        else
        {
            sr.sprite = spriteRespaldo;
        }

        var transito = go.AddComponent<UnidadEnTransito>();
        transito.StartCoroutine(transito.Deslizar(origen, destino, duracion));
    }

    private IEnumerator Deslizar(Vector3 origen, Vector3 destino, float duracion)
    {
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            transform.position = Vector3.Lerp(origen, destino, tiempo / duracion);
            yield return null;
        }
        Destroy(gameObject);
    }
}