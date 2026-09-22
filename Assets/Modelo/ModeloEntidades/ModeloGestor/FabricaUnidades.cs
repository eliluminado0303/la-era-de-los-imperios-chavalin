using System;

// Crea unidades a partir de su tipo y civilización.
public static class FabricaUnidades
{
    // Crea una unidad válida para la civilización indicada.
    // Lanza una excepción si el tipo no existe o no pertenece a la civilización.
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

            case "Healer":
                return new Healer(civilizacion);

            // Héroes: uno exclusivo por civilización. GestorEntrenamiento es quien
            // además valida que el jugador no tenga ya un héroe vivo en batalla.
            case "Gilgamesh":
                if (civilizacion != "Sumerios")
                    throw new InvalidOperationException("Gilgamesh es exclusivo de Sumerios.");
                return new Gilgamesh();

            case "Godzilla":
                if (civilizacion != "Nipones")
                    throw new InvalidOperationException("Godzilla es exclusivo de Nipones.");
                return new Godzilla();

            case "Jormungandr":
                if (civilizacion != "Vikingos")
                    throw new InvalidOperationException("Jormungandr es exclusivo de Vikingos.");
                return new Jormungandr();

            case "Medusa":
                if (civilizacion != "Griegos")
                    throw new InvalidOperationException("Medusa es exclusiva de Griegos.");
                return new Medusa();

            default:
                throw new ArgumentException($"Tipo de unidad desconocido: {tipoUnidad}");
        }
    }
}