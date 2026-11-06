using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.Actions;

/// <summary>
/// Liefert die vollständige Handler-Zuordnung (Aktionstyp -> Handler) für die
/// Engine (Kapitel 5.3).
/// </summary>
public static class HandlerCatalog
{
    public static IReadOnlyDictionary<Type, IActionHandler> Create()
    {
        var registry = new RegistryHandler();
        var powercfg = new PowercfgHandler();
        var bcdedit = new BcdeditHandler();

        return new Dictionary<Type, IActionHandler>
        {
            [typeof(RegistrySetAction)] = registry,
            [typeof(RegistryDeleteAction)] = registry,
            [typeof(ServiceConfigAction)] = new ServiceHandler(),
            [typeof(TaskDisableAction)] = new TaskHandler(),
            [typeof(PowercfgValueAction)] = powercfg,
            [typeof(PowercfgSchemeAction)] = powercfg,
            [typeof(PowercfgHibernateAction)] = powercfg,
            [typeof(BcdeditSetAction)] = bcdedit,
            [typeof(BcdeditDeleteAction)] = bcdedit,
            [typeof(DnsSetAction)] = new DnsHandler(),
            [typeof(AppxRemoveAction)] = new AppxHandler(),
            [typeof(CleanerPathsAction)] = new CleanerHandler(),
            [typeof(SpiMouseAction)] = new SpiMouseHandler(),
        };
    }
}