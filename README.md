# AshuraWebTerminal

## Public beta 1

A terminal for the web, built in C#. This terminal will work on windows. I will release web versions that work in web compiler, and a local version with extra features that are only runnable in the windows version (Linux versions may also be shared).

## Run

Requires the .NET 10 SDK.

```bash
dotnet run --project AshuraWebTerminal.csproj
```

The project can also be opened and run through `AshuraWebTerminal.sln`.

## Browser version

## THIS VERSION WILL NOT WORK IN SCHOOL

This is only if you have lightspeed blocker on you computer. Lightspeed will block "domain sharing" websites, so this is blocked. The only way to run the web version on a chromebook in school is to copy the web code (css, js, and html) and import it into a html loader. 

## This will not save the files or anything, until saving and loading storage is added (I am not sure if it is, I do not remember adding it. (I am talking about using this with lightspeed extension)

## No blocker extension, Great! View below

The browser workspace is in `web/` and stores files persistently with IndexedDB. Serve that folder with any static web server, then open its `index.html`:

```bash
cd web
python3 -m http.server 8080
```

By the way the website is up right now so you may run it at `https://ashuraschoolaccount.github.io/AshuraWebTerminal/`. Enjoy bro



Use `Download backup` to save all browser files and folders as a JSON backup. Use `Load backup` to restore that backup on another browser or after clearing site data.

### Publish on GitHub Pages

The repository includes a GitHub Actions workflow at `.github/workflows/pages.yml` that publishes the `web/` folder.

1. Push the repository to GitHub.
2. Open **Settings > Pages** in the repository.
3. Set **Source** to **GitHub Actions**.
4. Push to `main`, or run **Deploy browser app** from the repository's **Actions** tab.

The site will be published at `https://ashuraschoolaccount.github.io/AshuraWebTerminal/` after the workflow finishes. Browser files are stored separately for each browser and website URL; use the backup buttons to move them.

The C# console and browser app are kept feature-compatible. When a feature is added to `Main.cs`, its matching browser command or control in `web/` should be added in the same change.

## Files

Use these terminal commands. Every argument starts with `-`; quote an argument when it contains spaces:

- `MAKEFILE -path.at -"content"` creates or replaces a file. Without arguments, it prompts for the path and contents.
- `EDITFILE -path.at -"content"` replaces a file's contents. Without content, it prompts for new contents.
- `CODE -path.at -language` opens a file for coding. In the Windows console, `Shift+Enter` saves; `Escape` cancels. In the web editor, `Shift+Enter` saves.
- `RUN -path` runs a file based on its extension. Windows supports `.at`, `.py`, `.cpp`, `.cc`, `.cxx`, `.cs`, and `.csx` when the matching runtime or compiler is installed. The browser runs `.at` files and can load Python through Pyodide; C++ and C# require the Windows version because browsers cannot launch native compilers or the .NET runtime.
- `LOADFILE -path.at` prints a saved file. Without an argument, it prompts for the path.
- `LISTFILES` lists saved folders and `.at` files.
- `LISTFOLDERS` lists only folders.
- `OPENFOLDER -name` lists the files and subfolders inside a folder.
- `ADDFOLDER -name` creates a folder.
- `PRESETFOLDERS` creates the `documents` and `code` folders.
- `FETCH -IP` fetches the public IP address.

Supported coding languages are `cpp`, `csharp`, `python`, and `custom`. C# also accepts `c#` and `cs` as aliases. `DOCS -cpp`, `DOCS -csharp`, `DOCS -python`, and `DOCS -custom` open documentation links. The web language selector opens the same links.

The custom language profile is documented in [docs/CUSTOM_LANGUAGE.md](docs/CUSTOM_LANGUAGE.md).

File paths can include folders, for example `MAKEFILE -code/main.at -"print hello"`.

Custom `.at` files use one command per line: `echo`/`print`, `set NAME value`, `load path`, `make path content`, `list`, `mkdir folder`, and `run path`. Lines beginning with `#` are comments. Variables can be referenced as `$NAME`.

The `.at` extension is added automatically when it is omitted. On Windows, files are stored in `%LOCALAPPDATA%\AshuraTerminal\files`. Other native platforms use their local application-data directory. WebAssembly uses browser-session storage; a web host with persistent browser storage can replace that store when durable reload-to-reload persistence is required.
