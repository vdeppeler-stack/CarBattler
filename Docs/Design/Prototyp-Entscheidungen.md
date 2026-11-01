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

- Phase 1: Transporter mit extremem Rammschaden, sucht lange Rammlinien
- Phase 2: Das brennende Karussell-Oberteil löst sich als langsamer, rotierender
  Geschützturm („Hellfire Carousel“) und feuert wiederholte Salven. Erst sein Tod beendet den Kampf.

Jester Van wird passend dazu ein gepanzerter Schaustellerwagen.

## Arenen

Arenamechaniken (zerstörbare Brücke, Teleporter, Reaktor, Gefahrenzonen, Freilegungen)
dienen als Inspiration; Layouts, Wahrzeichen und Freischaltwege werden eigenständig gestaltet.
Freischaltcodes werden neu vergeben (siehe `Codes`).

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
