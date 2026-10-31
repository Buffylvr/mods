# Farthest Frontier mods

MelonLoader mods for the Mono build of Farthest Frontier. One folder per mod
under `mods/`, each its own C# project; shared build settings (game paths,
references, copy-to-game step) are in `Directory.Build.props`.

Game (Mono build): `C:\Program Files (x86)\Steam\steamapps\common\Farthest Frontier\Farthest Frontier (Mono)`

## Setup

- .NET SDK (installed user-level at `~/.dotnet`, not on PATH - call `~/.dotnet/dotnet`).
- Mods target .NET Framework 4.7.2 and reference the game's `Farthest Frontier_Data\Managed`
  DLLs plus MelonLoader's `net35\MelonLoader.dll` and `0Harmony.dll`; nothing is copied
  next to the mod.
- To read the game's code: `dotnet tool install -g ilspycmd`, then
  `ilspycmd -p -o <out dir> "<Mono folder>\Farthest Frontier_Data\Managed\Assembly-CSharp.dll"`.
  Keep the decompiled output outside this repo (it is the game's code).

## Building

    ~/.dotnet/dotnet build mods/<mod> -c Release

The build copies the DLL into the game's `Mods\` folder (for testing) and into
`mods/<mod>/workshop/` (for uploading). Launch "Play Farthest Frontier (Mono)" from
Steam; output and errors appear in the MelonLoader console and in
`<Mono folder>\MelonLoader\Latest.log`. Settings live in `<Mono folder>\UserData\MelonPreferences.cfg`.

## Publishing to Steam Workshop

Run "Farthest Frontier Mod Tools" from Steam (the uploader), choose type
"Melon Loader Mod", then point:
- Mod File Path -> `mods/<mod>/workshop/<Assembly>.dll`
- Mod Preview Image Path -> `mods/<mod>/workshop/preview.png`
- Description -> contents of `mods/<mod>/workshop/description.txt`

Start with Visibility = Private, subscribe and test, then switch to Public.

The uploader takes exactly one DLL, so a mod must be self-contained: no extra
DLLs or loose files. Use MelonPreferences for settings and embedded resources
for any assets.

## Updating a published mod

1. **Unsubscribe** from the mod on the Workshop and check its DLL is gone from the
   game's `Mods\` folder. Steam installs subscribed mods into the same folder under a
   different name (e.g. `EasyAnimals_FF.dll`), so a local build next to it would load
   the mod twice.
2. Make the change, build, and test in game.
3. Bump the version in both the mod's `.csproj` (`<Version>`) and its `MelonInfo`
   attribute.
4. Update `workshop/description.txt` if behaviour changed.
5. In the uploader, select the existing Workshop item (do not click New), point it at
   the new DLL, and upload.
6. Delete the local DLL from `Mods\`, subscribe again, and check the Workshop copy loads.
7. Commit and push.

After a game update, rebuild against the new game DLLs and check `Latest.log` for
Harmony errors: patches target game methods by name, so renamed or changed methods
break them.
