using Karaoke.Domain.Entities;
using Karaoke.Domain.Enums;

namespace Karaoke.Domain.Services;

/// <summary>
/// Regras puras de ordenação da fila. Sem acesso a banco, fácil de testar.
/// </summary>
public static class OrdenadorFila
{
    /// <summary>
    /// Devolve os itens que ainda aguardam, na ordem em que serão cantados.
    /// Recebe todos os itens do evento porque o revezamento precisa saber
    /// quantas vezes cada participante já cantou.
    /// </summary>
    public static IReadOnlyList<ItemFila> OrdenarAguardando(IEnumerable<ItemFila> todos, ModoFila modo)
    {
        var lista = todos.ToList();
        var aguardando = lista
            .Where(i => i.Status == StatusItemFila.Aguardando)
            .OrderBy(i => i.AdicionadoEm)
            .ThenBy(i => i.Id)
            .ToList();

        if (modo == ModoFila.Fifo || aguardando.Count <= 1)
            return aguardando;

        // Revezamento: simula a fila. A cada passo escolhe quem cantou menos
        // (cantadas + a que está cantando); empate resolve pela ordem de chegada.
        var jaCantou = lista
            .Where(i => i.Status is StatusItemFila.Cantada or StatusItemFila.Cantando)
            .GroupBy(i => i.ParticipanteId)
            .ToDictionary(g => g.Key, g => g.Count());

        var resultado = new List<ItemFila>(aguardando.Count);
        var restantes = new List<ItemFila>(aguardando);

        while (restantes.Count > 0)
        {
            var escolhido = restantes
                .OrderBy(i => jaCantou.GetValueOrDefault(i.ParticipanteId))
                .First(); // OrderBy é estável: mantém a ordem de chegada nos empates

            resultado.Add(escolhido);
            restantes.Remove(escolhido);
            jaCantou[escolhido.ParticipanteId] = jaCantou.GetValueOrDefault(escolhido.ParticipanteId) + 1;
        }

        return resultado;
    }

    public static ItemFila? ProximoItem(IEnumerable<ItemFila> todos, ModoFila modo) =>
        OrdenarAguardando(todos, modo).FirstOrDefault();
}
