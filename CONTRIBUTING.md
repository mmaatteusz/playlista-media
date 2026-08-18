# Współtworzenie projektu

Dzięki za zainteresowanie projektem Playlista Media.

## Zanim zaczniesz

- Dla błędu najpierw otwórz zgłoszenie z krokami odtworzenia.
- Dla większej funkcji opisz proponowane zachowanie i wpływ na interfejs przed przygotowaniem zmian.
- Nie dodawaj mechanizmów obchodzenia DRM, płatnych zabezpieczeń, ograniczeń dostępu ani zabezpieczeń serwisu.
- Nie dołączaj binariów `yt-dlp`, FFmpeg ani Deno do repozytorium.

## Budowanie

Na Windows 10/11 x64 uruchom:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Build-Release.ps1
```

Skrypt używa kompilatora .NET Framework 4.8 dostępnego w systemie i tworzy aplikację oraz samodzielny instalator.

## Pull request

- Zachowaj zgodność z .NET Framework 4.8 i Windows 10/11 x64.
- Nie umieszczaj sekretów, ciasteczek ani prywatnych adresów URL w kodzie, testach i dziennikach.
- Zaktualizuj README i CHANGELOG, jeśli zmiana jest widoczna dla użytkownika.
- Sprawdź kompilację przez `Build-Release.ps1`.
- Opisz test ręczny: typ źródła, format, jakość i wynik.

Wysyłając zmianę, zgadzasz się na udostępnienie jej na licencji MIT projektu.
