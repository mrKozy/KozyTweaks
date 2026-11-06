# Kozy Pack-Infrastruktur

Die Pack-Infrastruktur ermöglicht signierte, verifizierbare Updates von Tweak-Sammlungen (Packs) für verschiedene Tier-Level (Basic, Pro, Extreme).

## Architektur-Überblick

```
┌─────────────────────┐
│  Pack-Build Server  │
│   (kozy-pack CLI)   │
└──────────┬──────────┘
           │ 
           │ 1. Build & Sign
           ▼
    ┌──────────────┐
    │  manifest.   │
    │    json      │ ◄─── Ed25519-Signatur
    │  + Signatur  │
    └──────┬───────┘
           │
           │ 2. Upload
           ▼
    ┌──────────────┐
    │ Pack-Repository│
    │  (HTTP/HTTPS) │
    └──────┬───────┘
           │
           │ 3. Download
           ▼
    ┌──────────────┐
    │  Kozy Client │
    │ (Update CLI) │
    └──────────────┘
           │
           │ 4. Verify & Install
           ▼
    ┌──────────────┐
    │  Installierte│
    │     Packs    │
    └──────────────┘
```

## Komponenten

### 1. PackBuilder

**Zweck**: Baut tierbasierte Packs aus Tweak-Manifesten

**Eingabe**: Verzeichnis mit Tweak-JSON-Dateien  
**Ausgabe**: 
- `manifest.json` (Metadaten + Hashes)
- `manifest.json.sig` (Ed25519-Signatur)
- `pack-{tier}-{version}.json` (gruppierte Tweaks)

**Verwendung**:
```powershell
kozy-pack build ./packs --out ./dist --version 1.0.0 --kid pack-2026-01 --key base64:...
```

**Optionen**:
- `--out`: Ausgabeverzeichnis
- `--version`: Pack-Version (Semantic Versioning)
- `--kid`: Key-ID für Signatur
- `--channel`: Channel-Name (Standard: stable)
- `--key`: Signaturschlüssel (Base64/Base64URL/Hex) oder `PACK_SIGNING_KEY` Umgebungsvariable

### 2. PackSigningService

**Zweck**: Ed25519-Signierung und -Verifikation

**Features**:
- Ed25519-Signaturen (64 Bytes)
- Mehrere Schlüsselformate: Base64, Base64URL, Hex
- Öffentlicher Schlüssel aus privatem Schlüssel ableitbar
- 32-Byte und 64-Byte private Schlüssel unterstützt

**API**:
```csharp
// Signieren
var signature = PackSigningService.SignBytes(data, privateKey);

// Verifizieren
var isValid = PackSigningService.VerifyBytes(data, signature, publicKey);

// Public Key ableiten
var publicKey = PackSigningService.GetPublicKey(privateKey);
```

### 3. PackVerificationService

**Zweck**: Client-seitige Verifikation von Packs

**Sicherheitsprüfungen**:
1. **Kid-Prüfung**: Key-ID muss bekannt sein
2. **Signatur-Verifikation**: Ed25519-Signatur über kanonisches JSON
3. **SHA-256-Hash-Verifikation**: Jedes Pack gegen Manifest-Hash
4. **Downgrade-Schutz**: Version nicht kleiner als installiert
5. **Manifest-Integrität**: JSON-Deserialisierung + Schema-Prüfung

**Registrierung öffentlicher Schlüssel**:
```csharp
PackVerificationService.RegisterPublicKey("pack-2026-01", publicKeyBytes);
```

**Verifikation**:
```csharp
var manifest = PackVerificationService.VerifyAndLoad(
    manifestJson,
    signatureBase64,
    packFiles,
    installedVersions // Optional: Downgrade-Schutz
);
```

### 4. PackUpdateService

**Zweck**: Download und Cache-Management

**Features**:
- HTTP-Download von Manifest + Packs
- Bearer-Token-Authentifizierung
- Lokaler Cache für Offline-Nutzung
- Vollständige Verifikation vor Cache-Verwendung

**API-Endpunkte** (Server-seitig):
- `GET /v1/packs/manifest?channel={channel}` → `manifest.json` + Header `X-Signature`
- `GET /v1/packs/{tier}/{version}` → Pack-Datei

**Verwendung**:
```csharp
var service = new PackUpdateService(httpClient, cacheDirectory);

// Download + Verifikation
var result = await service.FetchAndVerifyAsync(
    "https://packs.kozy.app/v1",
    "stable",
    bearerToken: "..."
);

// Cache laden
var cached = await service.LoadFromCacheAsync("stable");
```

### 5. kozy-pack CLI

**Befehle**:

```bash
# Tweak-Manifeste validieren
kozy-pack validate ./packs

# Packs bauen und signieren
kozy-pack build ./packs --out ./dist --version 1.0.0 --kid pack-2026-01

# Packs herunterladen und verifizieren
kozy-pack update --uri https://packs.kozy.app/v1 --channel stable --token ...

# Dry-Run (ohne Installation)
kozy-pack update --dry-run
```

## Manifest-Format

### manifest.json
```json
{
  "channel": "stable",
  "generatedAt": "2026-11-05T12:00:00Z",
  "kid": "pack-2026-01",
  "packs": [
    {
      "tier": "basic",
      "version": "1.0.0",
      "sha256": "a3c8f9...",
      "size": 12345,
      "file": "pack-basic-1.0.0.json"
    },
    {
      "tier": "pro",
      "version": "1.0.0",
      "sha256": "b4d9e2...",
      "size": 23456,
      "file": "pack-pro-1.0.0.json"
    }
  ]
}
```

### manifest.json.sig
Base64-kodierte Ed25519-Signatur über kanonisches JSON des Manifests.

## Sicherheitsmodell

### Schlüsselverwaltung

**Produktions-Schlüsselpaar**:
- Private Key: Sicher auf Build-Server gespeichert (z.B. Azure Key Vault, AWS Secrets Manager)
- Public Key: Im Client hart codiert oder über sichere Konfiguration geladen
- Key Rotation: Unterstützt durch `kid` (Key-ID)

**Empfohlene Speicherorte**:
- Build-Server: Umgebungsvariable `PACK_SIGNING_KEY` (CI/CD Secret)
- Client: `PackVerificationService.RegisterPublicKey()` beim Start

### Angriffsvektoren und Mitigationen

| Angriff | Mitigation |
|---------|-----------|
| Manipuliertes Manifest | Ed25519-Signatur-Verifikation |
| Manipuliertes Pack | SHA-256-Hash-Verifikation |
| Man-in-the-Middle | HTTPS + Certificate Pinning (optional) |
| Downgrade-Angriff | Version-Vergleich mit installierter Version |
| Key-Kompromittierung | Key-Rotation via `kid` + neue Public Key Distribution |
| Replay-Angriff | `generatedAt` Timestamp (Client kann Alter prüfen) |

### Best Practices

1. **Produktionsschlüssel**:
   - Niemals im Repository committen
   - Nur auf Build-Server verfügbar
   - Regelmäßige Rotation (z.B. jährlich)

2. **Signatur-Verifikation**:
   - Immer vor Installation
   - Cache-Inhalte auch verifizieren
   - Bei Fehler: Update abbrechen

3. **Netzwerk**:
   - HTTPS für alle Downloads
   - Bearer-Token für authentifizierte Channels
   - Timeout-Limits (Standard: 5 Minuten)

4. **Version-Management**:
   - Semantic Versioning (Major.Minor.Patch)
   - Downgrade standardmäßig verboten
   - Pre-Release-Suffix erlaubt (z.B. `1.0.0-beta`)

## Tests

Alle Komponenten sind vollständig getestet:

### PackBuilderTests (3 Tests)
- ✅ Valide Packs bauen
- ✅ Nicht-existentes Verzeichnis
- ✅ Leeres Verzeichnis

### PackVerificationTests (7 Tests)
- ✅ Gültige Signatur + Hashes
- ✅ Ungültige Signatur
- ✅ Falscher Pack-Hash
- ✅ Downgrade-Versuch
- ✅ Unbekannte Key-ID
- ✅ Einzelner Hash-Check (valide)
- ✅ Einzelner Hash-Check (invalide)

### PackUpdateServiceTests (7 Tests)
- ✅ Gültiger Download + Verifikation
- ✅ Fehlender Signature-Header
- ✅ Ungültiger Pack-Hash
- ✅ Bearer-Token wird übergeben
- ✅ Cache laden funktioniert
- ✅ Fehlender Cache
- ✅ Cache löschen

### PackInstallationServiceTests (5 Tests)
- ✅ Gültiges UpdateResult installieren + Version speichern
- ✅ Fehlende Pack-Datei wird als Fehler markiert
- ✅ Leeres Pack erfolgreich mit 0 angewendet
- ✅ StopOnError bricht nachfolgende Packs bei Fehler ab
- ✅ GetInstalledVersions bei fehlender/korrupter Datei

**Gesamt: 22/22 Tests ✅**

## Roadmap / TODO

- [x] Pack-Installation implementieren (Phase 5.8)
- [ ] Server-seitiges Manifest-Repository
- [ ] Automatische Update-Prüfung im Client
- [ ] Rollback-Mechanismus
- [ ] Delta-Updates (nur geänderte Packs herunterladen)
- [ ] Certificate Pinning für zusätzliche MITM-Sicherheit
- [ ] Telemetrie für Update-Erfolgsrate

## Lizenz

Dieses Projekt verwendet **Org.BouncyCastle.Cryptography** (MIT License) für Ed25519-Kryptografie.
