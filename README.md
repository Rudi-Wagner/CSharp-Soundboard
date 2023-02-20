# C# Soundboard

## Screenshot:
![GUI Screenshot](https://raw.githubusercontent.com/Rudi-Wagner/CSharp-Soundboard/main/Docs/GUI.PNG)
![GUI Screenshot](https://raw.githubusercontent.com/Rudi-Wagner/CSharp-Soundboard/main/Docs/GUI2.PNG)

 
## How to Install
 1. Den aktuellsten Installer herunterladen & ausführen.
 2. Externe Dependency [VB-CABLE Virtual Audio Device](https://vb-audio.com/Cable/) herunterladen und installieren.

### Audiodevice Setup:
 1. Die Sound-Einstellungen in Windows öffnen.
 2. Sicherstellen, dass das richtige Standardeingabegerät ausgewählt ist. 
 3. Unter dem Standardeingabegerät , die "`Geräteeigenschaften`" öffnen.
 4. Auf der rechten Seite die "`Zusätzliche Geräteeigenschaften`" öffnen.
 5. Im neuen Fenster den Tab"`Abhören`" auswählen.
 6. Bei "`Dieses Gerät als Wiedergabequelle verwenden`" das Häkchen setzen.
 7. Darunter bei "`Wiedergabe von diesem Gerät`" "`CABLE Input (VB-Audio Virtual Cable)`" auswählen.
![Setup Screenshot](https://raw.githubusercontent.com/Rudi-Wagner/CSharp-Soundboard/main/Docs/Audiodevice_Setup.PNG)
Nachdem das Programm fertig installiert wurde und das Audiodevice Setup gemacht wurde, ist das Soundboard einsatzbereit. 


## How to Use
#### Sound-Buttons
Das Interface ist relativ einfach gehalten, im unteren Bereich werden alle Sound-Buttons angezeigt. 
 - Die Buttons werden automatisch für jede gefundene .mp3 Datei (im Sound-Verzeichnis des Programms) erstellt.
 - Sounds können vom User über den Button rechts oben in der GUI "`AddSounds`" hinzugefügt werden. 
 - Sounds mit einem aktivierten Hotkey werden mit einem roten Rahmen angezeigt, so wie mit der Hotkey-Taste vor dem Namen.

#### Sound-Control
Im oberen Bereich sind die Kontroll-Elemente für die Sounds.
 - Mit dem "`Refresh`"-Button werden die .mp3s neu geladen und die Sound-Buttons neu generiert.
 - Mit dem "`Stop`"-Button wird ein laufender Sound sofort unterbrochen.
 - Über das Eingabefeld "`Delay in ms:`" kann eine Verzögerung in Millisekunden eingestellt werden, jeder Sound wird also erst nach Ablauf dieser Zeit abgespielt.
 - Mit dem Volume-Slider wird die Lautstärke der Sounds festgelegt.
 - Im Feld links oben wird der gerade laufende Sound angezeigt.

#### Settings & Spezialfunktionen
Die Einstellungen und Spezialfunktionen können über das kleine Zahnrad rechts oben geöffnet werden.
 - Es kann über das erste Drop-Down-Menü ein anderer Input innerhalb der App ausgewählt werden. Aber die Hauptfunktion des Soundboards, also den Sound über das Mikrofon abspielen, funktioniert nur über VB-Cable Virtual Audio oder etwas Ähnliches.
 
 - Über das Eingabefeld "`Audio Auto-Download`" kann ein Sound direkt über YouTube heruntergeladen werden. Der Sound wird automatisch zu den anderen Sounds hinzugefügt, er kann aber nicht innerhalb des Soundboards gekürzt oder verändert werden.
 
 - Über den letzten Bereich können 10 Sounds mit einem Hotkey belegt werden. Diese Sounds können danach mit den NumPad Tasten von 0-9 abgespielt werden. Die NumPadTaste '/' kann für das Stoppen eines Sounds, wie auch der eigene Stop-Button, benutzt werden.
	 - Hotkey Aktivieren:
	 Um einen Hotkey für einen Sound zu aktivieren, muss der Sound aus der linken Liste auf eines der zehn rechten Felder gezogen werden (Drag & Drop). 
	 - Hotkey Deaktivieren:
	 Um einen Hotkey für einen Sound zu deaktivieren, muss man auf das rechte Feld Doppel-klicken.


## Beschreibung
Mit diesem Soundboard können vom User ausgewählte Audiostücke  über das Mikrofon abgespielt werden, gleichzeitig wird es dem User aber auch noch über das Standardausgabegerät abgespielt. 
Es dient vor allem dazu, Soundeffekte wie Klatschen, Applaus, Gelächter oder andere Geräusche in eine Live-Performance zu integrieren, ohne dass sie live erzeugt werden müssen. Diese Funktion kann auch bei der Aufnahme von Musik oder Sprache eingesetzt werden, um Soundeffekte oder Hintergrundgeräusche hinzuzufügen.
Weiterhin kann man leicht und dynamisch neue Sounds hinzufügen, egal ob direkt über das Sound-Verzeichnis oder über eine YouTube URL.


### Technologien
 - C#/.NET 7
 - XAML
 - WPF  .NET 7

### Dependencies
 - [VB-CABLE Virtual Audio Device](https://vb-audio.com/Cable/)
 - [NAudio](https://github.com/naudio/NAudio)
 - [NHotkey](https://github.com/thomaslevesque/NHotkey)
 - [VideoLibrary](https://github.com/omansak/libvideo)
 - [MediaToolkit](https://github.com/AydinAdn/MediaToolkit)
