# Enigma.MobileTasks v1.2.0 Release Notes

Faster list editing: tapping **+** in the quick-add box leaves the cursor ready for the next item,
and every item has a trash button, so you can remove items one tap at a time, for example while
shopping.

## New Features

- **Tap + and keep typing.** In edit mode, adding an item with a category's **+** button leaves the
  cursor in that category's *Add an item* box, and the keyboard stays up. It used to drop the focus,
  so the box had to be tapped again for every item. Enter works as before.
- **A trash button on every item.** Each item row ends with a trash button, in the everyday view as
  well as in edit mode. One tap removes the item; the message that announces it offers **Undo**,
  which puts it back where it was. The trash stays in the same place in both modes, and edit mode
  adds the item's ⋮ menu to its left.
- **A shorter item menu.** The item's ⋮ menu now offers *Rename*, *Move up* and *Move down*. Delete
  is the trash button next to it. A category's menu is unchanged.

## Dependencies

- No changes. The Avalonia set (`Avalonia`, `Avalonia.Android`, `Avalonia.Themes.Fluent`,
  `Avalonia.Fonts.Inter`, `Avalonia.Headless`, `Avalonia.Skia`) is still held at **12.1.1**, while
  12.1.4 is available. It moves as one set, together with `CommunityToolkit.Mvvm` and
  `Enigma.Icons.Avalonia`, as a change of its own.

## Version

- Released: **1.2.0** (versionCode 10200). It updates 1.1.0 in place, lists included, when signed
  with the same key.

# Enigma.MobileTasks v1.1.0 Release Notes

The app gets its name — **Enigma tasks** — its checkboxes line up with their text, and its releases
are now signed with your own key, following a documented, checked procedure (`docs/RELEASE.md`).

## New Features

- **The app is called Enigma tasks**, on the launcher and in Settings → About (it was "Lists"). It is
  the same app (`io.github.enigmalibs.mobiletasks`): installed over a build signed with the same key,
  it updates in place and keeps the lists.
- **Release builds signed with your own key.** `docs/RELEASE.md` takes you from creating the key to a
  verified, installed `.apk`: the signed `dotnet publish`, checking the signature and the version
  with `apksigner` and `aapt2`, installing, tagging, and what each signing error means.
- **The versionCode follows the version** — `major × 10000 + minor × 100 + patch`, so 1.1.0 is
  10100 — and a release bumps one number, `<Version>`.

## Fixes

- On the Checklist page, every checkbox sits level with its text; it was drawn 8 pixels high. The
  theme choices on the Settings page had the same offset and are fixed too.

## Breaking Changes & Migration

- **1.0.0 was signed with the SDK's debug key.** Android refuses to update it with a build signed by
  your release key (`INSTALL_FAILED_UPDATE_INCOMPATIBLE`), and uninstalling deletes the lists. Once:
  export every list (⋮ → *Export as JSON*), uninstall the old app, install 1.1.0, then import the
  files (Lists → the file icon). Every later release signed with the same key updates in place.

## Dependencies

- `Microsoft.Extensions.Hosting` **10.0.12 → 10.0.13**.
- `xunit.v3` **4.0.1 → 4.0.2** and `Microsoft.Testing.Extensions.CodeCoverage` **18.11.2 → 18.12.0**
  (tests only).
- The Avalonia set (`Avalonia`, `Avalonia.Android`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`,
  `Avalonia.Headless`, `Avalonia.Skia`) held back at **12.1.1** while 12.1.4 is available — it moves
  as one set, together with `CommunityToolkit.Mvvm` and `Enigma.Icons.Avalonia`, as its own change.

## Version

- Released: **1.1.0** (versionCode 10100).

# Enigma.MobileTasks v1.0.0 Release Notes

The first release of **Enigma.MobileTasks** — the Android counterpart of Enigma.Tasks, for the lists
that get reused: packing lists, shopping lists, anything with categories you tick off and reset for
the next time. It reads and writes the very same JSON files as Enigma.Tasks and the Listes web app,
byte for byte.

## Feature overview

- **Lists** — every list on the phone with its progress; create, rename, duplicate, export or delete
  a list.
- **Checklist** — tick a row anywhere on it; progress per list and per category, a green check on a
  finished category, *Untick everything* per list or per category, categories that fold and stay
  folded.
- **Edit mode** — rename, move and delete categories and items, quick-add items one after another,
  add categories; deletions can be undone.
- **Import and export** — lists as JSON files through the system file picker, in the Enigma.Tasks
  format (`schemaVersion` 1); a file the app cannot read is refused with the reason.
- **Theme** — the Enigma palette in Light and Dark, following the phone's dark mode or pinned.
- **Storage** — the lists live in the app's private storage, written atomically; no permission is
  asked for.

## Compatibility

- Android 7.0 (API 24) or later; built for Android 16 (API 36), `net10.0-android`.
- Built on **Avalonia 12.1.1**.

## Version

- Initial release: **1.0.0** (versionCode 1), signed with the SDK's debug key.
