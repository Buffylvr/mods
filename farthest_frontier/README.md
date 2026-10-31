# Farthest Frontier mods

MelonLoader mods for the Mono build of Farthest Frontier.

- `mods/`  - one folder per mod (each its own C# project)
- `docs/`  - notes on the game's code, findings from dnSpy, references
- `tools/` - shared build/deploy scripts

Game (Mono build): `C:\Program Files (x86)\Steam\steamapps\common\Farthest Frontier\Farthest Frontier (Mono)`
Built DLLs are copied into that folder's `Mods\` directory.

## Building

    ~/.dotnet/dotnet build mods/<mod> -c Release

The build copies the DLL into the game's `Mods\` folder (for testing) and into
`mods/<mod>/workshop/` (for uploading).

## Publishing to Steam Workshop

Run "Farthest Frontier Mod Tools" from Steam (the uploader), choose type
"Melon Loader Mod", then point:
- Mod File Path -> `mods/<mod>/workshop/<Assembly>.dll`
- Mod Preview Image Path -> `mods/<mod>/workshop/preview.png`

The uploader takes exactly one DLL, so a mod must be self-contained: no extra
DLLs or loose files. Use MelonPreferences for settings and embedded resources
for any assets.
