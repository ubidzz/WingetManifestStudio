namespace ManifestUpdater;

internal static class StudioGermanFullTranslations
{
	private static readonly string[] Translations =
	[
		"Winget Manifest Studio", // 001
		"Aktivieren und Installation testen", // 002
		"Lokale Tests aktiviert", // 003
		"Optional: In Sandbox testen", // 004
		"INSTALLATIONSTESTS ERFORDERN IHRE BESTÄTIGUNG", // 005
		"English", // 006
		"Español", // 007
		"Kurzname", // 008
		"Urheberrecht", // 009
		"Urheberrechts-URL", // 010
		"Kauf-URL", // 011
		"Kanal", // 012
		"Gemeinsamer interner Typ", // 013
		"Gemeinsamer ZIP-Inhalt", // 014
		"Protokolle", // 015
		"Dateierweiterungen", // 016
		"Nicht unterstützte Architekturen", // 017
		"Zusätzliche Erfolgscodes", // 018
		"Paketfamilienname", // 019
		"Reparaturverhalten", // 020
		"Installer beendet das Terminal", // 021
		"Installationsort erforderlich", // 022
		"Explizites Upgrade erforderlich", // 023
		"Installationswarnungen anzeigen", // 024
		"Downloadbefehl verbieten", // 025
		"Archivdateien hängen von PATH ab", // 026
		"Schalter für stille Installation", // 027
		"Still mit Fortschritt", // 028
		"Interaktiver Schalter", // 029
		"Schalter für Installationsort", // 030
		"Protokollschalter", // 031
		"Upgrade-Schalter", // 032
		"Benutzerdefinierter Schalter", // 033
		"Reparaturschalter", // 034
		"Vereinbarungen", // 035
		"Dokumentationslinks", // 036
		"Eingeschränkte Funktionen", // 037
		"Zulässige Märkte", // 038
		"Ausgeschlossene Märkte", // 039
		"Erwartete Rückgabecodes", // 040
		"Nicht unterstützte Winget-Argumente", // 041
		"Standardinstallationsort", // 042
		"Installierte Dateien", // 043
		"Authentifizierungstyp", // 044
		"Entra-Ressource", // 045
		"Entra-Bereich", // 046
		"Zusätzliche Sprachfelder", // 047
		"Zusätzliche Installerfelder", // 048
		"Aus ist sicherer. Nur aktivieren, wenn HTTPS nicht verfügbar ist.", // 049
		"Vollständiger WingetCreate-Zugriff für New, Update, New-Locale, Update-Locale, Submit, Show, Token, Settings, Cache, Info und DSC. Befehle werden ohne cmd.exe direkt ausgeführt. Befehle mit Fragen öffnen eine echte WingetCreate-Konsole, in der Sie antworten können.", // 050
		"MSIX-SIGNATUR SHA-256", // 051
		"Werte, die von allen Manifestdateien gemeinsam verwendet werden.", // 052
		"Wird Benutzern im Windows-Paketmanager angezeigt.", // 053
		"Verwenden Sie öffentliche HTTPS-Links, wenn verfügbar.", // 054
		"Optionale Felder des aktuellen Winget-Schemas. Lassen Sie ein Feld leer, wenn es nicht zutrifft.", // 055
		"Winget verwendet diese Befehlszeilenschalter für Installeraktionen. Bekannte Inno-, Nullsoft-, MSI- und MSIX-Typen benötigen meist keine eigenen Werte.", // 056
		"Einfache Einzeilenformate erzeugen das verschachtelte YAML. Verwenden Sie einen Eintrag pro Zeile und lassen Sie das gesamte Feld leer, wenn es nicht zutrifft.", // 057
		"Optionale Regeln für Pakete, die von einem anderen Winget-Paket oder Windows-Feature, MSIX-Funktionen oder Marktbeschränkungen abhängen.", // 058
		"Beschreiben Sie ungewöhnliche Installerergebnisse und installierte Dateien ohne YAML zu schreiben. Diese Werte sind optional; die offizielle Validierung prüft ihr Schema.", // 059
		"Nur private, durch Entra ID geschützte Quellen verwenden diese Felder. Pakete für das Community-Repository müssen alle drei leer lassen.", // 060
		"Verwenden Sie diese Felder nur für Schemafelder ohne geführtes Steuerelement. Vorhandene benutzerdefinierte Schlüssel bleiben auch bei leeren Feldern erhalten.", // 061
		"Erforderliches Format: Publisher.Application (Beispiel: Contoso.Sample)", // 062
		"Kein führendes v eingeben", // 063
		"Üblicherweise en-US", // 064
		"Einen leeren Ordner oder vorhandenen Manifestordner wählen", // 065
		"Beispiel: MIT, Proprietary, Freeware", // 066
		"Durch Kommas getrennt", // 067
		"Befehlsaliasnamen durch Kommas getrennt. Bleiben bei Updates erhalten", // 068
		"Wird dem Benutzer nach der Installation angezeigt", // 069
		"Schemaversion des erzeugten YAML; 1.12.0 wird für Microsoft-Winget-Community-Einreichungen empfohlen", // 070
		"Öffentlicher Produktname, den Benutzer in Winget sehen", // 071
		"Unternehmen oder Person, die die Anwendung veröffentlicht", // 072
		"Ursprünglicher Anwendungsautor, wenn er vom Herausgeber abweicht", // 073
		"Lizenzname, z. B. MIT, GPL-3.0, Proprietary oder Freeware", // 074
		"Ein klarer Satz, der die Funktion der Anwendung erklärt", // 075
		"Eine längere öffentliche Beschreibung der Anwendung und ihres Zwecks", // 076
		"Ein kurzer, befehlsfreundlicher Name zum Suchen des Pakets", // 077
		"Suchbegriffe durch Kommas getrennt; keine #-Zeichen hinzufügen", // 078
		"Vom Paket installierte Befehlsnamen, durch Kommas getrennt", // 079
		"Öffentliche HTTPS-Startseite des Herausgebers", // 080
		"Öffentliche HTTPS-Seite, auf der Benutzer Hilfe erhalten", // 081
		"Öffentliche HTTPS-Datenschutzseite", // 082
		"Öffentliche HTTPS-Startseite dieser Anwendung", // 083
		"Öffentliche HTTPS-Seite mit den Lizenzbedingungen", // 084
		"Urheberrechtshinweis, der mit dem Paket angezeigt wird", // 085
		"Öffentliche HTTPS-Seite mit Urheberrechtsinformationen", // 086
		"Öffentliche HTTPS-Kaufseite bei kostenpflichtiger Anwendung", // 087
		"Öffentliche HTTPS-Seite mit Hinweisen zu genau dieser Version", // 088
		"Änderungen in genau dieser Version", // 089
		"Hinweise, die Winget nach der Installation anzeigt", // 090
		"Beispiel: stable oder beta", // 091
		"Beispiel: en-US", // 092
		"Durch Kommas getrennt; normalerweise Windows.Desktop", // 093
		"Beispiel: 10.0.19041.0", // 094
		"Pfade im ZIP durch Semikolons getrennt; bei Bedarf | Befehl nach einer portablen Datei hinzufügen", // 095
		"URL-Protokolle durch Kommas getrennt", // 096
		"Durch Kommas getrennt, ohne Punkte", // 097
		"Ganze Zahlen durch Kommas getrennt", // 098
		"JJJJ-MM-TT", // 099
		"Optionaler gemeinsamer Typ; geprüfte Zeilen behalten ihren eigenen Typ. Bei gemischten Installern leer lassen", // 100
		"Tatsächlicher Installertyp in einem ZIP-Paket", // 101
		"Gemeinsame Pfade in einem ZIP; mit Semikolons trennen und | Befehl nur bei portablen Dateien hinzufügen", // 102
		"Optionaler gemeinsamer Bereich; user für ein Konto, machine für den ganzen Computer oder leer, wenn er je Installer variiert", // 103
		"Unterstützte Modi durch Kommas getrennt: interactive, silent, silentWithProgress", // 104
		"Optionale Upgradeanweisung; leer lassen, sofern der Installer kein bestimmtes Verhalten erfordert", // 105
		"Ob der Installer erhöhte Rechte benötigt; bei Unklarheit leer lassen", // 106
		"Von der App registrierte URL-Protokolle, durch Kommas getrennt", // 107
		"Von der App registrierte Dateierweiterungen, ohne Punkte und durch Kommas getrennt", // 108
		"Architekturen, die diesen Installer nicht verwenden können, durch Kommas getrennt", // 109
		"Zusätzliche erfolgreiche Installer-Endcodes, durch Kommas getrennt", // 110
		"Microsoft-Store- oder MSIX-Paketfamilienname", // 111
		"Öffentliches Veröffentlichungsdatum im Format JJJJ-MM-TT", // 112
		"Wie Winget die App repariert: modify, uninstaller oder installer", // 113
		"Nur true eingeben, wenn die Installation das Terminal des Benutzers schließt", // 114
		"Nur true eingeben, wenn ein benutzerdefinierter Installationsort zwingend ist", // 115
		"true eingeben, wenn Winget nicht automatisch aktualisieren darf", // 116
		"true eingeben, wenn Winget Installerwarnungen anzeigen soll", // 117
		"true eingeben, wenn winget download blockiert werden muss", // 118
		"Bei Archiven true eingeben, wenn extrahierte Befehle von PATH abhängen", // 119
		"Installerargument für eine vollständig stille Installation", // 120
		"Argument für eine stille Installation mit Fortschritt", // 121
		"Installerargument zum Erzwingen der interaktiven Oberfläche", // 122
		"Argumentvorlage für einen benutzerdefinierten Installationsordner", // 123
		"Argumentvorlage für einen Protokolldateipfad", // 124
		"Speziell bei Upgrades verwendetes Installerargument", // 125
		"Argument, das Winget jedem Installationsbefehl hinzufügen muss", // 126
		"Für die Reparatur verwendetes Installerargument", // 127
		"Eine Vereinbarung pro Zeile als Bezeichnung | HTTPS-URL | Vereinbarungstext", // 128
		"Ein Dokumentationslink pro Zeile als Bezeichnung | HTTPS-URL", // 129
		"Eine Winget-Abhängigkeit pro Zeile als Publisher.Application | Mindestversion", // 130
		"Von der Anwendung benötigte Windows-Featurenamen, durch Kommas getrennt", // 131
		"Vom Paket benötigte MSIX-Funktionen, durch Kommas getrennt", // 132
		"Eingeschränkte MSIX-Funktionen, durch Kommas getrennt", // 133
		"Marktcodes, in denen die Installation erlaubt ist, durch Kommas getrennt", // 134
		"Marktcodes, in denen die Installation gesperrt ist, durch Kommas getrennt", // 135
		"Ein Installerergebnis pro Zeile als Code | Winget-Antwort | optionale HTTPS-Hilfe-URL", // 136
		"log, location oder beide nur wählen, wenn der Installer diese Winget-Argumente nicht unterstützt", // 137
		"Üblicher Ordner der installierten Anwendung; Umgebungsvariablen wie %ProgramFiles% sind erlaubt", // 138
		"Eine installierte Datei pro Zeile als relativer Pfad | Dateityp | optionale SHA-256 | optionales Argument | optionaler Anzeigename", // 139
		"Authentifizierung für eine private Quelle; bei Community-Paketen leer lassen", // 140
		"Von einer privaten Quelle verwendete Microsoft-Entra-Ressource", // 141
		"Von einer privaten Quelle verwendeter Microsoft-Entra-Bereich", // 142
		"Nur erweitertes Sprach-YAML; die meisten Benutzer sollten dies leer lassen", // 143
		"Nur erweitertes Installer-YAML; die meisten Benutzer sollten dies leer lassen", // 144
		"Leer lassen, wenn der Wert nicht zutrifft oder unbekannt ist", // 145
		"Bereit", // 146
		"Wählen Sie die Sprache der Studio-Oberfläche. Paketdaten und erzeugtes YAML werden niemals übersetzt oder verändert.", // 147
		"Die Updatesuche erfordert Aufmerksamkeit: {0}", // 148
		"Das ausgewählte Update wird heruntergeladen und überprüft...", // 149
		"Das überprüfte Studio-Update wird von GitHub heruntergeladen...", // 150
		"Download... {0}%", // 151
		"{0} wird heruntergeladen und geprüft: {1}%", // 152
		"Das überprüfte Update ist bereit. Winget Manifest Studio wird geschlossen, damit das Update abgeschlossen werden kann.", // 153
		"Der Updatedownload wurde abgebrochen. Es wurden keine Anwendungsdateien geändert.", // 154
		"Erstellen Sie eine Winget-Einreichung, ohne YAML von Hand zu bearbeiten.", // 155
		"Erstellen Sie einen neuen Satz aus drei Manifesten oder aktualisieren Sie vorhandene sicher. Lokale Releasedateien liefern den echten SHA-256-Hash; öffentliche URLs bestimmen, wo Winget sie herunterlädt.", // 156
		"LOKAL ZUERST\n\nGitHub-Token bleibt in der Windows-Anmeldeinformationsverwaltung\nKein Manifest wird ohne Sicherung überschrieben\nKein Installer wird ohne Bestätigung heruntergeladen", // 157
		"Erstellen Sie ein leeres Paket, laden Sie YAML-Dateien von diesem Computer oder geben Sie eine vorhandene Winget-Paket-ID ein, um die aktuellen Manifeste in eine neue Arbeitskopie zu laden.", // 158
		"Vorhandene Manifeste laden", // 159
		"Vorhandenes Winget-Paket importieren", // 160
		"Neues Projekt erstellen", // 161
		"Geben Sie die Paketdaten selbst ein oder fügen Sie eine öffentliche GitHub-Release-URL ein. Der Import füllt nur leere Felder und fragt vor dem Download unterstützter Releasedateien zur Hashberechnung und Prüfung nach.", // 162
		"GitHub-Release importieren", // 163
		"Paketdetails öffnen", // 164
		"Wählen Sie die lokalen MSI-, EXE-, MSIX-, APPX-, ZIP-, portablen App- oder Schriftdateien, die Sie hochladen. Das Studio liest genau diese Dateien und berechnet ihre SHA-256-Hashes. Geben Sie danach die öffentliche Download-URL jeder Datei ein.", // 165
		"Installer und Hashes öffnen", // 166
		"Die Vorschau erstellt alle drei Manifeste im Arbeitsspeicher. Speichern schreibt sie erst nach der Prüfung und legt zeitgestempelte Sicherungen vorhandener Dateien an.", // 167
		"Vorschau und Einreichen öffnen", // 168
		"Offizielle Tools öffnen", // 169
		"Öffnet die integrierte Einsteigerhilfe zu Feldbedeutungen, Installer-IDs, Hashes, Validierung und Einreichung.", // 170
		"Winget Manifest Studio aktuell halten", // 171
		"Die Startseite sucht erst nach dem Öffnen des Fensters nach der neuesten stabilen GitHub-Version. Eine installierte Kopie verwendet StudioSetup.msi; eine portable Kopie ersetzt nur WingetManifestStudio.exe. Nichts wird heruntergeladen oder installiert, bis Sie die Updateschaltfläche wählen und bestätigen.", // 172
		"Jedes Feld unten ist bearbeitbar. Beim Laden eines Ordners werden nur dessen YAML-Dateien gelesen; Installer werden nie heruntergeladen und Manifeste nie verändert.", // 173
		"Optionale erweiterte Paketfelder", // 174
		"Die meisten Einsteiger benötigen keine überschriebenen Installerverhalten, benutzerdefinierten Schalter oder rohes erweitertes YAML. Öffnen Sie diesen Bereich nur, wenn die Installerdokumentation oder ein vorhandenes Manifest einen Wert verlangt.", // 175
		"1 Fügen Sie jede genaue Releasedatei hinzu. 2 Fügen Sie die direkte öffentliche HTTPS-URL ein. 3 Prüfen Sie die Datei, um Hash und Metadaten auszufüllen. 4 Prüfen Sie die URLs nach dem Hochladen. Architektur, Typ und Bereich bleiben neben der URL sichtbar und können in den Listen korrigiert werden.", // 176
		"1 Releasedateien hinzufügen", // 177
		"2 Öffentliche URL eingeben", // 178
		"3 Auswahl prüfen und ausfüllen", // 179
		"4 Öffentliche URLs prüfen", // 180
		"PRÜFEN UND SICHER SPEICHERN", // 181
		"Verwenden Sie die einzelne hervorgehobene Aktion unten. Die Prüfung ändert nichts, bis Sie Speichern wählen; vorhandene Manifeste werden vor dem Ersetzen gesichert.", // 182
		"PRÜFLISTE", // 183
		"Das Studio schaltet diese Schritte in der richtigen Reihenfolge frei.", // 184
		"1  Vorschau", // 185
		"Erstellt das vorgeschlagene YAML im Arbeitsspeicher", // 186
		"2  Sicher speichern", // 187
		"Erstellt Sicherungen vor dem Ersetzen von Dateien", // 188
		"3  Validieren", // 189
		"Führt den offiziellen Winget-Validator aus", // 190
		"4  Testen und einreichen", // 191
		"Fährt im geführten Testcenter fort", // 192
		"ANSICHTSOPTIONEN\r\nDie Klartextprüfung ist standardmäßig ausgewählt.", // 193
		"Technisches YAML anzeigen", // 194
		"Klartextprüfung anzeigen", // 195
		"Sicherungsordner öffnen", // 196
		"KLARTEXTPRÜFUNG", // 197
		"Paketinformationen korrigieren", // 198
		"Das Studio bringt Sie zur richtigen Seite zurück.", // 199
		"ERFORDERLICH · Vorschau bleibt bis zur Korrektur gesperrt", // 200
		"ERFORDERLICH · Tests bleiben bis zur Korrektur gesperrt", // 201
		"Die Paketversion ist erforderlich und darf nicht mit v beginnen", // 202
		"Der Paketname ist erforderlich", // 203
		"Der Herausgeber ist erforderlich", // 204
		"Die Kurzbeschreibung ist erforderlich", // 205
		"Die Lizenz ist erforderlich", // 206
		"Wählen Sie einen Manifest-Ausgabeordner", // 207
		"Fügen Sie mindestens einen Installer hinzu", // 208
		"Zu korrigierendes Feld öffnen", // 209
		"Validierungsproblem beheben", // 210
		"Das Klartextergebnis unten nennt das Problem und den Ort der Korrektur. Erstellen Sie danach erneut eine Vorschau und speichern Sie wieder.", // 211
		"STOPP · Einreichung bleibt bis zur erfolgreichen Validierung gesperrt", // 212
		"Zu korrigierende Felder öffnen", // 213
		"Vorgeschlagene Änderungen anzeigen", // 214
		"Erstellt die genauen Manifeständerungen im Arbeitsspeicher und erklärt sie unten. Es werden keine Dateien geschrieben.", // 215
		"SICHER · Die Vorschau ändert keine Dateien", // 216
		"Geprüfte Manifeste speichern", // 217
		"Schreibt das geprüfte YAML in den Ausgabeordner, nachdem wiederherstellbare Sicherungen vorhandener Dateien erstellt wurden.", // 218
		"GESCHÜTZT · Vorhandene Manifeste werden zuerst gesichert", // 219
		"Mit Winget validieren", // 220
		"Führt den Microsoft-Winget-Validator gegen eine saubere temporäre Kopie aus. Das Paket wird nicht installiert.", // 221
		"SICHER · Die Validierung ändert die gespeicherten Manifeste nicht", // 222
		"Zum Testcenter wechseln", // 223
		"Sichere Vorprüfung, Installationstest, Ergebnisprüfung und Einreichung in einem geführten Bildschirm.", // 224
		"WEITER · Tests und Einreichung werden ohne Rückkehr fortgesetzt", // 225
		"Bereit zum Einreichen im Testcenter", // 226
		"Alle erforderlichen Prüfungen und Installationstests waren erfolgreich. Die Einreichungsaktion ist im Testcenter bereit.", // 227
		"BEREIT · Microsoft WingetCreate übernimmt die Einreichung", // 228
		"WINGET HAT EIN PROBLEM GEFUNDEN   •   NICHTS WURDE EINGEREICHT", // 229
		"VORSCHAU BEREIT   •   NICHTS WURDE GESPEICHERT", // 230
		"SICHER GESPEICHERT   •   BEREIT FÜR OFFIZIELLE VALIDIERUNG", // 231
		"VALIDIERUNG BESTANDEN   •   BEREIT FÜR DAS TESTCENTER", // 232
		"ALLE PRÜFUNGEN UND INSTALLATIONSTESTS BESTANDEN", // 233
		"Testcenter öffnen und einreichen", // 234
		"TESTEN UND ABSCHLIESSEN", // 235
		"Folgen Sie der Fortschrittslinie und verwenden Sie dann die einzelne hervorgehobene Aktion unten. Das Studio schaltet jeden Test in der richtigen Reihenfolge frei und erlaubt die Einreichung, wenn alle vier bestanden sind.", // 236
		"ERFORDERLICHE PRÜFLISTE", // 237
		"Diese werden automatisch in der richtigen Reihenfolge abgeschlossen.", // 238
		"1  Sichere Vorprüfung", // 239
		"Manifest-, Hash-, Signatur- und Repositoryprüfungen", // 240
		"2  Lokale Tests", // 241
		"Einmalige Windows-Einstellung", // 242
		"3  Installationstest", // 243
		"Installiert genau diese Version mit Winget", // 244
		"4  Installiertes Ergebnis", // 245
		"Bestätigt die installierte Version", // 246
		"OPTIONALE DIAGNOSE\r\nNur zusätzliche Details — dies sind keine erforderlichen Schritte.", // 247
		"Winget-Konfiguration prüfen", // 248
		"Nur in Sandbox installieren", // 249
		"In Sandbox installieren und deinstallieren", // 250
		"ERGEBNISSE UND ANWEISUNGEN", // 251
		"Winget-Testkonfiguration reparieren", // 252
		"Der Windows-Paketmanager ist nicht bereit. Führen Sie die Konfigurationsprüfung aus, um genaue Reparaturanweisungen zu erhalten.", // 253
		"SICHER · Prüft nur Winget und ändert nichts", // 254
		"Prüft YAML, Dateihashes, Signaturen, offizielle Winget-Validierung und ob das Paket bereits existiert.", // 255
		"SICHER · Nichts wird installiert oder geändert", // 256
		"Lokale Manifesttests erlauben", // 257
		"Windows benötigt einmalig eine Administratorbestätigung, bevor Winget ein Manifest von diesem Computer installieren kann.", // 258
		"EINMALIGE EINRICHTUNG · Windows-Abfrage bestätigen", // 259
		"Diese Version testweise installieren", // 260
		"Führt winget install --manifest mit den exakt erzeugten Dateien aus. Prüfen Sie die Installerkonsole und schließen Sie sie danach.", // 261
		"BESTÄTIGUNG ERFORDERLICH · Dies installiert Software auf diesem PC", // 262
		"Installiertes Ergebnis bestätigen", // 263
		"Prüft die Winget-Paket-ID und bei Bedarf die MSI-Identität oder den Namen der installierten Anwendung.", // 264
		"SICHER · Die Prüfung installiert das Paket nicht erneut", // 265
		"Installation überprüfen", // 266
		"Alle Tests bestanden — bereit zum Einreichen", // 267
		"Startet die offizielle Microsoft-WingetCreate-Einreichung, ohne zur Prüfseite zurückzukehren.", // 268
		"BEREIT · WingetCreate übernimmt Anmeldung und Pull-Request-Erstellung", // 269
		"Sicherungsordner", // 270
		"Noch keine Sicherungen", // 271
		"Sandbox-Installations- und Deinstallationstest", // 272
		"Diese Anleitung erklärt jeden Bildschirm und die von Winget benötigten Informationen. Sie können sie jederzeit lesen; die Schaltflächen führen nur zum beschriebenen Bildschirm.", // 273
		"Manifestprojekt starten oder öffnen", // 274
		"Wählen Sie für eine erste Version Neues Projekt. Laden Sie für ein Update einen lokalen YAML-Ordner oder wählen Sie Vorhandenes Winget-Paket importieren und geben Sie die genaue Paket-ID ein. Der Repositoryimport lädt die neuesten Manifeste in einen separaten Arbeitsordner und überschreibt keinen vorhandenen Manifestordner.", // 275
		"Zu den Paketdetails", // 276
		"Paketidentität eingeben", // 277
		"Die Paketkennung ist der dauerhafte Winget-Name, normalerweise Publisher.Application. Geben Sie zuerst Herausgeber und Paketname ein und verwenden Sie bei Bedarf Paket-ID vorschlagen. Die Paketversion hat kein führendes v. Behalten Sie die Kennung bei Updates unverändert bei.", // 278
		"Paketidentität bearbeiten", // 279
		"Öffentliche Paketinformationen vervollständigen", // 280
		"Paketname, Herausgeber, Lizenz und Kurzbeschreibung sind erforderlich. Geben Sie sie selbst ein oder verwenden Sie GitHub-Release importieren auf der Startseite. Der Import füllt nur leere Felder und fragt vor dem temporären Download unterstützter Dateien. Optionale geführte Felder erstellen Abhängigkeiten, Vereinbarungen, Dokumentation, Rückgabecodes, Marktregeln und Installationserkennung ohne manuelle YAML-Bearbeitung.", // 281
		"Paketinformationen bearbeiten", // 282
		"INSTALLATIONSDATEIEN UND DOWNLOADLINKS", // 283
		"Winget lädt von einer öffentlichen URL herunter; das Studio verwendet jedoch die passende lokale Releasedatei, um den vertrauenswürdigen SHA-256-Wert zu berechnen.", // 284
		"Genaue Releasedatei hinzufügen", // 285
		"Verwenden Sie Releasedateien hinzufügen für jeden veröffentlichten Installer. Wählen Sie dieselbe MSI-, EXE-, MSIX-, APPX-, Bundle-, ZIP-, portable App- oder Schriftdatei, die hochgeladen wird. Verwenden Sie eine Zeile je Architektur, Bereich oder Installervariante. x64 wird nicht angenommen.", // 286
		"Öffentliche HTTPS-URL eingeben", // 287
		"Fügen Sie die direkte Download-URL jedes Installers ein, nicht eine Webseite mit einer Downloadschaltfläche. Die URL muss öffentlich bleiben und genau die lokale Datei dieser Zeile herunterladen. GitHub-Release-Asset-URLs sind geeignet.", // 288
		"Download-URLs eingeben", // 289
		"Veröffentlichten Installer prüfen", // 290
		"Prüfen und Details ausfüllen berechnet SHA-256, meldet signiert oder unsigniert und erkennt Hinweise auf MSI, MSIX, Inno, NSIS, WiX Burn, Squirrel, Velopack, InstallShield, Advanced Installer und selbstentpackende EXE. Unsignierte EXE/MSI werden unterstützt und als Warnung angezeigt; MSIX/APPX benötigen weiterhin ihre Paketsignatur. ZIP-Dateien zeigen interne Pfade. Öffentliche URLs prüfen beweist, dass die veröffentlichte Datei zum Hash passt.", // 291
		"Installationsdateien prüfen", // 292
		"BESONDERE PAKETTYPEN", // 293
		"Portable EXE können wie normale EXE-Installer aussehen; wählen Sie bei Bedarf portable in der Zeile. Schriftpakete verwenden Microsofts getrennte fonts-Manifestwurzel und haben strengere Regeln. PWA-Unterstützung variiert je Winget-Client und Repositoryrichtlinie; prüfen Sie immer die offizielle Validierung und den Installationstest.", // 294
		"PRÜFEN, SPEICHERN UND VERÖFFENTLICHEN", // 295
		"Die Vorschau ist Ihre Sicherheitsprüfung. Sie erstellt das vorgeschlagene YAML im Arbeitsspeicher, ohne in den ausgewählten Ordner zu schreiben.", // 296
		"Projektbereitschaft beachten, dann Vorschau erstellen", // 297
		"Die Bereitschaftsanzeige zählt fehlende Pflichtangaben und markiert Problemfelder. Wenn BEREIT angezeigt wird, wählen Sie Änderungen anzeigen und prüfen Sie Kennung, alte und neue Versionen, URLs, Architekturen, Installertypen, Hashes und Dateinamen.", // 298
		"Vorschau prüfen", // 299
		"Mit wiederherstellbaren Sicherungen speichern", // 300
		"Wählen Sie Manifeste speichern erst, wenn die Vorschau korrekt ist. Neue Dateien werden im Ausgabeordner erstellt. Vorhandene Dateien werden vor dem Ersetzen in einen zeitgestempelten .manifest-backups-Ordner kopiert.", // 301
		"Speichern oder validieren", // 302
		"Vor der Einreichung validieren", // 303
		"Lokal validieren führt den offiziellen Winget-Validator gegen eine saubere temporäre Kopie aus. Beheben Sie gemeldete Fehler im zugehörigen Feld und validieren Sie erneut. Die gespeicherten Manifeste werden nicht verändert.", // 304
		"Validierung öffnen", // 305
		"Testschritt 1 ausführen — Sichere Vorprüfung", // 306
		"Das Testcenter prüft zuerst Winget selbst, dann erneut Hashes und Signaturen angehängter Dateien, führt die offizielle Validierung aus und sucht in Winget sowie microsoft/winget-pkgs nach der genauen Paketkennung. Es installiert nichts.", // 307
		"Testschritte 2, 3 und 4 ausführen", // 308
		"Lokale Tests aktivieren fordert einmalig eine Windows-Administratorbestätigung an. Installation hier testen validiert erneut vor winget install --manifest. Installation überprüfen kontrolliert die Winget-ID und verwendet ersatzweise den genauen MSI-ProductCode oder Anwendungsnamen, wenn Winget die lokale Manifest-ID nicht behält.", // 309
		"Installationstests öffnen", // 310
		"Windows Sandbox verwenden, wenn verfügbar", // 311
		"Der Sandbox-Installationstest führt Microsofts offizielles SandboxTest.ps1 in einer Wegwerfumgebung aus. Sandbox installieren und deinstallieren prüft auch die Entfernung vor dem Schließen. Der erste Lauf kann mehrere Minuten dauern, während Microsoft-Abhängigkeiten vorbereitet werden. Ein Manifest mit elevationProhibited muss Installation hier testen verwenden, da Microsoft Sandbox Winget als Administrator ausführt.", // 312
		"Sandbox-Test öffnen", // 313
		"Direkt aus dem Testcenter einreichen", // 314
		"Nachdem alle vier erforderlichen Tests bestanden sind, wählen Sie unten im Testcenter An Winget senden. Microsofts offizieller WingetCreate-Ablauf öffnet sich für Anmeldung und Pull-Request-Erstellung. Das GitHub-Token bleibt in der Windows-Anmeldeinformationsverwaltung.", // 315
		"Verwenden Sie kein führendes v in der Version, keine Release-Webseiten-URL statt der direkten Asset-URL, keinen Hash einer anderen Datei und keine falsche Architektur. Prüfen Sie bei ZIP-Paketen INTERNER TYP und ZIP-INHALT. Hängen Sie die genaue veröffentlichte Datei nach jeder Änderung erneut an und prüfen Sie sie.", // 316
	];

	public static readonly IReadOnlyDictionary<string, string> Values =
		StudioFullTranslationCatalog.Create(Translations, "de-DE");
}
