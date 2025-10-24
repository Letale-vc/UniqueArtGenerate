# UniqueArtGenerate

Web scraper that extracts unique item names and their icon art paths from poedb.tw (Path of Exile database).

## Download

Get the latest release: [Download for Windows](../../releases/latest)

## What it does

1. Fetches the unique items page from https://poedb.tw/us/Unique_item
2. Collects all unique item links from all sections (Weapons, Armour, Other)
3. Processes each item's detail page in parallel
4. Extracts the icon art path from each item
5. Saves results to `unique_items_output.txt`

## Output format

```
Abberath's Hooves;Art/2DItems/Armours/Boots/AbberathsHooves
Circle of Guilt;Art/2DItems/Rings/Ring1Unique
Lifesprig;Art/2DItems/Weapons/OneHandWeapons/Wands/Wand1Unique
```

Each line: `ItemName;IconPath` (semicolon separated for easy parsing)

## How to run

### Prebuilt binary (recommended)
1. Download `UniqueArtGenerate-win-x64.exe` from [Releases](../../releases)
2. Run it (double-click or run in terminal)
3. Wait for completion
4. Check `unique_items_output.txt` in the same folder

### Build from source

**Run with .NET SDK:**
```bash
dotnet run
```

**Build Native AOT:**
```bash
dotnet publish -c Release -r win-x64
```

Executable location: `bin\Release\net8.0\win-x64\publish\UniqueArtGenerate.exe`

## For developers

### Creating a new release

Push a tag to trigger automatic build and release:

```bash
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions will automatically build and create a release with the executable.
