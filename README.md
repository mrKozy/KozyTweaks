<div align="center">

# ⚡ kozy — Modern Windows Optimizer

**100% Free & Open-Source Windows Performance, Latency & Gaming Optimizer**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0%20LTS-purple.svg)](https://dotnet.microsoft.com/)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg)](https://microsoft.com/windows)
[![Tests](https://img.shields.io/badge/Tests-75%20Passing-brightgreen.svg)]()
[![Fluent Design](https://img.shields.io/badge/UI-Fluent%20Design%20(WPF--UI)-512BD4.svg)](https://github.com/lepoco/wpfui)

*Ein sicheres, datengetriebenes Windows-Optimierungstool mit automatischer Systemwiederherstellung, Journaling und 1-Klick-Rollback.*

[Features](#-features) • [Sicherheitsgarantie](#-sicherheitsgarantie) • [Schnellstart](#-schnellstart) • [Tweak-Katalog](#-tweak-katalog) • [Architektur](#-architektur) • [Lizenz](#-lizenz)

</div>

---

## 🚀 Übersicht

Viele existierende Tuning-Tools sind undurchsichtige Batch- oder PowerShell-Skripte, die Registry-Werte unwiderruflich überschreiben und bei Systemproblemen schwer zu debuggen sind.

**kozy** geht einen modernen, sicheren Weg:
- **Datengetriebene Manifeste**: Alle Optimierungen sind in typisierten JSON-Dateien definiert – kein ungeprüfter Shell-Code.
- **Zustandserfassung (Capture)**: Vor jeder Änderung wird der Ist-Zustand exakt gemerkt.
- **Transparenz & Rollback**: Jede Aktion landet in einer Journal-Datei und kann jederzeit mit **einem Klick** auf den Ursprungszustand zurückgesetzt werden.
- **100 % Kostenlos & Open Source**: Keine Paywalls, keine Lizenzen, kein Tracking.

---

## ✨ Features

### ⚡ 23+ Kuratierte System-Optimierungen
* **Gaming & Frametimes**: MMCSS-Spielepriorisierung, Hardware-Accelerated GPU Scheduling (HAGS), GameDVR-Deaktivierung.
* **Input-Latenz**: Echte 1:1-Mausabtastung (Abschaltung von Windows-Mausbeschleunigung via Registry & `SystemParametersInfo`), Tastatur-Reaktionszeiten auf Maximum, Deaktivierung von Einrastfunktion/Filtertasten.
* **Energie & CPU**: Automatische Duplizierung & Aktivierung des „Ultimative Leistung"-Energieplans, Deaktivierung von CPU-Core-Parking, USB-Selective-Suspend und PCIe-ASPM.
* **Netzwerk**: DNS-Presets (Cloudflare 1.1.1.1 / 1.0.0.1) mit 1 Klick, Multimedia-Netzwerk-Throttling deaktivieren.
* **Windows-Debloat & Telemetrie**: Abschaltung von Diagnostics-Tracking (DiagTrack), SysMain (SuperFetch für SSDs/NVMes), Hintergrund-Aufgaben und Entfernung von vorinstallierter Windows-Bloatware.
* **Visuelle Effekte**: Abschaltung von DWM-Transparenzeffekten und Fenster-Animationen für maximale Reaktionsgeschwindigkeit.

### 🧹 Sicherer System-Cleaner
* Findet und bereinigt gefahrlos temporäre Anwendungsdaten (`%TEMP%`), Windows-Temp, DirectX-Shader-Caches, NVIDIA-Shader-Caches und Windows Update Download-Reste.
* Exakte Größenberechnung vorab.

### 🚀 Autostart-Manager
* Liest Autostart-Einträge aus `HKCU` und `HKLM`.
* Schaltet Programme über Windows `StartupApproved` (0x02 aktiv / 0x03 inaktiv) ohne Einträge destruktiv zu löschen.

### 🕒 Vollständige Historie & 1-Klick-Rollback
* Ansicht aller vergangenen Optimierungs-Sitzungen.
* 1-Klick-Rücknahme einer gesamten Sitzung oder einzelner Tweaks.
* Schneller Direktzugriff auf die Windows-Systemwiederherstellung (`rstrui.exe`).

---

## 🛡️ Sicherheitsgarantie

1. **Wiederherstellungspunkt**: Vor der ersten Optimierung wird automatisch ein nativer Windows-Wiederherstellungspunkt angelegt (`Checkpoint-Computer`).
2. **Registry-Allowlists**: kozy schreibt ausschließlich in sichere, geprüfte Pfade. Sensible Systembereiche (`HKLM\SAM`, `SECURITY`, Defender, UAC) sind systemweit blockiert.
3. **Keine Cheat- oder Kernel-Eingriffe**: Keine Treiber-Modifikationen, kein Speicher-Hooking – 100 % sicher für Spiele mit Anti-Cheat (z. B. Riot Vanguard, Easy Anti-Cheat, BattlEye).

---

## 💻 Schnellstart & Bauanleitung

### Voraussetzungen
* **Betriebssystem**: Windows 10 (22H2+) oder Windows 11 (23H2, 24H2, 25H2) – x64
* **SDK**: [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* **Rechte**: Als Administrator ausführen (für Registry-, Energie- und Dienst-Optimierungen erforderlich)

### Kompilieren & Ausführen

```powershell
# Repository klonen
git clone https://github.com/your-username/kozy.git
cd kozy

# Solution bauen
dotnet build

# Tests ausführen (75 automatisierte Tests)
dotnet test

# Desktop-App starten
dotnet run --project src/Kozy.App
```

### Headless CLI-Modus

Für Skripte, Admins und unbeaufsichtigte Installationen:

```powershell
# Manifeste prüfen
dotnet run --project src/Kozy.Cli -- validate ./packs

# Packs bauen und signieren
dotnet run --project src/Kozy.Cli -- build ./packs --out ./dist/packs --version 1.0.0 --kid default-key --key <base64-key>
```

---

## 📁 Repository-Struktur

```
kozy/
├── packs/                     # 23+ JSON-Manifeste für alle Tweaks
│   ├── gaming/               # HAGS, GameDVR, MMCSS
│   ├── input/                # Mausbeschleunigung, Tastaturverzögerung
│   ├── power/                # Ultimative Leistung, Core-Parking, USB/PCIe-Energie
│   ├── visual/               # Transparenz, Fenster-Animationen
│   ├── network/              # DNS-Presets (Cloudflare)
│   ├── services/             # SysMain, DiagTrack
│   ├── tasks/                # Telemetrie-Aufgaben
│   ├── debloat/              # Standard Windows-Apps
│   └── latency/              # BCD Platform Clock, Dynamic Tick
├── src/
│   ├── Kozy.App/             # Moderne WPF Fluent UI (Dark Mode)
│   ├── Kozy.Core/            # Engine, Models, Kompatibilität, JournalStore
│   ├── Kozy.Actions/         # 9 spezialisierte System-Handler (Registry, Service, BCD...)
│   ├── Kozy.Benchmark/       # PresentMon & Ping-Messungen
│   └── Kozy.Cli/             # kozy CLI
├── tests/
│   ├── Kozy.Core.Tests/      # Engine-, Journal- und Pack-Tests
│   └── Kozy.Actions.Tests/   # Handler-Roundtrip-Tests & StartupManager-Tests
└── docs/                     # Technische Dokumentationen
```

---

## 📄 Lizenz

Dieses Projekt ist unter der **[MIT-Lizenz](LICENSE)** lizenziert – frei zur privaten und kommerziellen Nutzung.
