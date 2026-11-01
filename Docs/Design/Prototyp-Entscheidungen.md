# Iron Syndicate – Prototyp-Entscheidungen (Phase 1)

Grundlage ist das Entwicklerhandbuch (`Iron_Syndicate_Entwicklerhandbuch.docx`).
Systeme, Architektur, Testschwellen und Tuningvorschläge werden übernommen.
Inhalte, die eine bestehende Spielvorlage direkt wiedergeben, werden eigenständig
gestaltet. Diese Datei hält die Abweichungen und Arbeitsentscheidungen fest.

## Übernommen wie im Handbuch

- Szenenfluss Boot → Fahrzeugwahl → Match, Testlabor über denselben Match-Einstieg
- `VehicleCommand` pro Sitz, Verarbeitung pro Physikschritt, Feuerflanken einmal konsumiert
- Datenmodell `VehicleDefinition` / `WeaponDefinition` / `StageDefinition`
- Fahrmodell (Rigidbody, Bodenabfrage, Seitenhaftung, Bremse → Rückwärts, Drift, Nitro-Tank)
- `DamageEvent` mit Treffer-ID, Explosion trifft jedes Fahrzeug einmal
- Ein Leben pro Teilnehmer, Unentschieden bei gleichzeitigem Ausscheiden
- Overdrive 100 Einheiten, 8/s, Kosten 15 / 30 / 45 / 75, Farbschwelle 50
- Pickups (Nano-Patch 25 %, Core Overcharge, Nitro-Cell), 20 s Respawn
- Brand, Stase (3 s, 3 Bordkanonentreffer), Löschwege
- Acht Pickup-Waffen mit Namen und Ladungen
- Fahrzeugnamen, Fahrer, Fahrzeugtypen und Menüwerte
- Bot-Regeln (Rückzug < 30 %, Angriff ab 55 %, Steckenbleiben nach 2 s)
- Laptop-Tastenbelegung: WASD, Leertaste Drift, Umschalt Nitro, Maus Feuer, Q/E Waffenwechsel, F Spezial

## Eigene Kombo-Eingaben

Gleiche Fähigkeiten und Kosten wie im Handbuch, eigene Richtungsfolgen.
Rückwärts-Varianten bestehen aus dem Rear-Salvo-Präfix plus Grundfolge; die
längere Folge hat Vorrang und löst kein zusätzliches Rear Salvo aus.

| Manöver | Folge | Kosten |
|---|---|---|
| Thruster Hop | ↓ ↓ ↑ | 15 |
| Aegis Barrier | ← ↓ → | 30 |
| Quick-Mine | → ↓ ← | 30 |
| Thermite Burst | ↓ ← ↑ | 30 |
| Cryo-Pulse | ↓ → ↑ | 45 |
| Phantom Cloak | ↑ ← ↓ → | 75 |
| Rear Salvo | ↑ ↓ ↓ | 1 Waffe |
| Rear Cryo-Pulse | ↑ ↓ ↓ ↓ → ↑ | 45 |
| Rear Thermite | ↑ ↓ ↓ ↓ ← ↑ | 30 |
| Cerberus Barrage | → → ← ← + Primärfeuer halten | 100 |
| Scrap-to-Repair | ← ↑ → ↓ ← ↑ → ↓ | alle Waffen, +5 % HP je Ladung |

## Eigene Spezialfähigkeiten

Archetyp und Rolle bleiben erhalten, die Mechanik ist neu.

| Fahrzeug | Rolle | Spezial | Wirkung |
|---|---|---|---|
| Goliath Wheel | Allrounder, Nahkampf | Gyro Crush | Räder drehen hoch, gerader Sturmlauf; alles im Weg wird beschädigt und seitlich weggestossen |
| Sandviper | leicht, ausweichend | Dust Devil | Sandwolke hinter dem Fahrzeug; Verfolger verlieren Lock-On und Grip |
| Iron Jaw | schwerer Greifer | Wall Press | Schaufel fasst ein Ziel frontal und schiebt es; Schaden steigt, solange es gegen Geometrie gepresst wird |
| Nocturne | Zermürbung | Wraith Lantern | Laterne bindet den nächsten Gegner im Radius und entzieht ihm HP, die Nocturne heilen |
| Vortex F1 | Tempo | Slipstream Lance | Kurzer Hochgeschwindigkeitsstoss mit Energiespur, die querende Fahrzeuge beschädigt |
| Bastion MK-IV | Fernkampf | Mortar Grid | Markiert eine Zielzone voraus, nach 1,5 s schlagen sechs Granaten ein |
| Interceptor V8 | Verfolger | Spike Net | Netz mit Krallen; Ziel verliert Grip und kann kurz kein Nitro zünden |
| Chrome Igniter | Brand | Exhaust Inferno | Auspuff-Flammenstoss nach hinten, hinterlässt Brandspur |
| Phase GT | Täuschung | Phase Shift | 1,5 s nicht greifbar für Fahrzeuge und Geschosse, Austritt verursacht Schockschaden |
| Rustfang | Jäger | Chain Harpoon | Harpune hakt ein Ziel ein und schleppt es nach; Wandkontakt verursacht Schaden |
| Bonechaser | Drive-by | Scythe Sweep | Seitliche Klingen schädigen Fahrzeuge an beiden Flanken beim Vorbeifahren |
| Mammoth 4x4 | Rammbock | Battering Charge | Rammschild für 4 s; nächster Aufprall verursacht tempoabhängig hohen Schaden und kippt das Ziel |
| Jester Van (Secret) | Artillerie | Fireworks Finale | Fächer aus fünf Feuerwerksraketen, die in Splitterladungen zerfallen |
| Leviathan APC (Boss) | Festung | Siege Lock | Stützen ausfahren, unbeweglich; geladener Schienenstrahl auf lange Distanz |

### Endboss Dread Jester (überarbeitetes Konzept)

Kein Eiswagen und kein Clownskopf. Neues Konzept: ein gigantischer gepanzerter
Kirmes-Schwertransporter.

- Phase 1 (`dread_ram`): Transporter mit 2,5-fachem Rammschaden, Rammfahrt (3 s, 4-fach) auf
  langen Rammlinien
- Phase 2 (`hellfire_carousel`, eigenes Fahrzeug, 450 HP): Das brennende Karussell-Oberteil löst
  sich beim Tod des Transporters genau einmal als langsamer, rotierender Geschützturm und feuert
  alle 2,6 s Ringsalve plus gezielte Dreiersalve. Erst sein Tod beendet den Kampf.
  (Handbuch: schwebender Flammenkopf – ersetzt durch das Karussell.)

Jester Van wird passend dazu ein gepanzerter Schaustellerwagen.

## Arenen

Arenamechaniken (zerstörbare Brücke, Teleporter, Reaktor, Gefahrenzonen, Freilegungen)
dienen als Inspiration; Layouts, Wahrzeichen und Freischaltwege werden eigenständig gestaltet.
Freischaltcodes werden neu vergeben (siehe `Codes`).

### Tectonic Wreck (Stage 1, eigenes Layout)

Industriegebiet, durch einen Erdgraben gespalten (`Iron Syndicate/Build/Tectonic Wreck Scene`).

- Nord- und Südplateau, dazwischen 8 m tiefer Graben; zwei überdachte Abschnitte bilden die
  Strassentunnel, an beiden Enden führt der Graben über Rampen hinauf
- Übergänge: Tunneldächer, Endverbinder, Sprungschanze (Süd → Nord, ~22 m/s) und die obere
  Highway-Brücke, deren Mittelspannweite nur C4 bricht
- Teleporter im Sicherheitskäfig (Nord) führt hinauf auf das Highway-Deck
- Tesla-Reaktor mit vier Säulen; jede Säule gibt beim Zerstören einmalig eine Waffe frei
- Zerstörbare Buchstaben „WRECK“ (E und K verbergen Waffen), Depot mit Tor und zwei versteckten Waffen
- Wachturm mit Rampe und Core Overcharge auf dem Dach
- Fünf Startplätze, Aussenmauern 30 m hoch
- Fahrgraph (`DriveGraph`): Kanten Strasse, Sprung, Abwurf, Teleporter; zerstörte Objekte sperren
  bzw. öffnen Kanten, Bots planen neu (`replan` im Protokoll). Ziele in direkter Linie mit Boden
  dazwischen fahren Bots direkt an. Ohne Feindsicht über 12 s jagen Bots den nächsten Gegner.

### Testlabor (`Iron Syndicate/Build/Test Lab Scene`)

Neun Stationen an einem gemeinsamen Korridor: 1 Start mit Boss-Pad, 2 fallende Brücke,
3 Turm wird Strasse, 4 Statue öffnet Tunnel, 5 Glas öffnet Raum, 6 Lava, 7 Pool mit Lavastreifen
davor, 8 Abgrund mit Eisplatten (Vorwarnung) und sicherer Insel, 9 Zug (zerstörbar) und Schilf.
Vorgaben bleiben beim Zurücksetzen erhalten: Fahrzeug, Seed, Station, Inventar, Arenazustand.
Boss-Spawner startet den Bosskampf genau einmal nach 8 Abschüssen (simulierbar mit J).

Tasten: 1–9 Station · Bild↑/↓ Fahrzeug · O Bot · P Dummy · B Endboss · J Abschuss simulieren ·
I Inventar · H auffüllen · K Gegner entfernen · U Arenazustand · Strg+R zurücksetzen · Y neuer Seed · F1 Hilfe.

## Prüfungen

- EditMode (45): Kombos, Daten, Menücodes (positiv/negativ), Fahrgraph mit Brückeneinsturz und Depottor
- PlayMode (18, Handbuch Kap. 10 und Gefahrenmodule): PM-01 Brandübertragung, PM-02/03/04 Löschen durch
  Nitro/Aegis/Pool, PM-05 Stase auf Rampe (Schwerkraft bleibt), PM-06 Schildende 3 s, PM-07 Lock-On gegen
  Tarnung, PM-08 Schilf, PM-09 Teleport ohne Rücksprung, PM-10 Pickup-Respawn, PM-11 Lava nur im Volumen,
  PM-12 Abgrund, PM-13 Eisplatte, PM-14 Zug, PM-15 C4 Rampe/Ebene, PM-16 Bossphasen, PM-17 Boss-Spawner,
  PM-18 Rundenneustart
- Jeder PlayMode-Fall schreibt eine Zeile in `Logs/IronSyndicate/Pruefprotokoll_<Datum>.csv`
  (Fall, Build, Gerät, Ausgangslage, Handlung, Soll, Ist, Ergebnis, Fehler-ID)

## Balancing und Laptopmessung (Build)

- `IronSyndicate.exe -batchmode -nographics -balancing` – 91 Paarungen × 2 Startseiten × 3 Seeds
  (11, 22, 33) = 546 Duelle auf Tectonic Wreck; nach 180 s entscheidet der HP-Anteil. Ergebnis in
  `Balancing/<Zeit>/duelle.csv` und `zusammenfassung.md` neben der exe.
- `IronSyndicate.exe -perf` – 1280×720, 60 s Aufwärmen, 10 min Messung mit fünf Bots, Brückeneinsturz
  nach 3 min, Tesla alle 20 s; Ergebnis `Perf/perf_<Zeit>.md` (Mittel, 95./99. Perzentil, Frames > 33,3 ms).
  Diese Messung muss auf dem i7-Laptop laufen.

## Fahrzeugmodelle aus den Handbuch-Bildern

Pipeline: Hauptbild je Fahrzeug aus dem Handbuch ausschneiden (`ArtSource/Tools/crop_handbook.ps1`) →
ComfyUI TRELLIS.2 (Bild → 3D, `ArtSource/Comfy/batch_v2/`; TRELLIS reduziert selbst auf ~15 000 Flächen
*vor* dem Abwickeln und Backen, 2K-Texturen) → Blender (`ArtSource/Tools/process_vehicle.py`: keine weitere
Reduktion, Ursprung Bodenmitte, Texturen für URP: Grundfarbe mit leichter Sättigungs-/Helligkeitsanhebung,
Normal Map, Metallic/Glätte aus glTF-ORM mit Metall ×0,4, Occlusion) → `Assets/_Project/Art/Vehicles/<id>.fbx`
→ `Iron Syndicate/Build/Vehicle Bodies (generated models)` (eigenes URP-Lit-Material je Fahrzeug, Prefab,
Ausrichtung +Z, Länge aus dem Fahrzeugdatensatz). Alles neu verarbeiten: `ArtSource/Tools/reprocess_all.ps1`.

Erste Fassung (verworfen): Reduktion in Blender von 60 000 auf 12 000 Flächen verzerrte die UVs des
gebackenen Atlas, dazu war die Normal Map als Farbtextur importiert – Ergebnis war verrauschte Optik. Die Kollisionsbox bleibt die des Datensatzes; Räder sind Teil des Modells.
Jester Van und Dread Jester werden vorher mit FLUX.2 Klein zum Kirmes-Schwertransporter umgestaltet
(keine Clown- oder Eiswagenmotive). Fahrzeuge ohne Modell behalten den Grundkörper-Platzhalter.

## Leistung (Ruckeln)

- Radmodelle werden lokal unter dem interpolierten Fahrzeug gesetzt (vorher Weltposition im Physiktakt)
- Testprotokoll gepuffert, Konsolenausgabe nur für Rundenereignisse
- VSync an

## Codes

Nur im jeweiligen Menü erkannt. Tastatur: Pfeile, Q = L1, E = R1, T = Dreieck.
Im Entwicklerbuild ist der Kader ohnehin komplett freigeschaltet (`MatchSetup.RosterUnlocked`).

| Freischaltung | Folge |
|---|---|
| Jester Van | ← ← → → L1 R1 |
| Leviathan APC | ↓ ↑ ↓ ↑ R1 R1 |
| Arenacodes | folgen mit den Arenen (Phase 2) |

## Testbetrieb über MCP

- Play Mode läuft auch ohne Editorfokus weiter (`Iron Syndicate/Lab/Tick Play Mode In Background`).
- `BackgroundPlayLoop.RunFrames(n)` spult n Frames vor (für Bot-Runden aus dem Agenten heraus).
- Jede Runde schreibt ein CSV-Protokoll nach `Logs/IronSyndicate/` (Build-ID, Seed, Gerät, Ereignisse, Ergebnis je Fahrzeug).
- Bots steuern vorerst direkt mit Fühlern; der handgepflegte Fahrgraph kommt mit Tectonic Wreck.

## Platzhalter

Erster Durchstich nutzt Unity-Grundkörper, damit Systeme früh testbar sind.
Karosserien werden anschliessend aus Blender-Grundkörpern ersetzt (Quellen unter `ArtSource/`).
