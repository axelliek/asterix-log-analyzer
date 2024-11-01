# Asterix Log Analyzer

## Testaufgabe für DMRZ

Ausführen vom GitHub:

Führen Sie in einem cmd Fenster folgende Befehle aus

`cd <path>` z.B. cd c:\temp

Falls kein Verzeichnis c:\temp vorhanden ist führen Sie den Befehl:

```
mkdir c:\temp

cd c:\temp
```

Klonen Sie die Repository
```
git clone https://github.com/axelliek/asterix-log-analyzer.git
```
Folgende Ausgabe sollte zu sehen sein:

```
Cloning into 'asterix-log-analyzer'...
remote: Enumerating objects: 136, done.
remote: Counting objects: 100% (136/136), done.
remote: Compressing objects: 100% (85/85), done.
remote: Total 136 (delta 69), reused 100 (delta 39), pack-reused 0 (from 0)
Receiving objects: 100% (136/136), 45.02 KiB | 1.13 MiB/s, done.
Resolving deltas: 100% (69/69), done.
```

```cd .\asterix-log-analyzer\AsterixLogAnalyzer\```

Erstellen Sie das Program mit:

```
dotnet build
```

Es sollten folgende Ausgaben zu sehen sein:

```
PS C:\temp> cd .\asterix-log-analyzer\AsterixLogAnalyzer\
PS C:\temp\asterix-log-analyzer\AsterixLogAnalyzer> dotnet build
  Wiederherzustellende Projekte werden ermittelt...
  "C:\temp\asterix-log-analyzer\AsterixLogAnalyzer\AsterixLogAnalyzer.csproj" wiederhergestellt (in 537 ms.).
  AsterixLogAnalyzer -> C:\temp\asterix-log-analyzer\AsterixLogAnalyzer\bin\Debug\net8.0\AsterixLogAnalyzer.dll

Der Buildvorgang wurde erfolgreich ausgeführt.
    0 Warnung(en)
    0 Fehler

Verstrichene Zeit 00:00:06.77
```

Starten Sie das Program mit dem Befehl:

```
dotnet run
```

Dabei sollte Sie folgende Ausgabe sehen:

```
PS C:\temp\asterix-log-analyzer\AsterixLogAnalyzer> dotnet run
PROMPT:
AsterixLogAnalyzer.exe [<INPUT_FILE>] [OPTIONS]

<INPUT_FILE>:           File with input data (Aterix log queue_log)
Options:
-H, --HELP, -?:         Show this help
-O, --OUTPUT-DIRECTORY  Write bitmap to output directory <OUTPUT_DIRECTORY>
PS C:\temp\asterix-log-analyzer\AsterixLogAnalyzer>
```

Starten Sie das Program mit Parametern

```
dotnet run .\Data\Testdaten.txt
```

Dabei werden fogende Aúsgaben zu sehen sein:

```
PS C:\temp\asterix-log-analyzer\AsterixLogAnalyzer> dotnet run .\Data\Testdaten.txt
Completed calls 7
Abandoned calls 12
Calls connected but not ended 1
Ringing calls but not ended 1
Bitmap is written to:
C:\temp\asterix-log-analyzer\AsterixLogAnalyzer\Testdaten-<CURRENT-DATE>.bmp
``` 

Es wird automatisch ein Bild geöffnet:

![Testdaten ausgabe Bitmap](/assets/Testdaten-241101-194247.bmp)
