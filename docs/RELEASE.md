# Release runbook

How to cut a version of **Enigma tasks** (this repository, Enigma.MobileTasks) and build the `.apk`
you put on your phones: a Release build **signed with your own key**, checked before it goes
anywhere. Every command runs from the repository root, in bash.

Replace `X.Y.Z` with the version being released (e.g. `1.1.0`) throughout.

## What you need

The toolchain the README's *Building and running it* lists — the .NET 10 SDK, the `android`
workload, an Android SDK with platform 36 and the build tools, a JDK 17 or 21:

```bash
dotnet workload install android
export ANDROID_HOME=~/Android/Sdk JAVA_HOME=/usr/lib/jvm/java-21-openjdk
```

`keytool` comes with the JDK. `apksigner` and `aapt2` come with the Android build tools; the newest
installed version is found with:

```bash
BT=$(ls -d "$ANDROID_HOME"/build-tools/*/ | sort -V | tail -1)   # e.g. ~/Android/Sdk/build-tools/36.0.0/
```

## 1. Create your release key — once, ever

```bash
mkdir -p ~/keystores
keytool -genkeypair -v \
  -keystore ~/keystores/enigma-tasks.keystore \
  -alias enigma-tasks \
  -keyalg RSA -keysize 4096 -validity 10000
```

`keytool` asks for a password, then for a name and an organisation — they go into the certificate
and identify you; nothing checks them. The keystore is PKCS12, which uses that one password for the
store and for the key alike. The 10 000-day validity (about 27 years) outlives the app.

Note the certificate's fingerprint — it is how you recognise a build signed with this key:

```bash
keytool -list -v -keystore ~/keystores/enigma-tasks.keystore -alias enigma-tasks | grep SHA256
```

> **Back the keystore and its password up** — a password manager, an offline copy. Android only
> accepts an update signed with the key the installed app was signed with: **lose the key and no
> build can ever update the app again.** The only way out is export every list, uninstall (which
> deletes the lists), install, import.

Keep it **outside the repository**. `.gitignore` ignores `*.keystore` and `*.jks` as a safety net,
not as an invitation.

## 2. Prepare the release

On the release branch:

- [ ] `<Version>X.Y.Z</Version>` in `Directory.Build.props` — plain `major.minor.patch`. Android's
      versionName is that version, and its versionCode is derived from it as
      `major × 10000 + minor × 100 + patch` (1.1.0 is 10100), so there is no second number to bump.
- [ ] `RELEASENOTES.md` has a top `X.Y.Z` section (newest first).
- [ ] The README's *What's new* callout names `X.Y`.
- [ ] Clean, warning-free build, and the whole suite green:

      ```bash
      dotnet build Enigma.MobileTasks.slnx -c Release
      dotnet test --solution Enigma.MobileTasks.slnx -c Release
      ```

Then merge the release branch into `main` and build from `main`:

```bash
git switch main
git merge --no-ff <release-branch>
```

## 3. Build the signed `.apk`

```bash
# Start from nothing. A signing that fails leaves an unsigned "-Signed.apk" behind, and the next
# build takes it as up to date; `dotnet clean` does not remove the publish folder either.
rm -rf src/Enigma.MobileTasks.Android/bin src/Enigma.MobileTasks.Android/obj

# The password, typed without echo, into this shell's environment only.
read -rsp "Keystore password: " ENIGMA_TASKS_KEY_PASS && echo && export ENIGMA_TASKS_KEY_PASS

dotnet publish src/Enigma.MobileTasks.Android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore="$HOME/keystores/enigma-tasks.keystore" \
  -p:AndroidSigningKeyAlias=enigma-tasks \
  -p:AndroidSigningStorePass=env:ENIGMA_TASKS_KEY_PASS \
  -p:AndroidSigningKeyPass=env:ENIGMA_TASKS_KEY_PASS

unset ENIGMA_TASKS_KEY_PASS
```

It takes a few minutes (the assemblies are compiled ahead of time for each phone architecture) and
writes the release:

```
src/Enigma.MobileTasks.Android/bin/Release/net10.0-android/publish/io.github.enigmalibs.mobiletasks-Signed.apk
```

About 45 MB. The `io.github.enigmalibs.mobiletasks.apk` beside it is the unsigned intermediate —
Android will not install it.

| Property | What it does |
|---|---|
| `AndroidKeyStore=true` | Sign with the keystore below instead of the SDK's debug key. |
| `AndroidSigningKeyStore` | The keystore file. A full path: `~` is not expanded inside `-p:`, so write `$HOME`. |
| `AndroidSigningKeyAlias` | The key's alias in it — `-alias` from step 1. |
| `AndroidSigningStorePass`, `AndroidSigningKeyPass` | `env:<VAR>`: the signer reads the password from that environment variable, so it never appears on the command line, in the shell's history or in the build log, even at `-v:diag`. `file:<path>` reads it from a file instead. |

Nothing about signing lives in the project file: a plain `dotnet build -c Release` still signs with
the SDK's debug key, which is fine for trying a build on your own phone and wrong for a release.

## 4. Check it before it goes anywhere

```bash
APK=src/Enigma.MobileTasks.Android/bin/Release/net10.0-android/publish/io.github.enigmalibs.mobiletasks-Signed.apk

"$BT"apksigner verify --verbose --print-certs "$APK"
"$BT"aapt2 dump badging "$APK" | grep -E "^package:|^application-label:"
```

`apksigner` runs the `java` on your `PATH`, not `JAVA_HOME`'s; on a JDK newer than 21 it first prints
four `WARNING: A restricted method in java.lang.System has been called …` lines — Java's own notice,
harmless. Then it must print `Verifies`, `Verified using v2 scheme (APK Signature Scheme v2): true`, and a
`Signer #1 certificate DN:` that is **yours**. `CN=Android Debug, O=Android, C=US` is the SDK's debug
key — the signing properties did not reach the build. `Signer #1 certificate SHA-256 digest` is the
fingerprint from step 1, in lower case without the colons.

`aapt2` must show the version you are releasing and the app's name — for 1.1.0:

```
package: name='io.github.enigmalibs.mobiletasks' versionCode='10100' versionName='1.1.0' …
application-label:'Enigma tasks'
```

## 5. Install it

On a phone connected over USB (with USB debugging on), or a running emulator:

```bash
adb install -r "$APK"
```

`-r` replaces the installed app and keeps its lists. Without a cable, copy the `.apk` to the phone
(Drive, Downloads) and open it from the file manager — Android asks to allow installs from that app
once.

### The first release-signed install over a debug-signed one

Every build before 1.1.0 — and any plain `dotnet build -c Release` — is signed with the SDK's debug
key. Android refuses to update it with a package signed by your key: `adb install -r` fails with
`INSTALL_FAILED_UPDATE_INCOMPATIBLE`, and a file manager just says the app was not installed.
Uninstalling deletes the lists, so, once:

1. On the phone, export every list: its ⋮ menu → *Export as JSON*, into Downloads or Drive.
2. Uninstall the old app — long-press its icon → *Uninstall*, or
   `adb uninstall io.github.enigmalibs.mobiletasks`.
3. Install the release-signed `.apk` (step 5).
4. Lists → the file icon → pick the exported files; several can be picked at once.

Every later release signed with the same key updates in place, lists included.

## 6. Tag the release

On `main`, at the merge commit — a bare `X.Y.Z`, the form this repository uses:

```bash
git tag X.Y.Z
git push origin main X.Y.Z    # once the repository has a remote
```

Keep the signed `.apk` with the release (a GitHub release asset, a folder of your own) if you want
to be able to reinstall exactly that version later.

## When it goes wrong

`dotnet publish` reports a signing failure only as `error MSB6006: "java" exited with code 2`. Run
it again with `-v:n` added and look for `Failed to load signer` — the line after it says why.

| What you see | Why | Fix |
|---|---|---|
| `java.io.IOException: keystore password was incorrect` | The password is wrong. | Export the right one and build again. |
| `Failed to read Keystore password for signer #1: environment variable null not specified` | `ENIGMA_TASKS_KEY_PASS` is not set in this shell. | Run the `read … && export …` line of step 3 in the same shell as `dotnet publish`. |
| `…enigma-tasks.keystore entry "<alias>" does not contain a key` | The alias is wrong. | `keytool -list -keystore ~/keystores/enigma-tasks.keystore` lists the aliases. |
| `apksigner verify` prints `DOES NOT VERIFY` / `Missing META-INF/MANIFEST.MF` | The `-Signed.apk` is what a failed signing left behind, reused by a later build. | Delete `bin` and `obj` (step 3's first line) and build again. |
| `Signer #1 certificate DN: CN=Android Debug, O=Android, C=US` | Built without the signing properties. | Use step 3's `dotnet publish`, not a plain `dotnet build`. |
| `INSTALL_FAILED_UPDATE_INCOMPATIBLE` | The installed app is signed with another key. | Export, uninstall, install, import — see step 5. |
| `INSTALL_FAILED_VERSION_DOWNGRADE` | The installed app has a higher versionCode. | Release a higher `<Version>`, or uninstall first (after exporting). |
