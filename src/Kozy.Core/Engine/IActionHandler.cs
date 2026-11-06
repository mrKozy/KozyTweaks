using Kozy.Core.Models;

namespace Kozy.Core;

/// <summary>
/// Abstraktion über einen konkreten Aktionen-Typ (Kapitel 5.3, T-10).
/// Implementierungen kommen in Phase 2 (Kozy.Actions).
/// </summary>
public interface IActionHandler
{
    void Validate(TweakAction action);

    /// <summary>Erfasst den Ist-Zustand vor der Änderung (für Rücknahme).</summary>
    object? Capture(TweakAction action);

    void Apply(TweakAction action);

    bool Verify(TweakAction action);

    void Restore(TweakAction action, object? captured);

    /// <summary>Meldet den aktuellen Zustand für die UI-Schalter (Kapitel 5.1, Regel 5).</summary>
    TweakStatus Detect(TweakAction action);
}