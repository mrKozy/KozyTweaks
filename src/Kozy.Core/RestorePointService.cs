using System.Diagnostics;
using Kozy.Core.Models;

namespace Kozy.Core;

public interface IRestorePointService
{
    /// <summary>
    /// Legt einen Wiederherstellungspunkt an. Gibt null zurück, wenn er nicht
    /// erstellt werden konnte – der Fehler wird dann als Warnung behandelt
    /// (Kapitel 5.1, Regel 8; T-11).
    /// </summary>
    RestorePointInfo? Create(string description);
}

/// <summary>
/// Windows-Implementierung über "Checkpoint-Computer". Das 24h-Limit eines
/// Wiederherstellungspunkts wird nicht durch Registryänderungen umgangen,
/// sondern führt lediglich zu einer Warnung (null).
/// </summary>
public sealed class WindowsRestorePointService : IRestorePointService
{
    public RestorePointInfo? Create(string description)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"Checkpoint-Computer -Description '{description}' -RestorePointType MODIFY_SETTINGS\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            process.WaitForExit();
            return process.ExitCode == 0
                ? new RestorePointInfo { Created = true, SequenceNumber = 0 }
                : null;
        }
        catch
        {
            return null;
        }
    }
}