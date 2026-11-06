using System.Text.Json.Serialization;

namespace Kozy.Core.Models;

/// <summary>
/// Basis aller typisierten Aktionen (Kapitel 5.3). Der Diskriminator "type"
/// wird über JsonPolymorphic abgebildet; unbekannte Typen führen zu einem Fehler.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RegistrySetAction), "registry.set")]
[JsonDerivedType(typeof(RegistryDeleteAction), "registry.delete")]
[JsonDerivedType(typeof(ServiceConfigAction), "service.config")]
[JsonDerivedType(typeof(TaskDisableAction), "task.disable")]
[JsonDerivedType(typeof(PowercfgValueAction), "powercfg.value")]
[JsonDerivedType(typeof(PowercfgSchemeAction), "powercfg.scheme")]
[JsonDerivedType(typeof(PowercfgHibernateAction), "powercfg.hibernate")]
[JsonDerivedType(typeof(BcdeditSetAction), "bcdedit.set")]
[JsonDerivedType(typeof(BcdeditDeleteAction), "bcdedit.delete")]
[JsonDerivedType(typeof(DnsSetAction), "dns.set")]
[JsonDerivedType(typeof(AppxRemoveAction), "appx.remove")]
[JsonDerivedType(typeof(CleanerPathsAction), "cleaner.paths")]
[JsonDerivedType(typeof(SpiMouseAction), "spi.mouse")]
public abstract record TweakAction;

public sealed record RegistrySetAction : TweakAction
{
    public required string Hive { get; init; }   // HKCU / HKLM
    public required string Path { get; init; }
    public required string Name { get; init; }
    public RegistryValueKind Kind { get; init; } = RegistryValueKind.DWord;
    public required object Value { get; init; }
    public int View { get; init; } = 64;
}

public sealed record RegistryDeleteAction : TweakAction
{
    public required string Hive { get; init; }
    public required string Path { get; init; }
    public required string Name { get; init; }
    public int View { get; init; } = 64;
}

public sealed record ServiceConfigAction : TweakAction
{
    public required string Name { get; init; }
    public ServiceStartMode StartMode { get; init; }
    public bool Stop { get; init; }
}

public sealed record TaskDisableAction : TweakAction
{
    public required string Path { get; init; }
}

public sealed record PowercfgValueAction : TweakAction
{
    public string Scheme { get; init; } = "ACTIVE";
    public required string Subgroup { get; init; }
    public required string Setting { get; init; }
    public required int Ac { get; init; }
    public int? Dc { get; init; }
}

public sealed record PowercfgSchemeAction : TweakAction
{
    public PowerSchemeOp Op { get; init; }
    public string? SourceGuid { get; init; }
    public string? Name { get; init; }
}

public sealed record PowercfgHibernateAction : TweakAction
{
    public required bool Enabled { get; init; }
}

public sealed record BcdeditSetAction : TweakAction
{
    public required string Element { get; init; }
    public required string Value { get; init; }
}

public sealed record BcdeditDeleteAction : TweakAction
{
    public required string Element { get; init; }
}

public sealed record DnsSetAction : TweakAction
{
    public required IReadOnlyList<string> Servers { get; init; }
}

public sealed record AppxRemoveAction : TweakAction
{
    public required IReadOnlyList<string> Names { get; init; }
    public bool AllUsers { get; init; }
}

public sealed record CleanerPathsAction : TweakAction
{
    public required IReadOnlyList<string> Targets { get; init; }
}

public sealed record SpiMouseAction : TweakAction
{
    public int Speed { get; init; }
    public int Threshold1 { get; init; }
    public int Threshold2 { get; init; }
}