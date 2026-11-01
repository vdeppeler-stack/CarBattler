# Balancing Stage 1 – Auswertung

## Lauf 1 (01.11.2026, `Lauf1_20261101/`)

546 Duelle (91 Paarungen × 2 Startseiten × Seeds 11/22/33), Tectonic Wreck, Startplätze 0 (NW) und 1 (SO),
Zeitlimit 180 s, danach entscheidet der HP-Anteil. Laufzeit 102 min im Batchmodus (Zeitfaktor 10).

**Befunde**

1. **Startseite 1 gewinnt 72 %** (351 von 487 entschiedenen Duellen). Ursache laut Rundenprotokollen: die
   Waffenverteilung. Bots von Startplatz 1 sammelten deutlich mehr Seeker Darts (93 zu 23), Thermite Gel
   (199 zu 40) und Titan Warheads (229 zu 142). Der Core Overcharge auf dem Wachturm wurde nie aufgenommen,
   er war nicht die Ursache.
   → Pickups sind jetzt punktsymmetrisch zur Kartenmitte, Startplatz 1 liegt exakt gespiegelt zu Startplatz 0.
2. **Vortex F1 18 %** (14 Siege, 61 Niederlagen, davon 55 durch K.o.): teilt im Mittel 89 Schaden aus und
   steckt 105 ein. Sandviper (31 %) und Goliath Wheel (32 %) liegen ebenfalls unter 35 %.
3. **Leviathan APC 67 %** – erwartbar für den Zwischenboss mit 5 Sternen Panzerung; im Kader als Boss markiert.
4. 185 Duelle (34 %) endeten nach Zeit – die Arena ist für 1-gegen-1 gross.
5. Nocturne (Wraith Lantern) warf 19-mal eine Ausnahme, wenn ein Entzugstick das Ziel tötete; behoben,
   ebenso dieselbe Lücke bei Chain Harpoon.

Die Startseiten-Verzerrung überlagert alle Fahrzeugwerte. Deshalb werden die Fahrzeugwerte erst nach dem
Kontrolllauf mit symmetrischer Karte angepasst.

## Lauf 2 (Kontrolle, symmetrische Pickups, Seed 11, 182 Duelle, `Lauf2_symmetrisch_seed11/`)

- Startseite 0 gewinnt 79 von 160 entschiedenen Duellen (49 %) – die Verzerrung ist behoben.
- Keine Ausnahmen mehr im Protokoll; 53 Duelle (29 %) nach Zeit entschieden.
- Siegquoten: Leviathan 85 %, Nocturne 69 %, Phase GT 65 %, Rustfang/Bonechaser 58 %, Interceptor 50 %,
  Mammoth 38 %, Sandviper/Iron Jaw/Bastion/Chrome Igniter 31 %, Goliath Wheel/Jester Van 27 %, Vortex F1 15 %.

Vortex F1, Goliath Wheel und Sandviper liegen in beiden Läufen unten, Leviathan oben.
→ HP-Korrekturen im Datenbau (`DataBuilder.HpTuning`, Sterne/Menüwerte unverändert):
Vortex F1 +40, Goliath Wheel +30, Sandviper +25, Leviathan APC −40.

## Lauf 3 (Kontrolle nach HP-Korrektur, Seed 22, 182 Duelle)

Ergebnis wird nach dem Lauf eingetragen.
