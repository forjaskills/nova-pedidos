using Nova.Pedidos.Aplicacion.Comun;

namespace Nova.Pedidos.Pruebas.Unitarias.Comun;

public sealed class ResultadoPruebas
{
    [Fact]
    public void Un_valor_es_un_resultado_exitoso()
    {
        Resultado<int> resultado = 42;

        Assert.True(resultado.EsExito);
        Assert.Equal(42, resultado.Valor);
        Assert.Null(resultado.Error);
    }

    [Fact]
    public void Un_error_es_un_resultado_fallido()
    {
        Resultado<int> resultado = new Error("no_encontrado", "No existe.", TipoError.NoEncontrado);

        Assert.False(resultado.EsExito);
        Assert.Equal("no_encontrado", resultado.Error!.Codigo);
    }
}
