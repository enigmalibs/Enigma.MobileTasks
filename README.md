# Enigma.MobileTasks

An Android application for the lists that get reused: packing lists, shopping lists, anything with
categories you tick off and then reset for the next time.

It is the phone counterpart of [Enigma.Tasks](../Enigma.Tasks), the desktop application, and it
reads and writes **the same files**, byte for byte. A list exported on the phone imports into
Enigma.Tasks unchanged; a list exported by Enigma.Tasks — or by another phone — imports here
unchanged. That is not a claim but a test: the suite round-trips the very reference file Enigma.Tasks
proves its own format against.

It paints with the Enigma palette of [Enigma.Avalonia](../Enigma.Avalonia) — the same Light and Dark
colours as the desktop applications — and follows the phone's dark mode. On the phone it is called
**Enigma tasks**.

> **What's new in 1.1** — the app is named *Enigma tasks*, the checkboxes line up with their text,
> and releases are signed with your own key ([docs/RELEASE.md](docs/RELEASE.md)). See
> [RELEASENOTES.md](RELEASENOTES.md).

## Using it

- **Lists** (the first page) holds every list on the phone, each with its progress. Tap a list to
  open it. Its ⋮ menu renames, duplicates, exports or deletes it. **+** creates a list; the file icon
  imports lists; the gear opens the settings.
- **Tick things off.** Tap a row — anywhere on it. The page counts the whole list, each card counts
  its own, and a finished category gets a green check.
- **Reset a list** for the next trip with *Untick everything*, in the page's ⋮ menu or in a
  category's.
- **Fold a category** by tapping its heading. Which cards are folded is remembered per list.
- **Edit mode** (the pencil) reveals everything structural: a ⋮ menu on every category and every item
  (rename, move up, move down, delete), a quick-add box under each category — type, press Enter, type
  the next one — and *Add a category*. A new, empty list opens in edit mode. The everyday view stays a
  clean checklist.
- **Undo** a deletion from the message that announces it. Deleting a category that still holds items
  asks first.
- **Settings** — the theme (System, Light or Dark), how many lists the phone holds, the version.

## Moving lists between the phone, the desktop and another phone

Lists live in the application's private storage — no permission is asked for, and nothing else on
the phone can read them. The system's own file picker is how a list leaves the phone and how one
arrives: Downloads, Google Drive, a USB stick, whatever the phone offers.

| From → to | How |
|---|---|
| Phone → Enigma.Tasks | On the phone: the list's ⋮ menu → *Export as JSON*, save it somewhere your computer can reach (Drive, Downloads over USB). On the desktop: *Import* (`Ctrl+O`) or drop the file on the window. |
| Enigma.Tasks → phone | On the desktop: *Export as JSON* (`Ctrl+S`). Put the file where the phone can see it, then on the phone: Lists → the file icon → pick it. |
| Phone → phone | Export on one, import on the other. Several files can be imported at once. |

An import always adds a copy — importing the same list twice gives two lists. A file that is not a
list this application can read is refused with the reason, and nothing is written.

> **Uninstalling the application deletes the lists it holds.** Export the ones you want to keep
> first.

## The file format

Every list is one JSON file, exactly as Enigma.Tasks and the Listes web app write it:

```json
{
  "schemaVersion": 1,
  "name": "Liste Judith",
  "categories": [
    {
      "id": "c-mtzxeu79-2-2lsj",
      "name": "Habits",
      "items": [
        { "id": "i-mtzxeu79-3-9zk2", "text": "Bodys", "done": true }
      ]
    }
  ]
}
```

Written as `JSON.stringify(list, null, 2)` writes it: that property order, two-space indentation,
`\n` line endings, no trailing newline, UTF-8 without a byte-order mark, accents written literally.
The file name is the list's name folded to a slug — *Liste Judith* is `liste-judith.json`.

Reading is forgiving in the same ways the desktop is: `schemaVersion` may be absent, and a version
**greater than 1 is refused** with an explanation; `id` is optional everywhere; `done` defaults to
`false`; text is trimmed. A category needs a name and an item needs text.

## Building and running it

You need the .NET 10 SDK (pinned in `global.json`), the Android workload, and an Android SDK with
platform 36 and a JDK 17 or 21:

```bash
dotnet workload install android
export ANDROID_HOME=~/Android/Sdk JAVA_HOME=/usr/lib/jvm/java-21-openjdk   # or pass -p:AndroidSdkDirectory=…
```

Install and start it on a connected phone (USB debugging on) or a running emulator:

```bash
dotnet build src/Enigma.MobileTasks.Android -t:Run
```

Or build an `.apk` you can install with `adb install` or copy to the phone:

```bash
dotnet build src/Enigma.MobileTasks.Android -c Release
adb install src/Enigma.MobileTasks.Android/bin/Release/net10.0-android/io.github.enigmalibs.mobiletasks-Signed.apk
```

That Release `.apk` (about 45 MB, trimmed) is signed with the SDK's debug key — fine to try a build
on your own phone. **A release** is signed with your own key, so every later version can update it in
place: [docs/RELEASE.md](docs/RELEASE.md) has the commands, from creating the key to checking and
installing the signed `.apk`. Android 7.0 (API 24) or later.

A plain Debug `dotnet build` does **not** give an installable `.apk`: Debug uses fast deployment,
which pushes the assemblies separately, so that `.apk` aborts on launch when installed by hand. Use
`-t:Run` / `-t:Install` for Debug, or the Release build.

## Working on it

```bash
dotnet build Enigma.MobileTasks.slnx                 # must stay at zero warnings
dotnet test --solution Enigma.MobileTasks.slnx       # 210 tests
```

The solution build skips the Android head (`<Build Project="false" />` in the `.slnx`), so it and
the tests run on any machine, with or without the Android workload; build the head explicitly after
touching it.

```
src/Enigma.MobileTasks.Core      the model: lists, operations, the JSON format, the workspace, the settings
src/Enigma.MobileTasks.App       the whole UI (net10.0): App, composition root, pages, ViewModels, services, theme
src/Enigma.MobileTasks.Android   the Android head (net10.0-android): activity, manifest, resources, icon
tests/                           xUnit v3 suites for both, including headless Avalonia render tests
docs/                            the release runbook, the roadmap, the plans, a record per completed piece of work
```

The model layer knows nothing about the UI — it is ported from Enigma.Tasks' own, rule for rule —
and the UI layer decides nothing about what a move or a tick means. That is what lets the
compatibility guarantee be a test rather than a hope.

The Enigma palette is vendored: `src/Enigma.MobileTasks.App/Themes/Colors.axaml` and `Brushes.axaml`
are Enigma.Avalonia.Desktop 1.2.0's own files, and `PaletteParityTests` compares every colour and
brush, in both variants, against that package. Upgrade them together with the test project's
`Enigma.Avalonia.Desktop` version, or replace both with Enigma.Avalonia.Mobile once it is published.

Built with [Avalonia](https://avaloniaui.net) 12 and `Enigma.Icons.Avalonia`. MIT licensed — see
`LICENSE.md`.
