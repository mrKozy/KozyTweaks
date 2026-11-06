using Kozy.Core;
using Kozy.Core.Models;

return await MainAsync(args);

static async Task<int> MainAsync(string[] args)
{
    if (args.Length < 1)
    {
        PrintUsage();
        return 1;
    }

    var command = args[0];

    return command switch
    {
        "validate" => Validate(args.Length >= 2 ? args[1] : string.Empty),
        "build" => Build(args.Skip(1).ToArray()),
        "update" => await Update(args.Skip(1).ToArray()),
        _ => Unknown(command),
    };
}

static int Validate(string target)
{
    if (string.IsNullOrWhiteSpace(target))
    {
        PrintUsage();
        return 1;
    }

    var paths = Directory.Exists(target)
        ? Directory.EnumerateFiles(target, "*.json", SearchOption.AllDirectories).ToArray()
        : new[] { target };

    var errorCount = 0;
    var manifestCount = 0;

    foreach (var file in paths)
    {
        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"Datei nicht gefunden: {file}");
            return 2;
        }

        try
        {
            var manifest = ManifestSerializer.LoadFromFile(file);
            manifestCount++;
            foreach (var error in ManifestValidator.Validate(manifest))
            {
                errorCount++;
                Console.Error.WriteLine($"{file}: {error.Field}: {error.Message}");
            }
        }
        catch (Exception ex)
        {
            errorCount++;
            Console.Error.WriteLine($"{file}: {ex.Message}");
        }
    }

    Console.WriteLine($"{manifestCount} Manifest(e) geprüft, {errorCount} Fehler.");
    return errorCount == 0 ? 0 : 1;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unbekannter Befehl '{command}'.");
    PrintUsage();
    return 1;
}

static int Build(string[] args)
{
    if (args.Length < 1)
    {
        PrintUsage();
        return 1;
    }

    var sourceDirectory = args[0];
    string? outputDirectory = null;
    string? version = null;
    string? kid = null;
    string? signingKey = null;
    var channel = "stable";

    for (var i = 1; i < args.Length; i++)
    {
        var option = args[i];
        switch (option)
        {
            case "--out":
                outputDirectory = ReadOptionValue(args, ref i, option);
                break;
            case "--version":
                version = ReadOptionValue(args, ref i, option);
                break;
            case "--kid":
                kid = ReadOptionValue(args, ref i, option);
                break;
            case "--channel":
                channel = ReadOptionValue(args, ref i, option);
                break;
            case "--key":
                signingKey = ReadOptionValue(args, ref i, option);
                break;
            default:
                Console.Error.WriteLine($"Unbekannte Option '{option}'.");
                PrintUsage();
                return 1;
        }
    }

    if (string.IsNullOrWhiteSpace(outputDirectory) ||
        string.IsNullOrWhiteSpace(version) ||
        string.IsNullOrWhiteSpace(kid))
    {
        Console.Error.WriteLine("Für 'build' sind --out, --version und --kid erforderlich.");
        PrintUsage();
        return 1;
    }

    signingKey ??= Environment.GetEnvironmentVariable("PACK_SIGNING_KEY");
    if (string.IsNullOrWhiteSpace(signingKey))
    {
        Console.Error.WriteLine("Kein Signaturschlüssel gefunden. Nutze --key oder die Umgebungsvariable PACK_SIGNING_KEY.");
        return 1;
    }

    try
    {
        var result = PackBuilder.Build(new PackBuildOptions
        {
            SourceDirectory = sourceDirectory,
            OutputDirectory = outputDirectory,
            Version = version,
            Channel = channel,
            KeyId = kid,
            SigningKey = signingKey,
        });

        Console.WriteLine($"{result.ManifestCount} Manifest(e) zu {result.Manifest.Packs.Count} Pack(s) gebaut.");
        Console.WriteLine($"Manifest: {result.ManifestPath}");
        Console.WriteLine($"Signatur: {result.SignaturePath}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static string ReadOptionValue(string[] args, ref int index, string option)
{
    if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
    {
        throw new ArgumentException($"Option '{option}' erwartet einen Wert.");
    }

    index++;
    return args[index];
}

static void PrintUsage()
{
    Console.WriteLine("kozy-pack");
    Console.WriteLine("  validate <datei|ordner>   Prüft Tweak-Manifeste gegen Schema v1");
    Console.WriteLine("  build <ordner> --out <ordner> --version <version> --kid <kid> [--channel <name>] [--key <base64|base64url|hex>]");
    Console.WriteLine("                           Baut tierbasierte Pack-Dateien und signiert manifest.json");
    Console.WriteLine("                           Falls --key fehlt, wird PACK_SIGNING_KEY verwendet.");
    Console.WriteLine("  update [--uri <base-uri>] [--channel <name>] [--token <bearer>] [--cache <pfad>] [--dry-run]");
    Console.WriteLine("                           Lädt Pack-Manifest herunter, verifiziert Signatur und Hashes.");
    Console.WriteLine("                           --uri: Basis-URI (Standard: https://packs.kozy.app/v1)");
    Console.WriteLine("                           --channel: Channel (Standard: stable)");
    Console.WriteLine("                           --token: Bearer-Token für Authentifizierung");
    Console.WriteLine("                           --cache: Cache-Verzeichnis (Standard: %TEMP%/kozy-packs)");
    Console.WriteLine("                           --dry-run: Nur Download und Verifikation, keine Installation");
}

static async Task<int> Update(string[] args)
{
    var baseUri = "https://packs.kozy.app/v1";
    var channel = "stable";
    string? bearerToken = null;
    var cacheDir = Path.Combine(Path.GetTempPath(), "kozy-packs");
    var dryRun = false;

    for (var i = 0; i < args.Length; i++)
    {
        var option = args[i];
        switch (option)
        {
            case "--uri":
                baseUri = ReadOptionValue(args, ref i, option);
                break;
            case "--channel":
                channel = ReadOptionValue(args, ref i, option);
                break;
            case "--token":
                bearerToken = ReadOptionValue(args, ref i, option);
                break;
            case "--cache":
                cacheDir = ReadOptionValue(args, ref i, option);
                break;
            case "--dry-run":
                dryRun = true;
                break;
            default:
                Console.Error.WriteLine($"Unbekannte Option '{option}'.");
                PrintUsage();
                return 1;
        }
    }

    try
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5),
        };

        var service = new PackUpdateService(httpClient, cacheDir);

        Console.WriteLine($"Lade Pack-Manifest von {baseUri} (Channel: {channel})...");

        var result = await service.FetchAndVerifyAsync(baseUri, channel, bearerToken);

        Console.WriteLine($"✓ Manifest erfolgreich verifiziert:");
        Console.WriteLine($"  - Channel: {result.Channel}");
        Console.WriteLine($"  - Key ID: {result.Manifest.Kid}");
        Console.WriteLine($"  - Erstellt: {result.Manifest.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"  - Packs: {result.Manifest.Packs.Count}");
        Console.WriteLine();

        foreach (var pack in result.Manifest.Packs)
        {
            Console.WriteLine($"  [{pack.Tier}] {pack.Version} - {pack.File}");
            Console.WriteLine($"    SHA-256: {pack.Sha256}");
            Console.WriteLine($"    Größe: {pack.Size:N0} Bytes");
        }

        if (dryRun)
        {
            Console.WriteLine();
            Console.WriteLine("Dry-Run: Installation übersprungen.");
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine("Installation der Packs...");

        // Installierte Versionen laden für Downgrade-Schutz
        var installedVersionsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "kozy",
            "pack-versions.json");

        var journalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "kozy",
            "journal");

        // Engine und Installation-Service erstellen
        var handlers = Kozy.Actions.HandlerCatalog.Create();
        var systemInfo = new Kozy.Actions.WindowsSystemInfoProvider();
        var restorePoints = new WindowsRestorePointService();
        var journalStore = new JournalStore(journalPath);
        var engine = new Engine(handlers, systemInfo, restorePoints, journalStore, "1.0.0");
        var installService = new PackInstallationService(engine, installedVersionsPath);

        var installResult = installService.Install(result, new ApplyOptions(
            CreateRestorePoint: true,
            StopOnError: true
        ));

        Console.WriteLine();
        Console.WriteLine($"✓ Installation abgeschlossen:");
        Console.WriteLine($"  - Angewendet: {installResult.TotalApplied}");
        Console.WriteLine($"  - Fehlgeschlagen: {installResult.TotalFailed}");
        Console.WriteLine($"  - Übersprungen: {installResult.TotalSkipped}");
        Console.WriteLine();

        foreach (var tierResult in installResult.TierResults)
        {
            var status = tierResult.Success ? "✓" : "✗";
            Console.WriteLine($"  {status} [{tierResult.Tier}] {tierResult.Version}");
            Console.WriteLine($"      Angewendet: {tierResult.Applied}, Fehlgeschlagen: {tierResult.Failed}, Übersprungen: {tierResult.Skipped}");

            if (!string.IsNullOrEmpty(tierResult.Error))
            {
                Console.WriteLine($"      Fehler: {tierResult.Error}");
            }
        }

        return installResult.OverallSuccess ? 0 : 1;
    }
    catch (PackVerificationException ex)
    {
        Console.Error.WriteLine($"✗ Verifikationsfehler: {ex.Message}");
        return 2;
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"✗ Netzwerkfehler: {ex.Message}");
        return 3;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"✗ Fehler: {ex.Message}");
        return 1;
    }
}


