using Kozy.Core.Models;

namespace Kozy.Core;

public sealed class DependencyException : Exception
{
    public DependencyException(string message) : base(message) { }
}

public static class DependencyResolver
{
    /// <summary>
    /// Prüft Konflikte, verlangt vorhandene Abhängigkeiten und liefert die
    /// ausgewählten Tweaks in topologischer Reihenfolge (Kapitel 5.5).
    /// </summary>
    public static IReadOnlyList<TweakManifest> Resolve(IReadOnlyCollection<TweakManifest> selected)
    {
        var byId = selected.ToDictionary(m => m.Id, m => m);
        var selectedIds = byId.Keys.ToHashSet();

        foreach (var manifest in selected)
        {
            foreach (var conflict in manifest.ConflictsWith)
            {
                if (selectedIds.Contains(conflict))
                {
                    throw new DependencyException($"Konflikt: '{manifest.Id}' widerspricht '{conflict}'.");
                }
            }
        }

        foreach (var manifest in selected)
        {
            foreach (var dep in manifest.DependsOn)
            {
                if (!selectedIds.Contains(dep))
                {
                    throw new DependencyException($"'{manifest.Id}' benötigt Abhängigkeit '{dep}', die nicht ausgewählt ist.");
                }
            }
        }

        var indegree = byId.Keys.ToDictionary(id => id, _ => 0);
        var dependents = new Dictionary<string, List<string>>();

        foreach (var manifest in selected)
        {
            foreach (var dep in manifest.DependsOn)
            {
                if (!dependents.TryGetValue(dep, out var list))
                {
                    list = [];
                    dependents[dep] = list;
                }

                list.Add(manifest.Id);
                indegree[manifest.Id]++;
            }
        }

        var queue = new Queue<string>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var order = new List<string>();

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            order.Add(id);

            if (dependents.TryGetValue(id, out var children))
            {
                foreach (var child in children)
                {
                    indegree[child]--;
                    if (indegree[child] == 0)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        if (order.Count != selected.Count)
        {
            throw new DependencyException("Zyklische Abhängigkeit erkannt.");
        }

        return order.Select(id => byId[id]).ToList();
    }
}