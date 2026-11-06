using System.Text.Json.Serialization;

namespace Kozy.Core.Models;

[JsonConverter(typeof(LowerCaseEnumConverter<Tier>))]
public enum Tier
{
    Basic,
    Pro,
    Extreme,
    Precision,
}

[JsonConverter(typeof(LowerCaseEnumConverter<Risk>))]
public enum Risk
{
    Safe,
    Moderate,
    Advanced,
    Experimental,
}

[JsonConverter(typeof(LowerCaseEnumConverter<DeviceClass>))]
public enum DeviceClass
{
    Desktop,
    Laptop,
}

[JsonConverter(typeof(LowerCaseEnumConverter<GpuVendor>))]
public enum GpuVendor
{
    Nvidia,
    Amd,
    Intel,
}

[JsonConverter(typeof(LowerCaseEnumConverter<BenchmarkHint>))]
public enum BenchmarkHint
{
    Frametime,
    Latency,
    Network,
    None,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RegistryValueKind
{
    DWord,
    QWord,
    String,
    ExpandString,
    Binary,
    MultiString,
}

[JsonConverter(typeof(LowerCaseEnumConverter<ServiceStartMode>))]
public enum ServiceStartMode
{
    Auto,
    Manual,
    Disabled,
}

[JsonConverter(typeof(LowerCaseEnumConverter<PowerSchemeOp>))]
public enum PowerSchemeOp
{
    Duplicate,
    SetActive,
}

/// <summary>Erkennungszustand eines Tweaks (Kapitel 5.1, Regel 5).</summary>
public enum TweakStatus
{
    Applied,
    NotApplied,
    Partial,
    Unknown,
}