using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Nova.Pedidos.Dominio.Comun;

namespace Nova.Pedidos.Dominio.Pedidos;

/// <summary>
/// El número que ven las personas: <c>PED-000123</c>. Sale de una secuencia de la base y es el que va en las rutas.
/// </summary>
public sealed record NumeroPedido : ObjetoValor
{
    private const string Prefijo = "PED-";

    private NumeroPedido(long valor) => Valor = valor;

    public long Valor { get; }

    public static NumeroPedido Desde(long valor) =>
        valor > 0 ? new NumeroPedido(valor) : throw new ArgumentOutOfRangeException(nameof(valor), valor, "El número de pedido es positivo.");

    /// <summary>Lee un texto como <c>PED-000123</c>; cualquier otra forma no es un número de pedido.</summary>
    public static bool TryParse(string? texto, [NotNullWhen(true)] out NumeroPedido? numero)
    {
        numero = null;
        if (texto is null || !texto.StartsWith(Prefijo, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var digitos = texto[Prefijo.Length..];
        if (digitos.Length == 0 || !digitos.All(char.IsAsciiDigit)
            || !long.TryParse(digitos, NumberStyles.None, CultureInfo.InvariantCulture, out var valor) || valor <= 0)
        {
            return false;
        }

        numero = new NumeroPedido(valor);
        return true;
    }

    public override string ToString() => Prefijo + Valor.ToString("D6", CultureInfo.InvariantCulture);
}
