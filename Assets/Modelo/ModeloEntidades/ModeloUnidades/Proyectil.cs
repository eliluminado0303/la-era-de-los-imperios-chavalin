using UnityEngine;
using System.Collections;

// Proyectil simple: viaja en línea recta desde origen hasta destino en
// 'duracion' segundos, mirando hacia donde va, y se autodestruye al llegar.
public class Proyectil : MonoBehaviour
{
    public static void Disparar(Vector3 origen, Vector3 destino, Sprite sprite, float duracion = 0.3f)
    {
        var go = new GameObject("Proyectil");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
        go.transform.position = origen;
        go.transform.right = (destino - origen).normalized;

        var proyectil = go.AddComponent<Proyectil>();
        proyectil.StartCoroutine(proyectil.Viajar(origen, destino, duracion));
    }

    private IEnumerator Viajar(Vector3 origen, Vector3 destino, float duracion)
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