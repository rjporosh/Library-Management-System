# Localization

English is the default; Bangla (`bn`) is included. Fallback is always English.

## How culture is chosen

1. `?culture=bn` (or `?lang=bn`) query parameter, then
2. the `Accept-Language` request header, then
3. English.

Configured in `Program.cs` (`AddLocalization` + `RequestLocalization`).

## Resources

`src/Library.Api/Resources/SharedResources.resx` (English) and
`SharedResources.bn.resx` (Bangla) hold:

- **system messages** — `Error.Unexpected`, `Error.NotFound`, `Error.Validation`,
  `Error.Conflict`, `Error.RateLimited`, `Error.FeatureDisabled`,
  `Error.DatabaseUnavailable`;
- **one entry per error code** in `ErrorCodes` (`BOOK_ISBN_REQUIRED`, …).

## How the error contract is localized

`ApiError.errorCode` is stable and machine-readable. The server response keeps
the English `errorMessage` (developer-facing); the **front-end** maps the code
to a localized string:

- `GET /api/metadata/messages` (respects the current culture) returns
  `{ culture, messages: { "<errorCode>": "<localized text>" } }`.
- The SPA loads this once at startup and `normaliseError()` replaces each error's
  message with the localized one.

This keeps every validation / business / system message localizable **without**
threading `IStringLocalizer` through the Application layer's static validators.

## Adding a language (e.g. French)

1. Copy `SharedResources.resx` to `SharedResources.fr.resx`, translate the
   `<value>` elements.
2. Add `new CultureInfo("fr")` to the `supported` list in `Program.cs` and
   `{ code = "fr", name = "Français" }` to `MetadataController.GetLanguages()`.
3. Add `'fr'` to the frontend `Lang` type and the switcher in `App.tsx`.

Only step 1 is a resource change; steps 2–3 register the option in the UI.
