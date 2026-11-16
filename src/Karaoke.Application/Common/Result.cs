namespace Karaoke.Application.Common;

public enum TipoErro
{
    NaoEncontrado,
    Invalido,
    NaoAutorizado,
    Conflito
}

public sealed record Erro(TipoErro Tipo, string Mensagem);

public class Result
{
    public Erro? Erro { get; }
    public bool Sucesso => Erro is null;

    protected Result(Erro? erro) => Erro = erro;

    public static Result Ok() => new(null);
    public static Result Falha(TipoErro tipo, string mensagem) => new(new Erro(tipo, mensagem));
    public static Result Falha(Erro erro) => new(erro);
}

public sealed class Result<T> : Result
{
    public T? Valor { get; }

    private Result(T? valor, Erro? erro) : base(erro) => Valor = valor;

    public static Result<T> Ok(T valor) => new(valor, null);
    public new static Result<T> Falha(TipoErro tipo, string mensagem) => new(default, new Erro(tipo, mensagem));
    public new static Result<T> Falha(Erro erro) => new(default, erro);
}
