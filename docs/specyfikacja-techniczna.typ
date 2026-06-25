#set document(title: "Specyfikacja techniczna — ContactManager", author: "Filip Pudlak")
#set page(
  paper: "a4",
  margin: (x: 2.2cm, y: 2.4cm),
  numbering: "1",
  number-align: center,
)
#set text(lang: "pl", size: 10.5pt)
#set par(justify: true, leading: 0.65em)
#set heading(numbering: "1.1")

// Odstęp i wygląd nagłówków
#show heading.where(level: 1): it => block(above: 1.4em, below: 0.9em)[#it]
#show heading.where(level: 2): it => block(above: 1.1em, below: 0.6em)[#it]

// Tabele: subtelne linie i nagłówek z szarym tłem
#set table(stroke: 0.5pt + luma(180))
#show table.cell.where(y: 0): set text(weight: "bold")

// Skrót na inline-kod w monospace
#let c(body) = raw(body)

// ------------------------------------------------------------------
// Strona tytułowa
// ------------------------------------------------------------------
#align(center)[
  #v(3cm)
  #text(22pt, weight: "bold")[Specyfikacja techniczna]
  #v(0.3cm)
  #text(17pt)[Aplikacja *ContactManager*]
  #v(0.8cm)
  #line(length: 45%, stroke: 0.6pt)
  #v(0.8cm)
  #text(12pt)[
    Wielowarstwowe API REST (ASP.NET Core 10) \
    z frontendem React 19 + TypeScript
  ]
  #v(2.5cm)
  #table(
    columns: 2,
    stroke: none,
    align: (right, left),
    [*Autor:*], [Filip Pudlak],
    [*Data:*], [25 czerwca 2026],
    [*Wersja:*], [1.0],
  )
]

#pagebreak()

#outline(title: "Spis treści", depth: 2, indent: auto)

#pagebreak()

// ------------------------------------------------------------------
= Wprowadzenie i architektura
// ------------------------------------------------------------------

*ContactManager* to wielowarstwowa aplikacja webowa służąca do zarządzania
kontaktami (książką adresową). Część serwerowa to API REST zbudowane w
*ASP.NET Core 10.0*, korzystające z bazy danych *PostgreSQL*, z uwierzytelnianiem
opartym o tokeny *JWT* oraz mechanizm odświeżania (refresh token) z rotacją i
wykrywaniem kradzieży tokenu. Część kliencka to aplikacja jednostronicowa (SPA)
napisana w *React 19* + *TypeScript* (bundler *Vite*).

Backend zaprojektowano zgodnie z zasadami *Clean Architecture* — kod podzielono na
cztery warstwy o zależnościach skierowanych wyłącznie „do wewnątrz”:

#table(
  columns: (auto, 1fr),
  [*Warstwa*], [*Odpowiedzialność*],
  [Domain], [Encje, reguły biznesowe, wyjątki dziedzinowe, interfejsy repozytoriów. Brak zależności zewnętrznych.],
  [Application], [Logika aplikacyjna: usługi, DTO, walidatory, interfejsy usług. Zależy tylko od Domain.],
  [Infrastructure], [Implementacje techniczne: dostęp do bazy (EF Core), hashowanie haseł, generowanie tokenów. Zależy od Domain i Application.],
  [Api], [Warstwa prezentacji HTTP: kontrolery, obsługa wyjątków, konfiguracja (DI, JWT, CORS, OpenAPI), start aplikacji.],
)

Kierunek zależności między projektami:

#align(center)[
  #c("Api  →  Application  →  Domain") \
  #c("Api  →  Infrastructure  →  (Application, Domain)")
]

Zastosowane wzorce projektowe: *Clean Architecture*, *Repository*, *Service Layer*,
*DTO*, *Dependency Injection*, walidacja (*FluentValidation*), uwierzytelnianie *JWT*
z rotacją refresh tokenów, *optimistic concurrency* (znacznik wersji wiersza – kolumna
systemowa `xmin` PostgreSQL) oraz globalna obsługa wyjątków mapowana na `ProblemDetails`.

// ------------------------------------------------------------------
= Opis klas i metod
// ------------------------------------------------------------------

== Warstwa Domain

Lokalizacja: `src/ContactManager.Domain/`.

=== Encje

*User* — `Entities/User.cs` \
Konto uwierzytelnionego użytkownika systemu (dane logowania i metadane).

#table(
  columns: (auto, auto, 1fr),
  [*Właściwość*], [*Typ*], [*Opis*],
  [`Id`], [`Guid`], [Identyfikator użytkownika.],
  [`Email`], [`string`], [Adres e-mail (login), wymagany.],
  [`FirstName` / `LastName`], [`string?`], [Imię i nazwisko (opcjonalne).],
  [`PasswordHash`], [`string`], [Hasło zahashowane algorytmem BCrypt.],
  [`CreatedAt`], [`DateTime`], [Data utworzenia konta (UTC).],
  [`RowVersion`], [`uint`], [Znacznik współbieżności (`xmin`).],
)

*Contact* — `Entities/Contact.cs` \
Rekord kontaktu z pełnym profilem, klasyfikacją kategoria/podkategoria oraz
osobnym hasłem (logowanie kontaktu).

#table(
  columns: (auto, auto, 1fr),
  [*Właściwość*], [*Typ*], [*Opis*],
  [`Id`], [`Guid`], [Identyfikator kontaktu.],
  [`FirstName` / `LastName`], [`string`], [Imię i nazwisko (wymagane).],
  [`Email`], [`string`], [Adres e-mail, unikalny.],
  [`PasswordHash`], [`string`], [Hasło kontaktu (BCrypt).],
  [`Phone`], [`string`], [Numer telefonu.],
  [`BirthDate`], [`DateOnly`], [Data urodzenia.],
  [`CategoryId` / `Category`], [`int` / `Category`], [Kategoria (wymagana) + nawigacja.],
  [`SubcategoryId` / `Subcategory`], [`int?` / `Subcategory?`], [Podkategoria słownikowa (opcjonalna).],
  [`CustomSubcategory`], [`string?`], [Podkategoria tekstowa (gdy kategoria na to pozwala).],
  [`CreatedAt` / `UpdatedAt`], [`DateTime` / `DateTime?`], [Daty utworzenia i modyfikacji (UTC).],
  [`RowVersion`], [`uint`], [Znacznik współbieżności (`xmin`).],
)

*Category* — `Entities/Category.cs` \
Encja słownikowa kategorii kontaktu (np. _Służbowy_, _Prywatny_, _Inny_). Pole
`AllowsCustomSubcategory` decyduje, czy dla kategorii dozwolona jest podkategoria tekstowa.
Właściwości: `Id`, `Name` (unikalna), `AllowsCustomSubcategory`,
kolekcje `Subcategories` i `Contacts`.

*Subcategory* — `Entities/Subcategory.cs` \
Encja słownikowa podkategorii w obrębie kategorii (np. _Szef_, _Klient_, _Pracownik_,
_Kontrahent_ dla kategorii _Służbowy_). Właściwości: `Id`, `Name`, `CategoryId`,
nawigacja `Category`, kolekcja `Contacts`. Unikalność pary `(CategoryId, Name)`.

*RefreshToken* — `Entities/RefreshToken.cs` \
Sesja tokenu odświeżającego. Przechowywany jest wyłącznie hash tokenu; wartość jawna
trafia jednorazowo do klienta (cookie HttpOnly). Umożliwia rotację tokenów i wykrycie kradzieży.

#table(
  columns: (auto, auto, 1fr),
  [*Właściwość*], [*Typ*], [*Opis*],
  [`Id`], [`Guid`], [Identyfikator tokenu.],
  [`UserId` / `User`], [`Guid` / `User?`], [Powiązany użytkownik.],
  [`TokenHash`], [`string`], [Hash SHA-256 wartości tokenu (nigdy jawny).],
  [`ExpiresAtUtc` / `CreatedAtUtc`], [`DateTime`], [Czas wygaśnięcia i utworzenia (UTC).],
  [`RevokedAtUtc`], [`DateTime?`], [Czas unieważnienia (`null` = aktywny).],
  [`ReplacedByTokenHash`], [`string?`], [Hash tokenu-następcy (śledzenie łańcucha rotacji).],
  [`IsActive`], [`bool`], [Właściwość wyliczana: token nieunieważniony i niewygasły.],
)

=== Wyjątki dziedzinowe

Lokalizacja: `Exceptions/`. Każdy mapowany jest przez `GlobalExceptionHandler` na
odpowiedni kod HTTP.

#table(
  columns: (auto, auto, 1fr),
  [*Klasa*], [*HTTP*], [*Znaczenie*],
  [`BusinessRuleViolationException`], [400], [Naruszenie reguły biznesowej (np. błędna para kategoria/podkategoria).],
  [`EmailConflictException`], [409], [Kontakt o podanym e-mailu już istnieje.],
  [`ConcurrencyConflictException`], [409], [Konflikt współbieżności — rekord zmieniony między odczytem a zapisem.],
)

=== Interfejsy repozytoriów

Lokalizacja: `Repositories/`. Definiują kontrakty dostępu do danych
(implementacje w warstwie Infrastructure).

*IUserRepository*
- `Task<User?> GetByEmailAsync(string email, CancellationToken)` — zwraca użytkownika po e-mailu lub `null`.

*IContactRepository*
- `Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken)` — wszystkie kontakty (z dołączonymi słownikami).
- `Task<Contact?> GetByIdAsync(Guid id, CancellationToken)` — kontakt po identyfikatorze (śledzony) lub `null`.
- `Task<Contact?> GetByEmailAsync(string email, CancellationToken)` — kontakt po e-mailu (kontrola unikalności).
- `Task AddAsync(Contact, CancellationToken)` — dodanie i zapis kontaktu.
- `Task UpdateAsync(Contact, uint expectedRowVersion, CancellationToken)` — aktualizacja z kontrolą współbieżności; rzuca `ConcurrencyConflictException` przy niezgodności `RowVersion`.
- `Task DeleteAsync(Contact, CancellationToken)` — usunięcie i zapis.

*ICategoryRepository*
- `Task<IReadOnlyList<Category>> GetAllWithSubcategoriesAsync(CancellationToken)` — kategorie wraz z podkategoriami.
- `Task<Category?> GetByIdWithSubcategoriesAsync(int id, CancellationToken)` — kategoria z podkategoriami lub `null`.

*IRefreshTokenRepository*
- `Task AddAsync(RefreshToken, CancellationToken)` — dodanie tokenu do kontekstu.
- `Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken)` — token po hashu (z użytkownikiem) lub `null`.
- `Task RevokeAllActiveForUserAsync(Guid userId, DateTime revokedAtUtc, CancellationToken)` — unieważnienie wszystkich aktywnych tokenów użytkownika (reakcja na kradzież).
- `Task SaveChangesAsync(CancellationToken)` — utrwalenie zmian.

== Warstwa Application

Lokalizacja: `src/ContactManager.Application/`.

=== Interfejsy usług

*IAuthService* — uwierzytelnianie.
- `Task<AuthResult?> LoginAsync(LoginRequestDto, CancellationToken)` — logowanie; para tokenów lub `null` przy błędnych danych.
- `Task<AuthResult?> RefreshAsync(string? rawRefreshToken, CancellationToken)` — wymiana refresh tokenu na nową parę (rotacja); `null` gdy token nieznany/wygasły/unieważniony; ponowne użycie zrotowanego tokenu traktowane jako kradzież.
- `Task LogoutAsync(string? rawRefreshToken, CancellationToken)` — unieważnienie tokenu (operacja idempotentna).

*IContactService* — logika kontaktów.
- `Task<IReadOnlyList<ContactResponseDto>> GetAllAsync(CancellationToken)` — lista wszystkich kontaktów.
- `Task<ContactResponseDto?> GetByIdAsync(Guid id, CancellationToken)` — kontakt po id lub `null`.
- `Task<ContactResponseDto> CreateAsync(CreateContactRequestDto, CancellationToken)` — utworzenie kontaktu (hashowanie hasła); `EmailConflictException` przy zajętym e-mailu.
- `Task<ContactResponseDto?> UpdateAsync(Guid id, UpdateContactRequestDto, CancellationToken)` — aktualizacja; `null` gdy brak; `EmailConflictException` / `ConcurrencyConflictException`.
- `Task<bool> DeleteAsync(Guid id, CancellationToken)` — usunięcie; `false` gdy brak.
- `Task<bool> ChangePasswordAsync(Guid id, ChangeContactPasswordRequestDto, CancellationToken)` — zmiana hasła (ponowne hashowanie); `false` gdy brak; kontrola `RowVersion`.

*ICategoryService*
- `Task<IReadOnlyList<CategoryResponseDto>> GetAllAsync(CancellationToken)` — kategorie z podkategoriami.

*IPasswordHasher*
- `string Hash(string password)` — hash hasła.
- `bool Verify(string password, string passwordHash)` — weryfikacja hasła względem hashu.

*ITokenService*
- `AccessToken CreateAccessToken(User user)` — podpisany JWT (HMAC-SHA256).
- `RefreshTokenInfo GenerateRefreshToken()` — kryptograficznie losowy refresh token.
- `string HashRefreshToken(string rawToken)` — hash SHA-256 wartości tokenu.

=== Implementacje usług

*ContactService* — `Services/ContactService.cs` \
Orkiestruje repozytorium kontaktów, waliduje reguły kategoria/podkategoria, hashuje
hasła i mapuje encje na DTO. Konstruktor: `(IContactRepository, ICategoryRepository, IPasswordHasher)`.
Metody prywatne: `ValidateCategorySelectionAsync(...)` — sprawdza poprawność pary
kategoria/podkategoria względem reguł w bazie (rzuca `BusinessRuleViolationException`,
zwraca znormalizowaną podkategorię tekstową); `ToResponse(Contact)` — mapowanie na DTO
(bez hashu hasła).

*AuthService* — `Services/AuthService.cs` \
Weryfikuje dane logowania, wydaje pary tokenów, realizuje rotację refresh tokenów z
wykrywaniem kradzieży. Konstruktor: `(IUserRepository, IRefreshTokenRepository, IPasswordHasher, ITokenService)`.
Metody prywatne: `IssueTokensAsync(User, ...)` — tworzy access token, generuje refresh
token, zapisuje jego hash i zwraca parę; `CreateTokenEntity(...)` — fabryka encji `RefreshToken`.

*CategoryService* — `Services/CategoryService.cs` \
Odczytuje kategorie z repozytorium i mapuje na DTO (`ToResponse(Category)` z uporządkowanymi
podkategoriami). Konstruktor: `(ICategoryRepository)`.

=== Modele (obiekty wartości)

#table(
  columns: (auto, 1fr),
  [*Rekord*], [*Zawartość*],
  [`AccessToken`], [`Token`, `ExpiresAtUtc` — wygenerowany JWT i czas wygaśnięcia.],
  [`AuthResult`], [`AccessToken`, `AccessTokenExpiresAtUtc`, `Email`, `RefreshToken`, `RefreshTokenExpiresAtUtc` — kompletny wynik uwierzytelnienia.],
  [`RefreshTokenInfo`], [`RawToken`, `TokenHash`, `ExpiresAtUtc` — wygenerowany refresh token (jawny + hash).],
)

=== Obiekty transferu danych (DTO)

#table(
  columns: (auto, 1fr),
  [*DTO*], [*Przeznaczenie i pola*],
  [`LoginRequestDto`], [Wejście logowania: `Email`, `Password`.],
  [`LoginResponseDto`], [Wyjście logowania: `Token`, `ExpiresAtUtc`, `Email`.],
  [`CreateContactRequestDto`], [Tworzenie kontaktu: dane profilu + `Password`, `CategoryId`, `SubcategoryId?`, `CustomSubcategory?`.],
  [`UpdateContactRequestDto`], [Aktualizacja kontaktu (bez hasła) + `RowVersion`.],
  [`ChangeContactPasswordRequestDto`], [Zmiana hasła: `NewPassword`, `RowVersion`.],
  [`ContactResponseDto`], [Reprezentacja kontaktu (bez hashu): dane profilu, nazwy kategorii/podkategorii, daty, `RowVersion`.],
  [`CategoryResponseDto`], [Kategoria: `Id`, `Name`, `AllowsCustomSubcategory`, lista `Subcategories`.],
  [`SubcategoryResponseDto`], [Podkategoria: `Id`, `Name`.],
)

=== Walidatory (FluentValidation)

#table(
  columns: (auto, 1fr),
  [*Walidator*], [*Reguły*],
  [`CreateContactRequestValidator`], [Imię/nazwisko (1–100), e-mail (format, 1–256), hasło (polityka złożoności), telefon (1–32), data urodzenia (przeszłość), kategoria (wymagana), podkategoria tekstowa (0–100).],
  [`UpdateContactRequestValidator`], [Jak wyżej, bez hasła.],
  [`ChangeContactPasswordRequestValidator`], [Polityka złożoności nowego hasła.],
  [`LoginRequestValidator`], [E-mail (wymagany, poprawny format) i hasło (niepuste).],
  [`PasswordRules`], [Wspólna polityka hasła: min. 8 znaków, ≥1 wielka litera, ≥1 znak specjalny. Metoda rozszerzająca `ValidPassword<T>()`.],
)

Rejestrację usług i walidatorów wykonuje `DependencyInjection.AddApplication(...)`
(usługi `Scoped`, walidatory wykrywane automatycznie ze złożenia).

== Warstwa Infrastructure

Lokalizacja: `src/ContactManager.Infrastructure/`.

=== Uwierzytelnianie

*BcryptPasswordHasher* (`IPasswordHasher`) — `Auth/BcryptPasswordHasher.cs` \
Hashowanie i weryfikacja haseł przy użyciu biblioteki BCrypt.NET (`Hash`, `Verify`).

*JwtTokenService* (`ITokenService`) — `Auth/JwtTokenService.cs` \
Generuje podpisane tokeny JWT (HMAC-SHA256) oraz kryptograficznie losowe refresh tokeny.
Konstruktor: `(IOptions<JwtSettings>, IOptions<RefreshTokenSettings>)`.
- `CreateAccessToken(User)` — JWT z oświadczeniami (`Sub`, `Email`, `GivenName`, `FamilyName`, `Jti`), podpis HMAC-SHA256, czas życia z `JwtSettings`.
- `GenerateRefreshToken()` — 32 losowe bajty kodowane Base64 URL-safe, hash SHA-256, czas życia z `RefreshTokenSettings`.
- `HashRefreshToken(string)` — szesnastkowy hash SHA-256.

*JwtSettings* — `Auth/JwtSettings.cs` (sekcja `Jwt`): `Issuer`, `Audience`, `Key`
(min. 32 znaki dla HMAC-SHA256), `ExpiryMinutes` (domyślnie 60). \
*RefreshTokenSettings* — `Auth/RefreshTokenSettings.cs` (sekcja `RefreshToken`):
`ExpiryDays` (7), `CookieName`, `CookieSecure`, `CookieSameSite`, `CookiePath`.

=== Dostęp do danych (EF Core)

*AppDbContext* (`DbContext`) — `Persistence/AppDbContext.cs` \
Kontekst Entity Framework Core dla PostgreSQL. Zbiory: `Users`, `RefreshTokens`,
`Contacts`, `Categories`, `Subcategories`. Metoda `OnModelCreating` stosuje wszystkie
konfiguracje `IEntityTypeConfiguration` ze złożenia.

*DesignTimeDbContextFactory* — `Persistence/DesignTimeDbContextFactory.cs` \
Fabryka kontekstu dla narzędzi (migracje EF Core); czyta connection string ze środowiska
lub używa wartości domyślnej dewelopera.

*DataSeeder* — `Persistence/Seed/DataSeeder.cs` \
Inicjalizacja bazy: `SeedAdminUserAsync(...)` tworzy konto administratora (dane z sekcji
`Admin`), `SeedContactsAsync(...)` dodaje przykładowe kontakty wraz z kontami logowania.

*Konfiguracje encji* (`Persistence/Configurations/`, Fluent API):
`UserConfiguration`, `ContactConfiguration` (relacje do Category/Subcategory z regułą
`Restrict`, unikalność e-mail), `CategoryConfiguration` (seed: _Służbowy_, _Prywatny_,
_Inny_), `SubcategoryConfiguration` (seed podkategorii _Służbowy_; unikalność
`(CategoryId, Name)`), `RefreshTokenConfiguration` (unikalny indeks `TokenHash`, kaskada do
`User`). Wszystkie konfigurują też znacznik współbieżności (`xmin`).

=== Repozytoria

Implementacje interfejsów z warstwy Domain (konstruktor każdej przyjmuje `AppDbContext`):

#table(
  columns: (auto, 1fr),
  [*Klasa*], [*Charakterystyka*],
  [`UserRepository`], [Wyszukiwanie użytkownika po e-mailu.],
  [`ContactRepository`], [CRUD kontaktów z dołączaniem (`Include`) Category/Subcategory; odczyty `AsNoTracking`; `UpdateAsync` ustawia oryginalną wartość `RowVersion` i mapuje `DbUpdateConcurrencyException` na `ConcurrencyConflictException`.],
  [`CategoryRepository`], [Odczyt kategorii z podkategoriami (`AsNoTracking`, sortowanie po `Id`).],
  [`RefreshTokenRepository`], [Cykl życia tokenów: dodanie, wyszukiwanie po hashu, masowe unieważnianie, zapis.],
)

Rejestrację warstwy wykonuje `DependencyInjection.AddInfrastructure(services, configuration)`:
`AppDbContext` (PostgreSQL/Npgsql), `BcryptPasswordHasher` i `JwtTokenService` (Singleton),
repozytoria (Scoped).

== Warstwa Api

Lokalizacja: `src/ContactManager.Api/`.

=== Kontrolery

*AuthController* — trasa `api/auth`. Access token w treści odpowiedzi; refresh token w
cookie HttpOnly (flagi Secure, SameSite, Path).
#table(
  columns: (auto, auto, 1fr),
  [*Metoda*], [*HTTP*], [*Działanie*],
  [`Login`], [`POST`], [Walidacja danych → `LoginAsync` → 200 + `LoginResponseDto` + cookie, lub 401.],
  [`Refresh`], [`POST`], [Odczyt cookie → `RefreshAsync` → 200 + nowa para + odświeżone cookie, lub 401.],
  [`Logout`], [`POST`], [Unieważnienie tokenu, usunięcie cookie → 204 (idempotentne).],
)
Metody pomocnicze: `IssueTokens`, `SetRefreshTokenCookie`, `DeleteRefreshTokenCookie`,
`BuildCookieOptions`.

*ContactsController* — trasa `api/contacts`. Odczyty publiczne, modyfikacje autoryzowane (JWT).
#table(
  columns: (auto, auto, auto, 1fr),
  [*Metoda*], [*HTTP*], [*Dostęp*], [*Działanie*],
  [`GetAll`], [`GET`], [Anon.], [200 + lista `ContactResponseDto`.],
  [`GetById`], [`GET {id}`], [Anon.], [200 + `ContactResponseDto` lub 404.],
  [`Create`], [`POST`], [Auth], [Walidacja → `CreateAsync` → 201 (nagłówek Location) lub 400/409.],
  [`Update`], [`PUT {id}`], [Auth], [Walidacja → `UpdateAsync` → 204 lub 404/409/400.],
  [`ChangePassword`], [`PUT {id}/password`], [Auth], [Walidacja → `ChangePasswordAsync` → 204 lub 404/409/400.],
  [`Delete`], [`DELETE {id}`], [Auth], [`DeleteAsync` → 204 lub 404.],
)

*CategoriesController* — trasa `api/categories`. Publiczny odczyt słownika.
- `GetAll` (`GET`, anonimowy) → 200 + lista `CategoryResponseDto` (z `AllowsCustomSubcategory` i podkategoriami).

=== Obsługa wyjątków i OpenAPI

*GlobalExceptionHandler* (`IExceptionHandler`) — `Errors/GlobalExceptionHandler.cs` \
Middleware przechwytujące nieobsłużone wyjątki i mapujące je na odpowiedzi `ProblemDetails`.
Wyjątki dziedzinowe → konkretne kody (400/409); pozostałe → 500 bez ujawniania szczegółów
(z logowaniem). Metoda `TryHandleAsync(HttpContext, Exception, CancellationToken)`.

*BearerSecuritySchemeTransformer* (`IOpenApiDocumentTransformer`) — `OpenApi/...` \
Rejestruje schemat zabezpieczeń JWT Bearer w dokumencie OpenAPI (przycisk „Authorize”
w UI dokumentacji). Metoda `TransformAsync(...)`.

=== Start aplikacji

*Program.cs* — punkt wejścia hosta `WebApplication`. Rejestruje: kontrolery, OpenAPI
(z `BearerSecuritySchemeTransformer`), health checks, CORS (`FrontendCorsPolicy`),
`ProblemDetails` + `GlobalExceptionHandler`, usługi warstw (`AddApplication`,
`AddInfrastructure`), uwierzytelnianie JWT Bearer (walidacja issuer/audience/lifetime/key,
HMAC-SHA256) i autoryzację. Potok middleware: obsługa wyjątków → OpenAPI (dev) → CORS →
uwierzytelnianie → autoryzacja → kontrolery → health checks. Przy starcie wykonuje
(opcjonalnie) automatyczne migracje i seed danych. Klasa `Program` jest publiczna na
potrzeby testów integracyjnych (`WebApplicationFactory<Program>`).

// ------------------------------------------------------------------
= Wykorzystane biblioteki
// ------------------------------------------------------------------

== Backend (.NET 10)

#table(
  columns: (auto, auto, 1fr),
  [*Projekt / Pakiet*], [*Wersja*], [*Zastosowanie*],
  table.cell(colspan: 3)[*ContactManager.Api*],
  [Microsoft.AspNetCore.Authentication.JwtBearer], [10.0.9], [Walidacja tokenów JWT (bearer).],
  [Microsoft.AspNetCore.OpenApi], [10.0.9], [Generowanie schematu OpenAPI.],
  [Microsoft.EntityFrameworkCore.Design], [10.0.4], [Narzędzia i migracje EF Core.],
  [Scalar.AspNetCore], [2.16.5], [Interaktywne UI dokumentacji API.],
  table.cell(colspan: 3)[*ContactManager.Application*],
  [FluentValidation.DependencyInjectionExtensions], [12.1.1], [Walidacja danych wejściowych + integracja z DI.],
  table.cell(colspan: 3)[*ContactManager.Infrastructure*],
  [BCrypt.Net-Next], [4.2.0], [Hashowanie haseł.],
  [Npgsql.EntityFrameworkCore.PostgreSQL], [10.0.2], [Dostawca bazy PostgreSQL dla EF Core.],
  [System.IdentityModel.Tokens.Jwt], [8.19.1], [Tworzenie i obsługa tokenów JWT.],
  [Microsoft.EntityFrameworkCore.Design], [10.0.4], [Narzędzia i migracje EF Core.],
  table.cell(colspan: 3)[*ContactManager.Domain*],
  [—], [—], [Brak zależności zewnętrznych.],
)

== Testy

#table(
  columns: (auto, auto, 1fr),
  [*Pakiet*], [*Wersja*], [*Zastosowanie*],
  [xunit], [2.9.3], [Framework testowy.],
  [xunit.runner.visualstudio], [3.1.4], [Uruchamianie testów w IDE.],
  [Microsoft.NET.Test.Sdk], [17.14.1], [Infrastruktura SDK testów.],
  [NSubstitute], [5.3.0], [Tworzenie atrap (mock) — testy jednostkowe.],
  [Microsoft.AspNetCore.Mvc.Testing], [10.0.9], [`WebApplicationFactory` — testy integracyjne.],
  [Testcontainers.PostgreSql], [4.12.0], [Izolowana baza PostgreSQL w kontenerze.],
  [coverlet.collector], [6.0.4], [Pomiar pokrycia kodu.],
)

== Frontend (Node / Vite)

React 19 (`react`, `react-dom`), `react-router-dom` 7 (routing), `axios` (klient HTTP
do API). Narzędzia: *Vite* 8 (bundler/serwer dev), *TypeScript* 6, *ESLint* 10
(+ wtyczki React).

// ------------------------------------------------------------------
= Sposób kompilacji i uruchomienia
// ------------------------------------------------------------------

Projekt korzysta z dwóch task runnerów: *`vpr`* (= `vp run`, Vite+) uruchamia skrypty
z `package.json`, a *`mise`* — zadania infrastrukturalne z `mise.toml`. Zalecane
środowisko to *dev container* (`.devcontainer/Dockerfile`), którego obraz zawiera
.NET 10 SDK, Node 22, Vite+ oraz `mise`.

== Wymagania

- .NET SDK *10.0.301* (przypięte w `global.json`, `rollForward: latestFeature`).
- PostgreSQL (uruchamiany w kontenerze przez `mise`).
- Node.js 22 + Vite+ (frontend).
- Docker (baza danych / opcjonalnie pełny stack).

Ustawienia kompilatora (`Directory.Build.props`): `LangVersion = latest` (C# 14+),
`Nullable = enable`, `ImplicitUsings = enable`.

== Krok po kroku

+ *Sklonuj repozytorium* i otwórz je w kontenerze (VS Code „Dev Containers”). Po
  utworzeniu kontenera automatycznie wykonuje się `vpr install` (`postCreateCommand`).
+ *Instalacja zależności* — `vpr install` \
  (`dotnet restore && dotnet tool restore && (cd frontend && vp install)`).
+ *Baza danych* — `mise db:up` \
  (`docker compose -f docker/docker-compose.infra.yml up -d postgres`). \
  Pomocniczo: `mise db:down`, `mise db:reset`, `mise db:logs`. \
  Migracje EF Core: `mise ef:add`, `mise ef:update`.
+ *Uruchomienie frontendu i backendu* — `vpr dev` \
  (równolegle: backend `dotnet watch run` z hot-reload + frontend `vp dev`/Vite).
+ *Build produkcyjny* — `vpr build` \
  (`dotnet build ContactManager.slnx -c Release && (cd frontend && vp build)`;
  artefakty frontendu w `frontend/dist/`).

== Testy

Skrypty testowe wywołują `dotnet test`:

#table(
  columns: (auto, 1fr),
  [*Polecenie*], [*Zakres*],
  [`vpr test`], [Wszystkie testy (`dotnet test ContactManager.slnx`).],
  [`vpr test:unit`], [Testy jednostkowe (`ContactManager.UnitTests`, xUnit + NSubstitute).],
  [`vpr test:integration`], [Testy integracyjne (`ContactManager.IntegrationTests`, `WebApplicationFactory` + Testcontainers PostgreSQL).],
)

== Konfiguracja i alternatywa

Zmienne środowiskowe definiuje się w pliku `.env` (na bazie `.env.example`):
połączenie do PostgreSQL, ustawienia JWT i refresh tokenu.

Alternatywnie cały stos (API + frontend + baza) można uruchomić w kontenerach:
`docker compose -f docker/docker-compose.dev.yml up --build` (skrót: `vpr docker:up`).

// ------------------------------------------------------------------
= Frontend (skrót)
// ------------------------------------------------------------------

Aplikacja kliencka (`frontend/`) to SPA w *React 19* + *TypeScript*, budowana narzędziem
*Vite*. Komunikacja z API odbywa się przez *axios* (serwer dev proxuje ścieżkę `/api` na
backend `http://localhost:5298`), a nawigację obsługuje *react-router-dom*. Polecenia:
`vp dev` (serwer deweloperski), `vp build` (build produkcyjny do `frontend/dist/`),
`vp lint` (ESLint). Opis poszczególnych komponentów wykracza poza zakres niniejszej
specyfikacji.
