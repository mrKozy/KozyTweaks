# kozy – Coding-Plan

**Windows-Optimierungstool + Lizenzserver + Shop**
Version 1.0 · Stand 05.11.2026 · Arbeitsname: `kozy` (zentral änderbar, siehe 2.2)

---

## 0. Lesehinweise

- Das Dokument ist zum phasenweisen Abarbeiten gedacht, durch einen Entwickler oder einen KI-Coding-Agenten.
- **MUSS** = Pflicht, **SOLL** = stark empfohlen, **KANN** = optional.
- Tasks haben IDs (`T-01` …). Jede Phase endet mit Abnahmekriterien (`AK`). Eine Phase gilt erst als fertig, wenn alle AK erfüllt sind.
- Alle Versionsnummern sind Stand November 2026. Vor dem Start prüfen, ob es neuere LTS-Versionen gibt.
- Annahme für Zeitschätzungen: 1 erfahrener Entwickler, Vollzeit.

---

## 1. Scope

### 1.1 Was „1:1" hier bedeutet

Die Referenz ist das Produkt, das du mir gezeigt hast. Ich kenne davon nur die **öffentliche Website** (Startseite, Produktübersicht, Seiten „Full Optimization" und „Extreme Tweaks") sowie Trustpilot-Bewertungen. Den Inhalt des eigentlichen Tweak-Panels kenne ich nicht.

| Wird nachgebaut (Funktionsebene) | Wird NICHT übernommen |
|---|---|
| Produktstruktur: Panel in Stufen (Basic / Pro / Extreme), Precision-Pack, Services, Guides | Deren Skripte, Tweak-Listen, Werte, Code (unbekannt, und fremder Code wäre Urheberrechtsverletzung) |
| Funktionskategorien: Windows-Tweaks, Registry, Netzwerk, Energie, Latenz, Debloat, Cleaner, GPU-Basics | Name, Logo, Farben, Layout, Grafiken, Screenshots, Marketingtexte |
| Lizenzierung pro Gerät, Download nach Kauf, Updates mehrmals pro Monat | Deren Benchmark-Zahlen und Testimonials (müssen aus eigenen Messungen bzw. echten Kunden stammen) |
| Einzelsitzung („One-on-One") als Dienstleistung | Behauptungen, die nicht belegbar sind (siehe 1.4) |

### 1.2 Nicht-Ziele (v1)

- Keine Kernel-Treiber, keine Zugriffe auf Spielprozesse oder Spielspeicher, keine Cheat-Funktionen.
- Kein automatisches CPU/GPU-Overclocking (nur manuell im Service).
- Kein ARM64, Linux, macOS. Keine Windows-Server-Editionen.
- Konsolen-Guides (Xbox/PlayStation) nur als PDF/Text-Content, nicht als Software.

### 1.3 Mapping Referenzangebot → eigenes Modul

| Referenzangebot | Eigenes Modul | Phase |
|---|---|---|
| Basic / Pro / Extreme Tweaks | Tweak-Panel, Filter nach `tier` | 1–4 |
| Precision Tweaks (Hit Registration) | Pack „Input & Netzwerk-Konsistenz" (`tier: precision`) | 3 |
| Optimization Services | Service-Modus (Bericht, Profile) | 8 |
| Full Optimization (Einzelsitzung) | Service-Modus + Buchungssystem | 8 |
| Xbox/PlayStation Guides | Content-Produkte (`kind: guide`) | 9 |
| Affiliate | Affiliate-System | 8 |
| Discord-Support | optional Discord-Bot | 9 |

### 1.4 Werbeaussagen: bitte anders machen als die Referenz

Die Referenz wirbt mit „Boost Aim" und „Hit Registration Tweaks". Kein Windows-Tweak kann serverseitige Trefferregistrierung ändern. In Deutschland ist irreführende Werbung abmahnfähig (UWG §5). Deshalb:

- Das Pack heißt **„Input & Netzwerk-Konsistenz"** und verspricht nur: gleichmäßigere Frametimes, weniger Eingabeverzögerung lokal, stabilere Netzwerk-Latenz.
- Tweak-Zahlen werden **ehrlich gezählt**. „1300+ Tweaks" zählt in der Referenz vermutlich einzelne Registrywerte. Bei dir: zeige „X Tweaks (Y Einzeländerungen)".
- FPS-Gewinne auf der Website nur aus eigenen Messungen (Modul Benchmark, Kapitel 8) mit vollständiger Hardware-Angabe.

---

## 2. Rahmenentscheidungen

### 2.1 Tech-Stack (festgelegt)

| Komponente | Technologie |
|---|---|
| Desktop-App | C# 14, **.NET 10 (LTS)**, WPF, `CommunityToolkit.Mvvm`, `WPF-UI` (lepoco, Fluent-Optik), `Serilog` |
| Task Scheduler | NuGet `TaskScheduler` (dahall) |
| Krypto (Client) | `BouncyCastle.Cryptography` (Ed25519) |
| Installer + Auto-Update | `Velopack` |
| Backend | ASP.NET Core 10 Minimal APIs, EF Core 10, PostgreSQL 16, `Npgsql` |
| Zahlung | Stripe Checkout + Webhooks (SEPA, Karte, PayPal je nach Stripe-Freischaltung) |
| E-Mail | Transaktionsmails über Postmark oder Brevo (EU-Hosting bevorzugt) |
| Website + Admin | Next.js 15 (App Router), TypeScript, Tailwind CSS, `next-intl` (de/en) |
| Hosting | Hetzner VPS (Docker Compose, Caddy als Reverse Proxy, automatisches TLS), Objektspeicher für Installer |
| Benchmark-Fremdtool | Intel **PresentMon** (MIT-Lizenz) als mitgelieferte Binärdatei |
| CI/CD | GitHub Actions (`windows-latest` für App, `ubuntu-latest` für Backend/Web) |

### 2.2 Namens-Konstante

Der Name darf **nur an einer Stelle pro Komponente** stehen:

- Desktop: `Directory.Build.props` mit `<ProductName>kozy</ProductName>` und `<ProductId>kozy</ProductId>`. Zugriff im Code über generierte Klasse `Brand` (Source Generator oder T4).
- Web: `web/src/config/brand.ts` mit `export const BRAND = { name: "kozy", domain: "kozy.example", supportEmail: "..." }`.
- Backend: `appsettings.json` → `"Brand": { "Name": "kozy" }`.

**CI-Check (MUSS):** Build schlägt fehl, wenn die Referenzmarke im Repo auftaucht:

```bash
! grep -rniE "risxn|rixzsen" --exclude-dir={.git,node_modules,bin,obj} .
```

**Vor dem Launch (MUSS):** Markenrecherche zu „kozy" im DPMA-Register und in EUIPO TMview, Klassen 9 (Software), 35 (Handel), 42 (IT-Dienste). Kollisionen sind bei einem kurzen Namen wahrscheinlich. Domain und Social-Handles prüfen.

### 2.3 Zielsysteme

| Windows | Build | Status |
|---|---|---|
| Windows 10 22H2 | 19045 | Best Effort (Consumer-Support endete Oktober 2025) |
| Windows 11 23H2 | 22631 | Voll |
| Windows 11 24H2 | 26100 | Voll |
| Windows 11 25H2 | 26200 | Voll |

Nur x64. Editionen: Home, Pro, Enterprise, Education. Die App läuft als Administrator und ist self-contained (keine .NET-Installation beim Kunden nötig).

Die Referenz schließt 24H2/25H2 laut Produktseite aus. Dein Produkt soll das besser lösen: **jeder Tweak hat `minBuild`/`maxBuild`** (5.2), die Engine blendet nicht passende Tweaks aus, statt das ganze Produkt zu sperren.

---

## 3. Architektur

```
┌──────────────────────────┐        HTTPS        ┌─────────────────────────────┐
│ Desktop-App (WPF, Admin) │ ───────────────────►│ Backend (ASP.NET Core)      │
│  ├ Tweak-Engine          │  activate/refresh   │  ├ License API              │
│  ├ Handler (Registry,    │  packs/manifest     │  ├ Pack-Download (signiert) │
│  │  Service, Task, ...)  │  pack-download      │  ├ Stripe-Webhook           │
│  ├ Benchmark (PresentMon)│                     │  ├ Admin API                │
│  └ Lizenz-Client         │                     │  └ PostgreSQL               │
└──────────────────────────┘                     └──────────────▲──────────────┘
                                                                │
┌──────────────────────────┐   Stripe Checkout    ┌─────────────┴───────────────┐
│ Website (Next.js)        │ ───────────────────► │ Stripe                      │
│  Shop, Legal, Admin-UI   │ ◄─────────────────── │ (Zahlung, Webhook-Events)   │
└──────────────────────────┘                      └─────────────────────────────┘
```

Datenfluss Kauf: Website → Stripe Checkout → Webhook `checkout.session.completed` → Backend legt Order + Lizenz an → E-Mail mit Key und Download-Link → Kunde installiert → App aktiviert Lizenz → App lädt signierte Tweak-Packs.

---

## 4. Repository-Struktur (Monorepo)

```
kozy/
├─ Directory.Build.props
├─ global.json                      # pinnt .NET SDK
├─ src/
│  ├─ Kozy.App/                     # WPF UI (Views, ViewModels, Themes)
│  ├─ Kozy.Core/                    # Engine, Modelle, Journal, Kompatibilitätsprüfung
│  ├─ Kozy.Actions/                 # Handler pro Aktionstyp
│  ├─ Kozy.Licensing/               # Client: Aktivierung, Token, HWID
│  ├─ Kozy.Benchmark/               # PresentMon-Wrapper, Ping, Auswertung
│  ├─ Kozy.Cli/                     # kozy-pack: Packs bauen + signieren (nur für dich)
│  └─ Kozy.Server/                  # ASP.NET Core Backend
├─ packs/                           # Quell-Manifeste (JSON), pro Kategorie ein Ordner
│  ├─ gaming/  input/  power/  visual/  network/  services/  debloat/  latency/
├─ web/                             # Next.js
├─ tests/
│  ├─ Kozy.Core.Tests/  Kozy.Actions.Tests/  Kozy.Server.Tests/
│  └─ vm/                           # Hyper-V-Testskripte (PowerShell)
├─ tools/                           # Build-/Sign-/Release-Skripte
├─ docs/
└─ .github/workflows/
```

---

## 5. Tweak-Engine (Kern des Produkts)

### 5.1 Grundregeln (MUSS)

1. **Alles ist datengetrieben.** Tweaks sind JSON-Manifeste, kein hartkodierter Code. Neue Tweaks = neues Pack, kein App-Update.
2. **Keine rohen Shell-Befehle im Manifest.** Nur die typisierten Aktionen aus 5.3. Das schützt vor Missbrauch, falls ein Pack manipuliert wird.
3. **Jede Aktion erfasst vor dem Ändern den Ist-Zustand** (Capture) und ist damit rücknehmbar. Ausnahme: `appx.remove` ist als `irreversible` markiert.
4. **Idempotenz:** Zweimal anwenden ergibt denselben Zustand wie einmal.
5. **Detect:** Jeder Tweak meldet `Applied | NotApplied | Partial | Unknown`, damit die UI Schalter korrekt anzeigt.
6. **Verify nach Apply:** Wert zurücklesen und vergleichen. Bei Abweichung: Rollback dieses Tweaks.
7. **Allowlist** für Registrypfade, Dienstnamen, Dateipfade (5.6).
8. **Vor dem ersten Apply einer Sitzung:** Windows-Wiederherstellungspunkt anlegen (Fehler dabei = Warnung, kein Abbruch, aber Nutzer muss bestätigen).
9. **Kein Tweak läuft ohne Kompatibilitätsprüfung** (Build, Edition, Gerätetyp, GPU-Hersteller).
10. **Nichts wird heimlich verändert:** Jede Änderung steht im Journal und in der UI-Historie.

### 5.2 Manifest-Schema v1

Dateiformat: UTF-8 JSON, eine Datei pro Tweak unter `packs/<kategorie>/<id>.json`.

```json
{
  "schemaVersion": 1,
  "id": "gaming.disable-gamedvr",
  "version": 1,
  "name":        { "de": "Game-DVR deaktivieren", "en": "Disable Game DVR" },
  "description": { "de": "Schaltet Hintergrundaufnahme von Xbox Game Bar ab.", "en": "Turns off background capture of Xbox Game Bar." },
  "category": "gaming",
  "tier": "basic",
  "risk": "safe",
  "requiresReboot": false,
  "supports": {
    "minBuild": 19045,
    "maxBuild": null,
    "editions": ["Home", "Pro", "Enterprise", "Education"],
    "deviceClass": ["desktop", "laptop"],
    "gpuVendor": null
  },
  "conflictsWith": [],
  "dependsOn": [],
  "irreversible": false,
  "benchmarkHint": "frametime",
  "actions": [
    { "type": "registry.set", "hive": "HKCU", "path": "System\\GameConfigStore", "name": "GameDVR_Enabled", "kind": "DWord", "value": 0 }
  ]
}
```

Feldregeln:

| Feld | Typ / Werte | Regel |
|---|---|---|
| `id` | `^[a-z]+(\.[a-z0-9-]+)+$` | global eindeutig, ändert sich nie |
| `tier` | `basic` \| `pro` \| `extreme` \| `precision` | Mapping auf Lizenz (9.4) |
| `risk` | `safe` \| `moderate` \| `advanced` \| `experimental` | `experimental` ist nie in Voreinstellungen |
| `supports.minBuild` | int | Windows-Buildnummer, inklusive |
| `supports.maxBuild` | int oder `null` | `null` = nach oben offen |
| `deviceClass` | `desktop` \| `laptop` | Laptop = `Win32_Battery` vorhanden |
| `gpuVendor` | `nvidia` \| `amd` \| `intel` \| `null` | per `Win32_VideoController` |
| `irreversible` | bool | UI verlangt zusätzliche Bestätigung |
| `benchmarkHint` | `frametime` \| `latency` \| `network` \| `none` | steuert Benchmark-Empfehlung in der UI |

### 5.3 Aktionstypen

| Typ | Felder | Capture / Revert |
|---|---|---|
| `registry.set` | `hive` (HKLM/HKCU), `path`, `name`, `kind` (DWord/QWord/String/ExpandString/Binary/MultiString), `value`, `view` (`64` default) | merkt: Wert existiert? Typ? Wert? Revert: alten Wert setzen oder Wert löschen |
| `registry.delete` | `hive`, `path`, `name` | merkt Wert + Typ, Revert: wiederherstellen |
| `service.config` | `name`, `startMode` (`auto`/`manual`/`disabled`), `stop` (bool) | merkt `StartMode` und Laufzustand (WMI `Win32_Service`) |
| `task.disable` | `path` (z. B. `\Microsoft\Windows\...`) | merkt `Enabled` |
| `powercfg.value` | `scheme` (`ACTIVE` oder GUID), `subgroup` (GUID), `setting` (GUID), `ac` (int), `dc` (int, optional) | merkt aktuellen AC/DC-Index per `powercfg /query`, Revert setzt Indizes zurück |
| `powercfg.scheme` | `op` (`duplicate`/`setactive`), `sourceGuid`, `name` | merkt aktives Schema, Revert: altes Schema aktivieren, Kopie löschen |
| `powercfg.hibernate` | `enabled` (bool) | merkt Registry `HibernateEnabled` |
| `bcdedit.set` | `element`, `value` | merkt `bcdedit /enum {current}`, Revert: alten Wert setzen oder `/deletevalue` |
| `bcdedit.delete` | `element` | merkt Wert, Revert: wieder setzen |
| `dns.set` | `servers` (Liste), Ziel: aktive Adapter | merkt DNS-Liste je Adapter |
| `appx.remove` | `names` (Liste von Paketnamen), `allUsers` (bool) | merkt Paketname + Version, **irreversible** |
| `cleaner.paths` | `targets` (Liste aus fester Enum, siehe 5.6) | zeigt Größe vorab, Löschen nicht rücknehmbar |
| `spi.mouse` | `speed`, `threshold1`, `threshold2` | ruft `SystemParametersInfo(SPI_SETMOUSE = 0x0004)` mit `SPIF_UPDATEINIFILE \| SPIF_SENDCHANGE`, damit kein Abmelden nötig ist |

Alles andere ist in v1 nicht erlaubt. Neue Aktionstypen brauchen ein App-Update.

### 5.4 Zustandsmodell und Journal

Pro Sitzung eine Datei: `%ProgramData%\kozy\journal\{yyyyMMdd-HHmmss}-{guid}.json`.

```json
{
  "sessionId": "b6c1...",
  "startedAt": "2026-11-05T14:02:11Z",
  "appVersion": "1.0.0",
  "osBuild": 26100,
  "restorePoint": { "created": true, "sequenceNumber": 412 },
  "tweaks": [
    {
      "id": "gaming.disable-gamedvr",
      "status": "applied",
      "actions": [
        {
          "index": 0,
          "type": "registry.set",
          "captured": { "exists": true, "kind": "DWord", "value": 1 },
          "applied": { "kind": "DWord", "value": 0 },
          "verified": true
        }
      ]
    }
  ]
}
```

Journal-Dateien werden nie automatisch gelöscht. Der Nutzer kann Sitzungen in der Historie einzeln oder komplett zurücknehmen.

### 5.5 Algorithmus (Pseudocode, verbindlich)

```
Apply(selectedTweakIds, options):
  Preflight():
    require IsAdmin, OS x64, not pendingReboot(optional warn)
    filter tweaks by Compatibility(osBuild, edition, deviceClass, gpuVendor)
    resolve dependsOn (topologische Sortierung), reject conflictsWith
  create restore point (Checkpoint-Computer, RestorePointType MODIFY_SETTINGS)
  create session journal
  for tweak in sorted:
    state = Detect(tweak)
    if state == Applied: log "skip"; continue
    appliedActions = []
    try:
      for action in tweak.actions:
        handler = HandlerFor(action.type)
        handler.Validate(action)            // Allowlist, Typprüfung
        captured = handler.Capture(action)
        journal.Record(tweak, action, captured)
        handler.Apply(action)
        if !handler.Verify(action): throw VerifyFailed
        appliedActions.push(action)
    catch ex:
      for a in reverse(appliedActions): handler.Restore(a, journal.CapturedFor(a))
      journal.Mark(tweak, "failed", ex)
      if options.StopOnError: break
    journal.Mark(tweak, "applied")
  flush journal
  if any tweak.requiresReboot: show reboot prompt (nicht automatisch neu starten)

Revert(sessionId | tweakId):
  for tweak in reverse(order of application):
    for action in reverse(tweak.actions):
      if tweak.irreversible: skip + warn
      handler.Restore(action, journal.CapturedFor(action))
    journal.Mark(tweak, "reverted")
```

### 5.6 Allowlists

- **Registry:** schreibbar nur unter diesen Präfixen (alles andere → `Validate` wirft):
  `HKCU\Control Panel\`, `HKCU\System\GameConfigStore`, `HKCU\Software\Microsoft\Windows\CurrentVersion\` (Explorer, Themes, GameDVR, Run, StartupApproved), `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile`, `HKLM\SOFTWARE\Policies\Microsoft\Windows\`, `HKLM\SOFTWARE\Policies\Google\Chrome`, `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers`, `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`, `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\`.
- **Gesperrt (immer):** `HKLM\SAM`, `HKLM\SECURITY`, Defender-Policies, UAC-Werte, Windows-Update-Policies, Boot-Optionen mit `testsigning` oder `nointegritychecks`.
- **Dienste:** nur Namen aus fester Liste (`SysMain`, `DiagTrack`, `dmwappushservice`).
- **Cleaner-Ziele (feste Enum):** `UserTemp` (`%TEMP%`), `WindowsTemp` (`C:\Windows\Temp`), `INetCache` (`%LOCALAPPDATA%\Microsoft\Windows\INetCache`), `ThumbnailCache` (`%LOCALAPPDATA%\Microsoft\Windows\Explorer\thumbcache_*.db`), `D3DShaderCache` (`%LOCALAPPDATA%\D3DSCache`), `NvidiaShaderCache` (`%LOCALAPPDATA%\NVIDIA\DXCache`, `%LOCALAPPDATA%\NVIDIA\GLCache`), `WUDownload` (`C:\Windows\SoftwareDistribution\Download`, vorher `wuauserv` stoppen und danach wieder starten). Nicht löschen: Prefetch, `WinSxS`, Nutzerdaten.
- **Prozessaufrufe:** nur `powercfg.exe`, `bcdedit.exe`, `sc.exe`, `powershell.exe` mit festen Argument-Templates. Parameterwerte werden gegen Regex geprüft. Verboten: `-EncodedCommand`, `Invoke-Expression`, Download-Cmdlets.

### 5.7 Pack-Format und Signatur

- Build: `kozy-pack build packs/ --out dist/packs/` erzeugt pro Tier eine Datei `pack-{tier}-{version}.json` (Array aller Tweak-Manifeste) plus `manifest.json`.
- `manifest.json`:

```json
{
  "channel": "stable",
  "generatedAt": "2026-11-05T12:00:00Z",
  "kid": "pack-2026-01",
  "packs": [
    { "tier": "basic", "version": "1.4.0", "sha256": "…", "size": 48213, "file": "pack-basic-1.4.0.json" }
  ]
}
```

- Signatur: Ed25519 über die kanonische Byte-Form von `manifest.json`, abgelegt in `manifest.json.sig`. Jedes Pack-Hash steht im Manifest.
- Der Client prüft: Signatur gültig, `kid` bekannt, SHA-256 jedes Packs stimmt, `version` ist nicht kleiner als die lokal installierte (Downgrade-Schutz).
- Schlüssel: `kid` erlaubt Rotation. Die App enthält bis zu 2 öffentliche Schlüssel. Der private Pack-Schlüssel liegt nur in CI-Secrets oder offline, nie auf dem Webserver.

---

## 6. Seed-Tweak-Katalog (Startbestand, eigene Implementierung)

Das sind **gängige, öffentlich dokumentierte** Einstellungen. Du musst jeden Wert vor dem Release selbst auf Testsystemen prüfen (Verhalten kann sich je Windows-Build ändern). Die Spalte „Messung" sagt, ob ein Benchmark vor/nach Pflicht ist, bevor der Tweak in einer Voreinstellung landet.

| ID | Tier | Risiko | Exakte Änderung | Messung |
|---|---|---|---|---|
| `gaming.disable-gamedvr` | basic | safe | `HKCU\System\GameConfigStore` `GameDVR_Enabled`=0 (DWord); `HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR` `AppCaptureEnabled`=0; `HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR` `AllowGameDVR`=0 | optional |
| `gaming.mmcss-games` | pro | moderate | `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile`: `SystemResponsiveness`=10 (DWord), `NetworkThrottlingIndex`=0xFFFFFFFF (DWord). Unterschlüssel `Tasks\Games`: `GPU Priority`=8 (DWord), `Priority`=6 (DWord), `Scheduling Category`="High" (String), `SFIO Priority`="High" (String) | ja |
| `gaming.hags` | pro | experimental | `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers` `HwSchMode`: 2 = an, 1 = aus; Neustart; nur wenn GPU/Treiber es unterstützt | ja, Pflicht |
| `input.mouse-accel-off` | basic | safe | `HKCU\Control Panel\Mouse` `MouseSpeed`="0", `MouseThreshold1`="0", `MouseThreshold2`="0" (alle String) plus `spi.mouse` 0/0/0 | nein |
| `input.sticky-keys-off` | basic | safe | `HKCU\Control Panel\Accessibility\StickyKeys` `Flags`="506"; `...\ToggleKeys` `Flags`="58"; `...\Keyboard Response` `Flags`="122" (alle String) | nein |
| `input.keyboard-repeat` | pro | safe | `HKCU\Control Panel\Keyboard` `KeyboardDelay`="0", `KeyboardSpeed`="31" (String) | nein |
| `power.ultimate-performance` | basic | safe | `powercfg.scheme`: Duplikat von `e9a42b02-d5df-448d-aa00-03f14749eb61`, Name „kozy Performance", danach `setactive`. Bei Laptops nur nach Bestätigung | nein |
| `power.core-parking-off` | pro | moderate | `powercfg.value` Subgruppe `54533251-82be-4824-96c1-47b60b740d00`, Setting `0cc5b647-c1df-4637-891a-dec35c318583` (CPMINCORES) ac=100 | ja |
| `power.usb-suspend-off` | pro | safe | Subgruppe `2a737441-1930-4402-8d77-b2bebba308a3`, Setting `48e6b7a6-50f5-4782-a5d4-53bb8f07e226` ac=0 | nein |
| `power.pcie-aspm-off` | pro | safe | Subgruppe `501a4d13-42af-4429-9fd1-a8218c268e20`, Setting `ee12f906-d277-404b-b6da-e5fa1a576df5` ac=0 | nein |
| `power.fast-startup-off` | pro | safe | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power` `HiberbootEnabled`=0 (DWord) | nein |
| `power.hibernate-off` | extreme | moderate | `powercfg.hibernate` enabled=false (spart Plattenplatz, Ruhezustand entfällt) | nein |
| `visual.transparency-off` | basic | safe | `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize` `EnableTransparency`=0 (DWord) | nein |
| `visual.best-performance` | pro | safe | `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects` `VisualFXSetting`=2 (DWord) | nein |
| `visual.min-animate-off` | basic | safe | `HKCU\Control Panel\Desktop\WindowMetrics` `MinAnimate`="0" (String) | nein |
| `net.nagle-off` | extreme | moderate | je aktivem Adapter (Status Up, Gateway vorhanden) unter `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{Adapter-GUID}`: `TcpAckFrequency`=1, `TCPNoDelay`=1 (DWord). Wirkt nur auf TCP, nicht auf UDP-Spiele | ja |
| `net.dns-preset` | basic | safe | `dns.set` mit Auswahl: Cloudflare 1.1.1.1/1.0.0.1, Google 8.8.8.8/8.8.4.4, Quad9 9.9.9.9/149.112.112.112. UI misst vorher die Antwortzeit und empfiehlt | ja |
| `services.sysmain-off` | extreme | moderate | `service.config` `SysMain` disabled + stop. Nur wenn Systemlaufwerk SSD/NVMe (WMI `MSFT_PhysicalDisk.MediaType`) | nein |
| `services.diagtrack-off` | pro | safe | `service.config` `DiagTrack` und `dmwappushservice` disabled + stop. Zusätzlich `HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection` `AllowTelemetry`=0 (greift offiziell nur bei Enterprise/Education, bei Home/Pro wird 0 wie 1 behandelt: das in der UI so erklären) | nein |
| `tasks.telemetry-off` | pro | safe | `task.disable`: `\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser`, `\Microsoft\Windows\Application Experience\ProgramDataUpdater`, `\Microsoft\Windows\Customer Experience Improvement Program\Consolidator`, `\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip` | nein |
| `debloat.appx-basic` | basic | moderate | `appx.remove` (irreversible): `Microsoft.BingNews`, `Microsoft.BingWeather`, `Microsoft.GetHelp`, `Microsoft.Getstarted`, `Microsoft.MicrosoftSolitaireCollection`, `Microsoft.WindowsFeedbackHub`, `Microsoft.MicrosoftOfficeHub`, `Microsoft.YourPhone` | nein |
| `browser.chrome-background-off` | pro | safe | `HKLM\SOFTWARE\Policies\Google\Chrome` `BackgroundModeEnabled`=0 (DWord). Chrome zeigt danach „von Organisation verwaltet" an, das in der Beschreibung erwähnen | nein |
| `latency.bcd-remove-useplatformclock` | pro | safe | `bcdedit.delete` `useplatformclock` (nur falls vorhanden; erzwungenes HPET gilt als Verschlechterung) | ja |
| `latency.bcd-disable-dynamic-tick` | extreme | experimental | `bcdedit.set` `disabledynamictick` `yes`, Neustart. Wirkung umstritten, nie in Voreinstellungen | ja, Pflicht |

**Geschützte Pakete (nie entfernen):** `Microsoft.WindowsStore`, `Microsoft.XboxIdentityProvider`, `Microsoft.Xbox.TCUI`, `Microsoft.XboxGameOverlay`, `Microsoft.GamingApp`, `Microsoft.WindowsTerminal`, `Microsoft.Windows.Photos`, `Microsoft.WindowsCalculator`.

**Autostart-Manager (kein Tweak, eigene Ansicht):** liest `HKCU` und `HKLM` `SOFTWARE\Microsoft\Windows\CurrentVersion\Run` sowie `StartupApproved\Run`. Eintrag aktiv = erstes Byte `0x02`, deaktiviert = `0x03`. Nutzer schaltet pro Eintrag, nichts wird gelöscht.

**Nie umsetzen (Hartregel):** Defender oder Firewall abschalten, UAC abschalten, Windows Update dauerhaft sperren, Spectre/Meltdown-Mitigations abschalten, VBS/HVCI abschalten (höchstens als `advanced` mit deutlicher Sicherheitswarnung in einer späteren Version), `testsigning`, Treiber-Signaturprüfung abschalten, irgendetwas, das Anti-Cheat-Systeme als Manipulation werten könnte.

---

## 7. Desktop-App (UI-Spezifikation)

Fenster: Mindestgröße 960×600, Standard 1100×720, dunkles Fluent-Theme als Standard, Hell optional. Sprache de/en, Umschaltung ohne Neustart (Resx).

| Screen | Inhalt (verbindlich) |
|---|---|
| **Aktivierung** | Eingabefeld Lizenzschlüssel (Maske `XXXXX-XXXXX-XXXXX-XXXXX`, automatisch groß, Leerzeichen entfernt), Button „Aktivieren", Statusmeldung bei Fehlercodes (9.3). Ohne gültige Lizenz sind Tweak-Anwendung und Pack-Download gesperrt. Historie, Revert und Benchmark bleiben nutzbar |
| **Dashboard** | Systemkarte: Windows-Edition/Build, CPU, GPU, RAM, Systemlaufwerk-Typ, Gerätetyp. Karte „Zuletzt angewendet" (Sitzung, Anzahl Tweaks). Karte „Letzter Benchmark". Buttons: „Voreinstellung wählen", „Benchmark starten". **Kein erfundener „Optimierungs-Score"** |
| **Tweaks** | Linke Kategorienleiste, Suchfeld, Filter Tier/Risiko. Liste: Name, Kurzbeschreibung, Tier-Badge, Risiko-Chip (grün/gelb/orange/rot), Neustart-Icon, Schalter (Zustand aus Detect). Unten Leiste: „X ausgewählt · Anwenden · Zurücknehmen". Voreinstellungen oben: **Sicher** (alle `safe`), **Ausgewogen** (`safe`+`moderate`), **Maximal** (alles bis `advanced`; nie `experimental`). Bestätigungsdialog listet alle Änderungen und markiert `irreversible` |
| **Cleaner** | Checkliste der Cleaner-Ziele mit berechneter Größe. Button „Bereinigen". Ergebnis: freigegebener Platz |
| **Debloat** | Liste installierter Appx-Pakete aus der Entfernen-Liste, geschützte Pakete ausgegraut und nicht wählbar |
| **Autostart** | Tabelle der Autostart-Einträge mit Schalter |
| **Netzwerk** | Adapterliste, DNS-Preset-Auswahl mit Ping-Messung je Preset (5 Pings), Button „Anwenden" |
| **Benchmark** | siehe Kapitel 8 |
| **Verlauf** | Sitzungsliste (Datum, Anzahl Tweaks, Status), Detailansicht je Sitzung, Buttons „Sitzung zurücknehmen" und „Einzelnen Tweak zurücknehmen", Link „Wiederherstellungspunkt öffnen" (`rstrui.exe`) |
| **Einstellungen** | Sprache, Theme, Update-Kanal (stable/beta), Logs-Ordner öffnen, Lizenz deaktivieren, Telemetrie (Standard **aus**, nur Opt-in) |

Zustände, die jede Ansicht abdecken muss: Laden, leer, Fehler mit Klartext, Offline (Lizenz noch gültig vs. abgelaufen).

**CLI (MUSS, für Tests und Service):**

```
kozy.exe --headless --detect [--json]
kozy.exe --headless --apply <id,id,...|preset:safe|preset:balanced|preset:max> [--dry-run] [--no-restore-point]
kozy.exe --headless --revert <sessionId|tweakId>
kozy.exe --headless --report out.json
```

Exit-Codes: `0` OK, `10` Teilfehler, `20` Preflight fehlgeschlagen, `30` Lizenz ungültig, `40` Signatur ungültig.

---

## 8. Benchmark-Modul

Zweck: eigene, ehrliche Vorher/Nachher-Messungen für die App und für die Website.

### 8.1 Frametimes (PresentMon)

- PresentMon wird als Binärdatei mitgeliefert (Lizenztext der MIT-Lizenz in `THIRD-PARTY-NOTICES.md`). Aufruf mit Zielprozessname, Messdauer 60 s, CSV-Ausgabe. Die genauen Parameter hängen von der PresentMon-Version ab: gegen `PresentMon --help` prüfen.
- Auswertung aus CSV (Spalte `MsBetweenPresents` in 1.x, `FrameTime` in 2.x):
  - `avgFps = 1000 / mean(frameMs)`
  - `p99FrameMs`, `1% low (Perzentil) = 1000 / p99FrameMs`
  - `1% low (Durchschnitt) = 1000 / mean(schlechteste 1 % der frameMs)`, **beide Werte getrennt beschriftet**
  - `0.1% low` analog
  - Frametime-Streuung: Standardabweichung

### 8.2 Netzwerk

- ICMP-Messung: 100 Pings, Intervall 200 ms, Ziel frei wählbar (Standard 1.1.1.1). Ausgabe: min / avg / max, Jitter (Mittelwert der absoluten Differenzen aufeinanderfolgender RTTs), Paketverlust in %.

### 8.3 Messprotokoll (für Website-Zahlen verbindlich)

1. Gleiche Szene, gleiche Einstellungen, Hintergrundprogramme geschlossen.
2. Je Zustand **3 Läufe**, Median zählt, Abweichung >5 % zwischen Läufen = Messung wiederholen.
3. Reihenfolge: Vorher → Tweaks anwenden → Neustart → Nachher.
4. Gespeichert wird: Hardware, Windows-Build, Treiberversion, Spiel/Version, Einstellungen, Rohwerte.
5. Auf der Website steht **die komplette Hardware** neben jeder Zahl.

---

## 9. Lizenz-Backend

### 9.1 Datenbank (PostgreSQL)

```sql
create extension if not exists pgcrypto;
create extension if not exists citext;

create table customers (
  id uuid primary key default gen_random_uuid(),
  email citext unique not null,
  created_at timestamptz not null default now()
);

create table products (
  id uuid primary key default gen_random_uuid(),
  sku text unique not null,                       -- z. B. 'panel-basic'
  name text not null,
  kind text not null check (kind in ('panel','service','guide')),
  tier text null check (tier in ('basic','pro','extreme','precision')),
  price_cents int not null check (price_cents >= 0),
  currency char(3) not null default 'EUR',
  stripe_price_id text null,
  active boolean not null default true
);

create table orders (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid not null references customers(id),
  stripe_session_id text unique not null,
  stripe_payment_intent text null,
  status text not null check (status in ('paid','refunded','disputed')),
  total_cents int not null,
  currency char(3) not null,
  affiliate_code text null,
  created_at timestamptz not null default now()
);

create table licenses (
  id uuid primary key default gen_random_uuid(),
  order_id uuid not null references orders(id),
  product_id uuid not null references products(id),
  key_hash bytea unique not null,                 -- SHA-256 des normalisierten Keys
  key_last4 char(4) not null,
  status text not null default 'active' check (status in ('active','revoked','expired')),
  max_devices int not null default 1,
  expires_at timestamptz null,                    -- null = lebenslang
  created_at timestamptz not null default now()
);

create table activations (
  id uuid primary key default gen_random_uuid(),
  license_id uuid not null references licenses(id),
  hwid_hash bytea not null,
  device_name text not null,
  app_version text not null,
  os_build int not null,
  first_seen timestamptz not null default now(),
  last_seen timestamptz not null default now(),
  revoked_at timestamptz null,
  unique (license_id, hwid_hash)
);

create table stripe_events (id text primary key, received_at timestamptz not null default now());

create table audit_log (
  id bigserial primary key,
  at timestamptz not null default now(),
  actor text not null,
  action text not null,
  target text not null,
  details jsonb null
);
```

### 9.2 Schlüssel und Tokens

- **Lizenzschlüssel:** 20 Zeichen aus Crockford-Base32 (ohne I, L, O, U), per CSPRNG, Format `XXXXX-XXXXX-XXXXX-XXXXX` (100 Bit Entropie). Klartext wird **nur einmal** per E-Mail verschickt, in der DB liegt nur der SHA-256-Hash plus die letzten 4 Zeichen.
- **HWID:** Client berechnet `SHA-256(salt || smbiosUuid || baseboardSerial || cpuProcessorId)`; `salt` ist eine feste App-Konstante. Fehlende Werte werden durch leeren String ersetzt, aber mindestens 2 von 3 müssen vorhanden sein. Ungültige Platzhalter (`To Be Filled By O.E.M.`, `Default string`, `0000…`) zählen als fehlend. Es wird nur der Hash übertragen.
- **Lizenz-Token (offline prüfbar):** `base64url(payloadJson) + "." + base64url(ed25519Signature)`.

```json
{
  "ver": 1, "kid": "lic-2026-01",
  "lic": "<license uuid>", "hwid": "<base64 hash>",
  "ent": ["tier:extreme", "tier:pro", "tier:basic"],
  "iat": 1793900000, "exp": 1794504800
}
```

- Gültigkeit 7 Tage. Die App versucht bei jedem Start einen Refresh, wenn das Token älter als 24 h ist. Ist das Token abgelaufen und das Gerät offline, wechselt die App in den **gesperrten Modus**: Anwenden gesperrt, Revert bleibt erlaubt.
- Speicherung lokal: `%ProgramData%\kozy\license.dat`, geschützt mit DPAPI (`DataProtectionScope.LocalMachine`).
- **Entitlements:** Tier `extreme` enthält `pro` und `basic`, Tier `pro` enthält `basic`. `precision` ist separat.

### 9.3 API (alle Antworten JSON, Fehler im Format `{ "error": { "code": "...", "message": "..." } }`)

| Methode + Pfad | Body | Erfolg | Fehlercodes |
|---|---|---|---|
| `POST /v1/license/activate` | `{ licenseKey, hwidHash, deviceName, appVersion, osBuild }` | 200 `{ token, expiresAt, entitlements[] }` | 404 `LICENSE_NOT_FOUND`, 403 `LICENSE_REVOKED`, 403 `LICENSE_EXPIRED`, 409 `DEVICE_LIMIT_REACHED` |
| `POST /v1/license/refresh` | `{ token }` | 200 `{ token, expiresAt }` | 401 `TOKEN_INVALID`, 403 `LICENSE_REVOKED` |
| `POST /v1/license/deactivate` | `{ token }` | 204 | 401 `TOKEN_INVALID` |
| `GET /v1/packs/manifest?channel=stable` | Header `Authorization: Bearer <token>` | 200 `manifest.json` + Header `X-Signature` | 401, 403 |
| `GET /v1/packs/{tier}/{version}` | Bearer-Token | 302 auf zeitlich begrenzte Download-URL (5 Minuten) | 401, 403 `NO_ENTITLEMENT` |
| `POST /v1/webhooks/stripe` | Stripe-Event | 200 | 400 bei ungültiger Signatur |

Admin-API (JWT, 2FA Pflicht), Präfix `/admin/v1/`: `GET licenses?email=`, `POST licenses/{id}/revoke`, `POST licenses/{id}/reset-activations`, `POST licenses` (manuell ausstellen, z. B. für Support), `GET orders`, `GET audit`.

Rate-Limits: `activate` 10/Minute je IP und 5/Stunde je Schlüssel-Hash; alle Treffer des Limits im `audit_log`.

### 9.4 Stripe-Ablauf

1. Website ruft `POST /api/checkout` mit `sku` (und optional Affiliate-Code). Der Server erstellt eine Checkout Session (`mode: payment`, `metadata: { sku, affiliate }`, Erfolgs-/Abbruch-URL).
2. Webhook `checkout.session.completed` mit `payment_status == "paid"`: Signatur prüfen, `stripe_events` auf Duplikat prüfen (Idempotenz), Kunde anlegen oder finden, Order, Lizenz erzeugen, E-Mail senden.
3. Webhooks `charge.refunded` und `charge.dispute.created`: Order-Status ändern, Lizenz auf `revoked`.
4. E-Mail enthält: Schlüssel (einmalig), Download-Link zur Installer-Seite, Support-Kontakt, Widerrufsbelehrung (siehe 14).

---

## 10. Website

### 10.1 Routen

| Route | Inhalt |
|---|---|
| `/` | Hero, Nutzenversprechen (4 Kacheln), eigene Benchmarks (nur echte Messungen), FAQ, Footer |
| `/products` | Produktkarten mit Preis |
| `/products/[sku]` | Produktdetail, Leistungsumfang, Kompatibilität (Windows-Builds), „Kaufen" |
| `/service` | Einzelsitzung, Pakete, Add-ons, Buchungslink |
| `/about`, `/contact` | Über uns, Kontaktformular (serverseitig mit Spam-Schutz, z. B. Turnstile) |
| `/affiliate` | Programm-Erklärung, Registrierung |
| `/checkout/success`, `/checkout/cancel` | Ergebnisseiten |
| `/impressum`, `/datenschutz`, `/agb`, `/widerruf` | Rechtliches (14) |
| `/admin/*` | Bestellungen, Lizenzen, Affiliates (nur mit Login + TOTP) |

### 10.2 Technische Vorgaben

- i18n de/en (`next-intl`), Standard de.
- Lighthouse-Ziele: Performance ≥ 90, Accessibility ≥ 95, SEO ≥ 95.
- Keine nicht notwendigen Cookies ohne Einwilligung (Consent-Banner nur wenn nötig, z. B. Affiliate-Cookie `kozy_ref`, 30 Tage).
- Inhalte (Texte, Bilder, Testimonials, Zahlen) sind Eigenleistung. Platzhalter sind klar als `TODO-CONTENT` markiert. Testimonials nur mit Einwilligung echter Kunden.
- Preise in EUR brutto, USt-Handling über Stripe Tax oder manuell (mit Steuerberater klären).

---

## 11. Service-Modus (Einzelsitzung)

Der Service ist eine menschliche Dienstleistung, die die App unterstützt.

- **Bericht (`kozy.exe --headless --report out.json`)**: Hardware, Windows-Build, aktuelle Tweaks, Benchmark vorher/nachher, Laufzeit, Hinweise. Daraus erzeugt das Admin-Backend ein PDF für den Kunden.
- **Ablauf:** Buchung (Cal.com selbst gehostet oder vergleichbar) → Fernwartung (z. B. AnyDesk, außerhalb des Codes) → Erstanalyse → Anwendung eines Profils → Messung → Bericht.
- **Add-ons** (BIOS, Overclocking) sind rein manuelle Leistungen mit schriftlichem Haftungsausschluss und Hinweis auf Garantieverlust. Nicht automatisiert.
- **Malware-Check (Zusatzleistung):** `"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -Scan -ScanType 1` (Schnellscan), Ergebnis in den Bericht. Kein Anspruch auf vollständige Entfernung.

---

## 12. Affiliate (Phase 8, optional)

- Tabelle `affiliates` (id, code, email, commission_percent default 10, payout_method, created_at) und `affiliate_payouts`.
- Link `?ref=CODE` setzt Cookie `kozy_ref` (30 Tage, nur nach Einwilligung). Beim Checkout wandert der Code in die Stripe-Metadaten und in `orders.affiliate_code`.
- Provision wird erst nach Ablauf der Widerrufsfrist (14 Tage) und ohne Erstattung fällig. Admin-Ansicht: Umsatz je Code, offene Auszahlungen.

---

## 13. Sicherheit und Virenscanner

Tweak-Tools werden von Virenscannern oft als verdächtig eingestuft. Das ist der wichtigste Vertrauensfaktor des Produkts. Die Referenz hat laut einer Trustpilot-Bewertung genau dieses Problem gehabt.

**MUSS:**

1. Alle ausgelieferten Dateien **Authenticode-signiert** (Azure Trusted Signing oder OV/EV-Zertifikat). Ohne Signatur steigt die SmartScreen-Warnrate stark.
2. **Kein Packer, keine Obfuskation** (kein UPX, kein ConfuserEx). Das löst fast sicher Fehlalarme aus.
3. Keine Prozesse mit `-EncodedCommand`, kein Nachladen und Ausführen von ungeprüftem Code. Nur Daten (signierte Manifeste) werden nachgeladen, nie ausführbarer Code.
4. Updates nur über Velopack mit Signaturprüfung. Pack-Updates über Ed25519 (5.7).
5. Jeder Release: SHA-256-Hashes auf der Website veröffentlichen, bei VirusTotal prüfen und bei Fehlalarmen über das Microsoft-Security-Intelligence-Portal melden.
6. Reproduzierbare Builds (`<Deterministic>true</Deterministic>`, `<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>`).
7. Keine Telemetrie ohne Opt-in. Logs bleiben lokal unter `%ProgramData%\kozy\logs` (Serilog, rollierend, 14 Tage), enthalten keine Lizenzschlüssel.
8. Backend: Secrets nur in Umgebungsvariablen/Key Vault, Datenbank nicht öffentlich erreichbar, tägliche verschlüsselte Backups, Admin mit TOTP, alle Admin-Aktionen im `audit_log`.

**Ehrlicher Hinweis:** Lokale Tweak-Listen lassen sich nicht wirksam vor Kopieren schützen. Der Mehrwert liegt in Kuratierung, Qualität, Updates, Support und Messungen, nicht in Geheimhaltung. Plane deshalb keinen aufwendigen Kopierschutz.

---

## 14. Recht (DE/EU), Checkliste

Keine Rechtsberatung. Vor dem Start von einem Anwalt oder einer Anwältin für IT-Recht prüfen lassen.

- [ ] Impressum (§5 DDG), Angaben zum Unternehmen, Kontakt, ggf. USt-IdNr.
- [ ] Datenschutzerklärung (DSGVO): Auftragsverarbeitungsverträge mit Stripe, Hosting, E-Mail-Dienst. Die HWID-Hashes sind pseudonyme Daten und gehören in die Erklärung.
- [ ] AGB und Widerrufsbelehrung für digitale Inhalte: Vor dem Kauf eine ausdrückliche Zustimmung zum Beginn der Ausführung und zum Verlust des Widerrufsrechts (§356 Abs. 5 BGB) einholen und protokollieren.
- [ ] Preisangaben brutto, Hinweis auf Umsatzsteuer.
- [ ] Haftungsausschluss/Hinweise zu Systemänderungen, Wiederherstellungspunkt, Datensicherung.
- [ ] Gewerbeanmeldung, Steuerliche Erfassung.
- [ ] Marken- und Domainprüfung (2.2).
- [ ] Lizenzen Dritter: `THIRD-PARTY-NOTICES.md` (PresentMon, WPF-UI, BouncyCastle, Velopack, Serilog, TaskScheduler).
- [ ] Affiliate-Cookie nur mit Einwilligung (§25 TDDDG).

---

## 15. Tests

### 15.1 Automatisiert

- **Unit-Tests (xUnit, NSubstitute):** Manifest-Parsing und -Validierung, Kompatibilitätsfilter, Topologische Sortierung, Journal lesen/schreiben, Allowlist-Validierung (negativ: Pfad außerhalb → Exception), Token-Prüfung (gültig, abgelaufen, falsche HWID, falsche Signatur), Benchmark-Berechnungen (feste CSV-Fixtures mit erwarteten Werten).
- **Handler-Tests:** Registry gegen einen Testschlüssel unter `HKCU\Software\kozy-test`. Capture → Apply → Verify → Restore muss exakt den Ausgangszustand liefern.
- **Backend-Integrationstests:** Testcontainers mit PostgreSQL: Webhook → Lizenz → Aktivierung → Gerätelimit → Refresh → Revoke. Stripe-Webhook-Signatur mit Testschlüssel.
- **Web:** Playwright-E2E (Produkt → Checkout-Mock → Erfolgsseite), Lighthouse-CI.

### 15.2 VM-Matrix (Hyper-V, Skript `tests/vm/run.ps1`)

Zu testen auf: Win10 22H2, Win11 23H2, Win11 24H2, Win11 25H2; je Home und Pro; je als Desktop und als Laptop-Profil (emulierte Batterie nicht möglich, deshalb `deviceClass` per Mock testen). Ablauf je VM:

1. Checkpoint „clean" wiederherstellen.
2. `reg export` der betroffenen Zweige als Vorher-Dateien.
3. `kozy.exe --headless --apply preset:max --no-restore-point`, Neustart.
4. `kozy.exe --headless --detect --json`: alle kompatiblen Tweaks = `Applied`.
5. `kozy.exe --headless --revert <sessionId>`, Neustart.
6. `reg export` erneut, **normalisierter Diff gegen Vorher muss leer sein** (ausgenommen `irreversible` Tweaks).

### 15.3 Manuelle Tests

- Zwei echte PCs (je einer mit NVIDIA und AMD), ein Laptop. Spiele mit Anti-Cheat (z. B. Valorant/Vanguard, FACEIT) müssen nach Anwendung von `preset:balanced` weiterhin starten.
- Stromausfall während Apply (VM hart ausschalten): Beim nächsten Start erkennt die App eine unvollständige Sitzung im Journal und bietet Rollback an.

---

## 16. CI/CD und Release

| Workflow | Trigger | Schritte |
|---|---|---|
| `app.yml` | Push/PR | restore, build, test, Branding-Grep, `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` |
| `release.yml` | Tag `v*` | wie `app.yml`, dann `vpk pack`, Signieren (signtool oder Trusted Signing), SHA-256-Datei, Upload in Objektspeicher, GitHub Release (privat) |
| `packs.yml` | Änderung in `packs/` | Schema-Validierung aller Manifeste, `kozy-pack build`, Signieren mit `PACK_SIGNING_KEY` aus CI-Secret, Upload, Manifest aktualisieren (Kanal `beta`; Befördern nach `stable` per manuellem Approval) |
| `server.yml` | Push/PR/Tag | build, test, Docker-Image, Deploy per SSH/Compose |
| `web.yml` | Push/PR | lint, typecheck, Playwright, Deploy |

Versionierung: SemVer. Kanäle `beta` (du und Tester) und `stable`. Rollback eines Packs = vorherige Manifest-Version wieder als `stable` setzen.

---

## 17. Phasenplan mit Tasks und Abnahmekriterien

Zeitschätzung gesamt: **ca. 15 Wochen** bis zur öffentlichen Beta.

### Phase 0: Setup (2–3 Tage)

| ID | Task | Ergebnis |
|---|---|---|
| T-01 | Monorepo nach Kapitel 4 anlegen, `global.json`, `Directory.Build.props` mit Branding | Repo baut leer |
| T-02 | `.editorconfig`, Analyzer, nullable aktiv, `Deterministic` | Build ohne Warnungen |
| T-03 | CI-Grundgerüst (`app.yml`) inkl. Branding-Grep | grüner Lauf |
| T-04 | Markenrecherche (2.2) und Domain sichern | Entscheidung dokumentiert |

**AK:** `dotnet build` und `dotnet test` grün in CI; Branding-Grep schlägt bei Testwort `risxn` fehl.

### Phase 1: Engine-Kern (2 Wochen)

| ID | Task | Ergebnis |
|---|---|---|
| T-05 | Modelle: `TweakManifest`, `ActionBase` + Subtypen, `TweakState`, `Journal*` (System.Text.Json, Source-Generation) | Serialisierung getestet |
| T-06 | JSON-Schema-Datei `schema/tweak.v1.json` + Validator in `Kozy.Cli` | ungültige Manifeste werden mit Zeile/Feld abgelehnt |
| T-07 | `CompatibilityService`: Build, Edition (Registry `EditionID`), Gerätetyp, GPU-Hersteller (WMI) | Unit-Tests mit Mock-Systeminfo |
| T-08 | `DependencyResolver` (topologisch, Konfliktprüfung) | Unit-Tests inkl. Zyklus-Fehler |
| T-09 | `JournalStore` (Schreiben atomar: temp + move) | Absturztest: halbe Datei wird nie als gültig gelesen |
| T-10 | `Engine.Apply` / `Engine.Revert` nach 5.5 | Tests mit Fake-Handlern: Rollback bei Fehler |
| T-11 | `RestorePointService` (`Checkpoint-Computer`, Hinweis: Windows begrenzt auf 1 Punkt/24 h, Registry `SystemRestorePointCreationFrequency` nicht ändern, sondern Warnung zeigen) | Fehler wird als Warnung gemeldet |

**AK:** Alle Engine-Tests grün. Fake-Handler-Szenarien: Apply ok, Apply fehlgeschlagen mit Rollback, Revert, Teilzustand.

### Phase 2: Handler (1,5 Wochen)

| ID | Task | Ergebnis |
|---|---|---|
| T-12 | `RegistryHandler` inkl. Allowlist (5.6), 64-Bit-Sicht, alle Wertarten | Capture/Restore-Roundtrip-Test |
| T-13 | `ServiceHandler` (WMI lesen, `sc.exe config` schreiben) | Test auf VM |
| T-14 | `TaskHandler` (TaskScheduler-Bibliothek) | Test auf VM |
| T-15 | `PowercfgHandler` (query-Parser für „Current AC Power Setting Index", Schema duplizieren, aktivieren, löschen) | Parser-Tests mit echten Ausgaben (DE + EN Windows) |
| T-16 | `BcdeditHandler` (Parser für `bcdedit /enum {current}`, deutsche/englische Ausgabe) | Parser-Tests |
| T-17 | `DnsHandler` (Adapterliste, Set/Reset) | Test auf VM |
| T-18 | `AppxHandler` (Entfernen, geschützte Liste hart im Code, keine Entfernung bei Treffer in der Schutzliste) | Negativtest |
| T-19 | `CleanerHandler` (Größe berechnen, Löschen mit Fehlertoleranz bei gesperrten Dateien) | Test mit Temp-Fixtures |
| T-20 | `SpiMouseHandler` | Test: Wert wirkt ohne Neuanmeldung |

**AK:** Für jeden Handler existiert ein automatisierter Roundtrip-Test (Capture → Apply → Verify → Restore = Ausgangszustand).

### Phase 3: Packs und Seed-Katalog (2 Wochen)

| ID | Task | Ergebnis |
|---|---|---|
| T-21 | Alle Tweaks aus Kapitel 6 als Manifeste unter `packs/` | Validator grün |
| T-22 | `kozy-pack build` + Signierung (Ed25519, `kid`) | `manifest.json` + `.sig` |
| T-23 | Client: `PackRepository` (Laden, Signatur prüfen, Hash prüfen, Downgrade-Schutz, lokaler Cache `%ProgramData%\kozy\packs`) | Tests inkl. manipuliertem Pack |
| T-24 | Eingebettetes „Fallback-Pack" `basic` in der App (offline nutzbar) | App startet ohne Server |
| T-25 | Pack „Input & Netzwerk-Konsistenz" (`precision`) aus den Tweaks `input.*`, `net.nagle-off`, `net.dns-preset`, `gaming.mmcss-games`, `latency.*` | Pack gebaut |
| T-26 | Autostart-Manager (`StartupApproved`) | Test auf VM |

**AK:** Manipuliertes Pack (1 geändertes Byte) wird abgelehnt. VM-Matrix aus 15.2 für `preset:safe` ist grün (leerer Diff nach Revert).

### Phase 4: Desktop-UI (2,5 Wochen)

| ID | Task | Ergebnis |
|---|---|---|
| T-27 | Shell: Navigation, Theme, Lokalisierung, Fehlerdialog, Admin-Manifest (`requestedExecutionLevel="requireAdministrator"`) | App startet nur elevated |
| T-28 | Dashboard (Systemkarte per WMI) | Werte stimmen mit Task-Manager überein |
| T-29 | Tweaks-Ansicht mit Suche, Filter, Schalter, Voreinstellungen, Bestätigungsdialog | alle Statuszustände sichtbar |
| T-30 | Cleaner, Debloat, Autostart, Netzwerk-Ansichten | je mit Lade-/Fehlerzustand |
| T-31 | Verlauf mit Revert pro Sitzung/Tweak | Revert-Test auf VM |
| T-32 | Einstellungen, Sprachumschaltung, Log-Ordner | umschaltbar ohne Neustart |
| T-33 | CLI (`--headless …`, Exit-Codes aus Kapitel 7) | VM-Skript nutzt CLI |

**AK:** Alle Screens aus Kapitel 7 vorhanden, tastaturbedienbar, getestet bei 100 % und 150 % Skalierung.

### Phase 5: Benchmark (1 Woche)

| ID | Task | Ergebnis |
|---|---|---|
| T-34 | PresentMon-Wrapper (Prozess starten, CSV lesen, Timeout) | Fixture-Tests |
| T-35 | Auswertung nach 8.1 (beide „1% low"-Varianten getrennt) | Tests gegen handgerechnete Werte |
| T-36 | Ping-Messung nach 8.2 | Test gegen Loopback |
| T-37 | UI: Vorher/Nachher, 3 Läufe, Median, Export JSON/CSV | Export stimmt mit Anzeige überein |

**AK:** Handgerechneter Beispiel-Datensatz liefert exakt die erwarteten Kennzahlen.

### Phase 6: Lizenzierung (2 Wochen)

| ID | Task | Ergebnis |
|---|---|---|
| T-38 | DB-Migrationen nach 9.1 | `dotnet ef database update` ok |
| T-39 | Schlüsselgenerator, Hashing, Token-Signierung (`lic-…` Schlüssel) | Unit-Tests |
| T-40 | Endpunkte `activate`, `refresh`, `deactivate` inkl. Rate-Limit | Integrationstests |
| T-41 | Stripe-Webhook (Signatur, Idempotenz, Refund/Dispute) | Test mit Stripe-CLI |
| T-42 | E-Mail-Versand (Template de/en) | Testmail kommt an |
| T-43 | Client `Kozy.Licensing`: HWID, Aktivierung, Refresh, DPAPI-Speicherung, gesperrter Modus | Offline-Test mit abgelaufenem Token |
| T-44 | Pack-Download-Endpunkte mit Entitlement-Prüfung und kurzlebiger URL | falsches Tier → 403 |

**AK:** Gerätelimit funktioniert (2. Gerät → `DEVICE_LIMIT_REACHED`), Revoke wirkt spätestens beim nächsten Refresh, Offline-Betrieb bis `exp` möglich.

### Phase 7: Website und Checkout (2 Wochen)

| ID | Task | Ergebnis |
|---|---|---|
| T-45 | Next.js-Projekt, Brand-Config, i18n, Layout | Lighthouse-Ziele erreicht |
| T-46 | Seiten nach 10.1 (eigene Inhalte, `TODO-CONTENT` markiert) | alle Routen erreichbar |
| T-47 | Checkout-Anbindung (`/api/checkout`, Erfolgs-/Abbruchseite), Widerrufs-Checkbox mit Protokollierung | Testkauf im Stripe-Testmodus liefert Lizenzmail |
| T-48 | Rechtsseiten nach Anwaltsvorlage | Checkliste Kapitel 14 abgehakt |

**AK:** End-to-End: Testkauf → Mail → Installer → Aktivierung → Tweak anwenden, ohne manuelle Eingriffe.

### Phase 8: Admin, Affiliate, Service (1,5 Wochen)

| ID | Task | Ergebnis |
|---|---|---|
| T-49 | Admin-UI (Login + TOTP, Suche nach E-Mail, Lizenz sperren/zurücksetzen, manuell ausstellen) | alle Aktionen im `audit_log` |
| T-50 | Affiliate-Tabellen, Cookie, Provisionsbericht | Testbestellung ordnet Code zu |
| T-51 | `--report` und PDF-Bericht für Einzelsitzungen | PDF enthält Hardware + Vorher/Nachher |
| T-52 | Buchungssystem verlinken | Termin buchbar |

### Phase 9: Härtung, Release, optional (1 Woche + laufend)

| ID | Task | Ergebnis |
|---|---|---|
| T-53 | Code-Signing in CI, SHA-256-Datei, VirusTotal-/Microsoft-Prüfung | Release ohne Fehlalarm bei großen Engines |
| T-54 | VM-Matrix (15.2) für `preset:balanced` und `preset:max` | Diff leer |
| T-55 | Absturz-/Stromausfall-Test, Rollback-Angebot beim nächsten Start | funktioniert |
| T-56 | Penetrationscheck Backend (Rate-Limits, Webhook-Spoofing, Token-Manipulation, SQL-Injection-Tests) | Bericht, Findings behoben |
| T-57 | KANN: Discord-Bot (Lizenz-Lookup für Support, Rolle nach Kauf) | optional |
| T-58 | KANN: Guide-Produkte (PDF) für Xbox/PlayStation, Auslieferung per Download-Link | optional |
| T-59 | Geschlossene Beta (10–20 Tester), Feedback, Fixes | Beta-Report |

**AK Gesamtprojekt:** siehe Definition of Done.

---

## 18. Offene Entscheidungen (vor Phase 0 klären)

1. **Endgültiger Produktname** (Arbeitsname `kozy`) und Ergebnis der Markenrecherche.
2. **Preismodell:** Einmalkauf oder Abo? Der Plan geht von Einmalkauf pro Tier mit lebenslanger Lizenz aus (`expires_at = null`). Abo erfordert nur `expires_at` + Stripe-Subscriptions.
3. **Rechtsform und Steuer** (Kleinunternehmer vs. USt-pflichtig), Einfluss auf Stripe Tax.
4. **Eigener Shop vs. Shopify:** Der Plan baut einen eigenen Shop. Shopify wäre schneller zu starten, Lizenzvergabe liefe dann über einen Webhook an dein Backend.
5. **Windows 10:** weiter unterstützen oder streichen?
6. **Zielgruppe der Tweaks:** nur Gaming oder auch Büro/Laptop? Laptops brauchen konservativere Energie-Tweaks.
7. **Umfang des Service:** Wer führt Einzelsitzungen durch, in welchen Zeiten?

---

## 19. Definition of Done (Gesamtprodukt)

- [ ] Alle Phasen-AK erfüllt.
- [ ] VM-Matrix: Revert stellt den Ausgangszustand her (leerer Registry-Diff).
- [ ] Alle ausgelieferten Binärdateien signiert; Hashes veröffentlicht.
- [ ] Keine Fehlalarme bei den großen Virenscannern auf VirusTotal (oder dokumentierte Meldung an die Hersteller).
- [ ] Testkauf im Livemodus mit eigener Karte, danach Rückerstattung → Lizenz wird gesperrt.
- [ ] Alle Website-Zahlen stammen aus eigenen Messungen nach Protokoll 8.3.
- [ ] Rechtsseiten durch Anwalt freigegeben.
- [ ] Branding-Grep sauber, kein Text/Bild/Code aus der Referenz im Repo.
- [ ] Support-Prozess steht (Kontaktadresse, Antwortzeiten, Rückerstattungsprozess).
