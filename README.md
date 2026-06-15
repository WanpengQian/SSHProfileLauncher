# SSH Profile Launcher

A small, fast Windows desktop app that gives [Bitvise SSH Client](https://www.bitvise.com/ssh-client) the profile manager it lacks: one list of all your servers, with create / edit / delete, and **double‑click to open a remote terminal console**.

> **Disclaimer:** This is an independent, third‑party tool. It is **not affiliated with, endorsed by, or sponsored by Bitvise Limited**. "Bitvise" is a trademark of its respective owner. This project does **not** include or redistribute any Bitvise software — it launches a copy of Bitvise SSH Client that **you** install separately, through that software's documented command‑line interface.

## Why

Bitvise SSH Client is excellent, but it manages connection profiles as loose `.tlp` files through the Windows open/save dialog — there's no central list, search, or one‑click launch. This app adds exactly that, and nothing else.

## Features

- 📋 **One list** of all profiles — name, group, host, port, user, auth — with instant search.
- ➕ **Create / edit / delete** the common SSH fields in a simple dialog.
- 🖥️ **Double‑click = connect** — opens an interactive SSH terminal console (`stermc.exe`) in its own window.
- 🔑 Asks for the **username** before connecting when a profile has none (SSH usernames rarely match your Windows account).
- ⚙️ **Advanced config stays in Bitvise** — for tunnels, port forwarding, host keys, etc., open the profile in Bitvise's own editor, save a `.tlp`, and link it back. The app then connects via `-profile=`.
- 🌐 **English / 中文**, switchable at runtime.
- 🎨 Modern Fluent (Windows 11) theme.

## How it works

The app never parses Bitvise's proprietary profile format and ships no Bitvise code. For ordinary connections it just invokes Bitvise's documented command‑line tools:

- **Connect** → `stermc.exe -host=… -port=… -user=… [-pk=…]` (opens a terminal console).
- **Open in Bitvise** → `BvSsh.exe …` (the full GUI client, for advanced setup).
- **Linked `.tlp`** → `… -profile=<file>`.

Host keys are read from Bitvise's normal registry store, so servers you've already trusted in the GUI connect without re‑prompting.

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/) (to build) or the .NET Desktop Runtime (to run a published build)
- [Bitvise SSH Client](https://www.bitvise.com/ssh-client-download) installed (the app prompts you to download it if it can't be found)

## Build & run

```powershell
dotnet build -c Release
dotnet run -c Release
```

Single‑file publish:

```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained
```

## Where data is stored

Your profile library is a plain JSON file:

```
%APPDATA%\SSHProfileLauncher\library.json
```

No passwords are stored — Bitvise prompts for them at connect time.

## License

MIT — see [LICENSE](LICENSE). Third‑party asset attributions are in [NOTICE](NOTICE).

The application icon is derived from Google **Material Symbols** (Apache‑2.0).
