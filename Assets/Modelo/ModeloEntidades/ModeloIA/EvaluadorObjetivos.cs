using System.Collections.Generic;
using System.Linq;

// Decide qué unidad enemiga conviene atacar. No es un cálculo perfecto a
// propósito: pondera varios factores (amenaza, rematar heridos, priorizar
// soporte) y deja un margen de error para que la IA se sienta táctica y
// vencible, no infalible.
public static class EvaluadorObjetivos
{
    private const float PESO_AMENAZA = 1.0f;
    private const float BONUS_REMATE = 100f;   // rematar a quien está casi muerto
    private const float BONUS_SOPORTE = 80f;   // priorizar Healers/Heroes enemigos
    private const float PROBABILIDAD_ERROR = 0.15f; // 15%: no elige el mejor, sino el 2do

    public static Unidad ElegirObjetivo(List<Unidad> candidatos)
    {
        if (candidatos == null || candidatos.Count == 0) return null;

        var ordenados = candidatos.OrderByDescending(Puntaje).ToList();

        if (ordenados.Count > 1 && Aleatorio.Valor() < PROBABILIDAD_ERROR)
            return ordenados[1];

        return ordenados[0];
    }

    private static float Puntaje(Unidad u)
    {
        float puntaje = u.Ataque * PESO_AMENAZA;

        float vidaProporcion = u.VidaMaxima > 0 ? (float)u.Vida / u.VidaMaxima : 1f;
        if (vidaProporcion <= 0.3f) puntaje += BONUS_REMATE;

        if (u is Healer || u is Heroe) puntaje += BONUS_SOPORTE;

        return puntaje;
    }
}