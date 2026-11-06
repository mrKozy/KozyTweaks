namespace Kozy.Core.Models;

/// <summary>Diskriminator-Namen der Aktionstypen (Kapitel 5.3).</summary>
public static class ActionTypeNames
{
    public const string RegistrySet = "registry.set";
    public const string RegistryDelete = "registry.delete";
    public const string ServiceConfig = "service.config";
    public const string TaskDisable = "task.disable";
    public const string PowercfgValue = "powercfg.value";
    public const string PowercfgScheme = "powercfg.scheme";
    public const string PowercfgHibernate = "powercfg.hibernate";
    public const string BcdeditSet = "bcdedit.set";
    public const string BcdeditDelete = "bcdedit.delete";
    public const string DnsSet = "dns.set";
    public const string AppxRemove = "appx.remove";
    public const string CleanerPaths = "cleaner.paths";
    public const string SpiMouse = "spi.mouse";

    public static string Of(TweakAction action) => action switch
    {
        RegistrySetAction => RegistrySet,
        RegistryDeleteAction => RegistryDelete,
        ServiceConfigAction => ServiceConfig,
        TaskDisableAction => TaskDisable,
        PowercfgValueAction => PowercfgValue,
        PowercfgSchemeAction => PowercfgScheme,
        PowercfgHibernateAction => PowercfgHibernate,
        BcdeditSetAction => BcdeditSet,
        BcdeditDeleteAction => BcdeditDelete,
        DnsSetAction => DnsSet,
        AppxRemoveAction => AppxRemove,
        CleanerPathsAction => CleanerPaths,
        SpiMouseAction => SpiMouse,
        _ => throw new NotSupportedException($"Unbekannter Aktionstyp '{action.GetType().Name}'."),
    };
}