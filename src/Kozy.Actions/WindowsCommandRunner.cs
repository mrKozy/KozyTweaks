using System.Diagnostics;

namespace Kozy.Actions;

public sealed record CommandResult(int ExitCode, string StdOut, string StdErr);

/// <summary>
/// Führt ausschließlich die erlaubten Systemtools aus (Kapitel 5.6):
/// powercfg.exe, bcdedit.exe, sc.exe und powershell.exe.
/// </summary>
public static class WindowsCommandRunner
{
    private static readonly string[] AllowedTools = ["powercfg.exe", "bcdedit.exe", "sc.exe", "powershell.exe"];

    public static CommandResult Run(string fileName, params string[] arguments)
    {
        if (!AllowedTools.Contains(fileName, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Ausführung von '{fileName}' ist nicht erlaubt.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"'{fileName}' konnte nicht gestartet werden.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new CommandResult(process.ExitCode, stdout, stderr);
    }
}