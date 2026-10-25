namespace Nova.Pedidos.Adaptadores.Controladores;

/// <summary>Nombres de las políticas que usan los controladores; la Api las define.</summary>
public static class Politicas
{
    /// <summary>Exige el scope <c>pedidos.leer</c> en el token.</summary>
    public const string Lectura = "lectura";

    /// <summary>Exige el scope <c>pedidos.escribir</c> en el token.</summary>
    public const string Escritura = "escritura";

    /// <summary>Exige el rol <c>operador</c> o <c>admin</c>: lo que gestiona lo de todos (avanzar un pedido).</summary>
    public const string Gestion = "gestion";

    /// <summary>Límite de intentos para endpoints sensibles (login).</summary>
    public const string LimiteEstricto = "estricto";

    public const string ScopeLectura = "pedidos.leer";

    public const string ScopeEscritura = "pedidos.escribir";
}
