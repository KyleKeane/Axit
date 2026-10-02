# Axit.Core, the shared pure code

Code every app may use and no window needs: `Dispatch` (which app a command line starts, `docs/axit/SPEC.md` AX-1), `BundleInfo` (the version). Planned (plan step 4): `AppPaths`, `SettingsFile`, `Updates/`, `Audio/WaveTone`. Unit tested in `tests/Axit.Tests`, which also holds the bundle's own tests (`LayoutTests` for the project references, `PrivacyTests` for the whole repository, the key collision test of AX-8.4).

Rules: nothing here references an app (SPEC M2); a piece arrives here only when a second app needs it, unchanged where possible (M3); plain static classes and records, no frameworks (B-10).
