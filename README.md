# Playlista Media 2.0.1

Nowoczesna aplikacja desktopowa dla Windows 10/11, która zapisuje playlisty i pojedyncze materiały z YouTube jako pliki audio lub wideo. Wklejasz link, wybierasz format, jakość i folder — resztę wykonują lokalnie `yt-dlp`, FFmpeg i Deno.

> Używaj aplikacji wyłącznie do materiałów, które wolno Ci pobierać: własnych treści, materiałów na odpowiedniej licencji albo takich, na których pobranie masz zgodę.

## Możliwości

- audio: MP3, M4A, Opus, FLAC, WAV lub oryginalny strumień;
- jakość audio: 320, 256, 192 lub 128 kb/s dla formatów stratnych;
- wideo: MP4 lub WebM;
- jakość wideo: najlepsza dostępna albo limit 2160p, 1440p, 1080p, 720p, 480p lub 360p;
- playlisty i pojedyncze filmy;
- osobny podfolder playlisty, numerowanie plików, metadane i okładki tam, gdzie format je obsługuje;
- osobna historia pobrań dla każdego profilu formatu i jakości;
- dwa paski postępu: cała playlista i aktualny element;
- obsługa własnych prywatnych materiałów przez ciasteczka z Edge, Chrome lub Firefox;
- aktualizacja `yt-dlp` z poziomu aplikacji;
- instalacja i odinstalowanie dla bieżącego użytkownika, bez uprawnień administratora;
- brak telemetrii — aplikacja nie wysyła własnych danych analitycznych.

## Instalacja

1. Pobierz `Playlista_MP3_Setup.exe` z sekcji [Releases](../../releases).
2. Uruchom instalator, wybierz folder i kliknij „ZAINSTALUJ”.
3. Przy pierwszej instalacji poczekaj na pobranie `yt-dlp`, FFmpeg i Deno. FFmpeg jest największym składnikiem i przy kolejnych aktualizacjach nie jest pobierany bez potrzeby.
4. Otwórz **Playlista Media** z jednego skrótu na pulpicie albo z menu Start.

Instalator zapisuje program domyślnie w `%LOCALAPPDATA%\Programs\Playlista Media`, a narzędzia w `%LOCALAPPDATA%\PlaylistaMP3\tools`. Aktualizacja z wersji 1.x zachowuje ustawienia i dotychczasowy folder instalacji.

Przy aktualizacji instalator usuwa znane stare skróty również z pulpitu synchronizowanego przez OneDrive, sprawdza cel nowego skrótu i uruchamia automatyczny test aplikacji przed zakończeniem instalacji. Dzienniki startu znajdują się w `%LOCALAPPDATA%\PlaylistaMP3\logs`.

Instalator nie jest obecnie podpisany certyfikatem code-signing, dlatego Windows SmartScreen może pokazać komunikat o nieznanym wydawcy. Kod instalatora i automatyczny proces budowania są dostępne w tym repozytorium.

## Użycie

1. Wklej link do playlisty albo filmu.
2. Wybierz folder docelowy.
3. Wybierz **Audio** lub **Wideo**, a następnie format i jakość.
4. Dla publicznych materiałów zostaw „Bez logowania”. Przeglądarkę wybieraj tylko dla własnych prywatnych lub ograniczonych materiałów. Jeśli odczyt ciasteczek się nie powiedzie, zamknij przeglądarkę i spróbuj ponownie.
5. Kliknij „POBIERZ AUDIO” albo „POBIERZ WIDEO”.

Domyślny wzór nazwy to `001 - Tytuł.ext`. Historia nie miesza profili: pobranie MP3 320 kb/s nie blokuje późniejszego pobrania MP4, FLAC albo MP3 w innej jakości.

## Co naprawdę oznacza 320 kb/s i FLAC

320 kb/s określa bitrate **pliku wynikowego**. YouTube udostępnia dźwięk już skompresowany, więc ponowne zapisanie go jako MP3 320 kb/s nie odtworzy utraconych szczegółów. Podobnie FLAC lub WAV tworzy bezstratny plik wyjściowy, ale nie zmienia stratnego źródła w nagranie studyjnej jakości. Jeśli zależy Ci na uniknięciu kolejnej konwersji, wybierz „Oryginalny” albo odpowiedni strumień Opus/M4A.

## Aktualizacja narzędzi

Gdy YouTube zmieni sposób działania, kliknij „Aktualizuj narzędzia”. Moduł konserwacji pobierze najnowszy `yt-dlp` i sprawdzi sumy SHA-256 pobranych składników. FFmpeg i Deno są pobierane tylko wtedy, gdy ich brakuje.

## Budowanie ze źródeł

Wymagany jest Windows 10/11 x64 z .NET Framework 4.8.

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Build-Release.ps1
```

Gotowe pliki pojawią się w katalogu `build`, a instalator również jako `Playlista_MP3_Setup.exe` w katalogu projektu. Workflow GitHub Actions uruchamia ten sam skrypt i publikuje instalator jako artefakt; tag `v*` tworzy wydanie GitHub.

## Zgłaszanie błędów

Przed zgłoszeniem:

1. zaktualizuj narzędzia z poziomu aplikacji;
2. sprawdź, czy problem występuje również dla publicznego filmu;
3. dołącz wersję Windows, wybrany format/jakość i fragment dziennika bez danych prywatnych.

Nie wklejaj ciasteczek, tokenów, pełnych prywatnych linków ani innych danych logowania.

## Składniki zewnętrzne

Aplikacja pobiera narzędzia ze źródeł producentów i nie przechowuje ich binariów w repozytorium:

- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [FFmpeg — kompilacje Windows od gyan.dev](https://www.gyan.dev/ffmpeg/builds/)
- [Deno](https://github.com/denoland/deno)

Szczegóły znajdują się w [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Licencja

Kod aplikacji jest udostępniany na licencji MIT — zobacz [LICENSE.txt](LICENSE.txt). Każdy pobierany składnik zewnętrzny ma własną licencję.
