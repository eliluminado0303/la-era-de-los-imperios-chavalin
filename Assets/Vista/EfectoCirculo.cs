using UnityEngine;

// Anillo que se expande y se desvanece: marca el ÁREA real de un ataque
// (Godzilla, Jormungandr). "radioEnCeldas" ya viene en unidades de mundo
// (celdas * tamañoCelda). Genera su propia textura, no necesita sprites.
public class EfectoCirculo : MonoBehaviour
{
    public static void Crear(Vector3 centro, float radioMundo, Color color, float duracion = 0.45f)
    {
        var go = new GameObject("EfectoCirculo");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ObtenerSpriteAnillo();
        sr.color = color;
        sr.sortingOrder = 9;
        go.transform.position = centro;
        // El sprite mide 1 unidad de mundo de diámetro => escala = diámetro deseado.
        go.transform.localScale = Vector3.one * (radioMundo * 2f * 0.5f);

        var efecto = go.AddComponent<EfectoCirculo>();
        efecto.duracion = duracion;
        efecto.escalaFinal = radioMundo * 2f;
        efecto.sr = sr;
    }

    private float duracion, escalaFinal, tiempo;
    private SpriteRenderer sr;

    void Update()
    {
        tiempo += Time.deltaTime;
        float p = Mathf.Clamp01(tiempo / duracion);
        transform.localScale = Vector3.one * Mathf.Lerp(escalaFinal * 0.5f, escalaFinal, p);
        var c = sr.color; c.a = Mathf.Lerp(0.9f, 0f, p); sr.color = c;
        if (p >= 1f) Destroy(gameObject);
    }

    private static Sprite anillo;
    private static Sprite ObtenerSpriteAnillo()
    {
        if (anillo != null) return anillo;
        const int T = 128;
        var tex = new Texture2D(T, T, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float centro = (T - 1) / 2f;
        for (int y = 0; y < T; y++)
            for (int x = 0; x < T; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro)) / centro; // 0 centro .. 1 borde
                float borde = Mathf.Clamp01(1f - Mathf.Abs(d - 0.93f) / 0.07f);   // anillo fino
                float relleno = d < 0.93f ? 0.18f : 0f;                            // relleno tenue
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(borde, relleno)));
            }
        tex.Apply();
        anillo = Sprite.Create(tex, new Rect(0, 0, T, T), new Vector2(0.5f, 0.5f), T); // 1 unidad de diámetro
        return anillo;
    }
}
