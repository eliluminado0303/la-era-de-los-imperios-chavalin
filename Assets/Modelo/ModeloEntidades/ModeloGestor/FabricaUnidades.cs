using System;

/// <summary>Crea unidades a partir de su tipo y civilización.</summary>
public static class FabricaUnidades
{
    /// <summary>
    /// Crea una unidad válida para la civilización indicada.
    /// </summary>
    /// <exception cref="ArgumentException">El tipo de unidad no existe.</exception>
    /// <exception cref="InvalidOperationException">La unidad no pertenece a la civilización.</exception>
    public static Unidad Crear(string tipoUnidad, string civilizacion)
    {
        switch (tipoUnidad)
        {
            case "Assassin":
                if (civilizacion != "Nipones")
                    throw new InvalidOperationException("Assassin es exclusivo de Nipones.");
                return new Assassin();

            case "Avenger":
                if (civilizacion != "Griegos")
                    throw new InvalidOperationException("Avenger es exclusivo de Griegos.");
                return new Avenger();

            case "Berserker":
                if (civilizacion != "Vikingos")
                    throw new InvalidOperationException("Berserker es exclusivo de Vikingos.");
                return new Berserker();

            case "Caster":
                if (civilizacion != "Sumerios")
                    throw new InvalidOperationException("Caster es exclusivo de Sumerios.");
                return new Caster();

            case "Defender":
                return new Defender(civilizacion);

            case "Vanguard":
                return new Vanguard(civilizacion);

            case "Ranger":
                return new Ranger(civilizacion);

            default:
                throw new ArgumentException($"Tipo de unidad desconocido: {tipoUnidad}");
        }
    }
}