# UniqueArtGenerate

Web scraper that extracts unique item names and their icon art paths from poedb.tw (Path of Exile database).

## Download

Get the latest release for your platform:
- [Windows (x64)](../../releases/latest)
- [Linux (x64)](../../releases/latest)
- [macOS (x64)](../../releases/latest)

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

### Prebuilt binaries (recommended)
1. Download the executable for your platform from [Releases](../../releases)
2. Run it:
   - **Windows**: Double-click `UniqueArtGenerate-win-x64.exe` or run in terminal
   - **Linux/macOS**: `chmod +x UniqueArtGenerate-* && ./UniqueArtGenerate-*`

### Build from source

**Option 1: Run with .NET SDK**
```bash
dotnet run --project UniqueArtGenerate
```

**Option 2: Build Native AOT**

Windows:
```bash
dotnet publish UniqueArtGenerate -c Release -r win-x64
```

Linux:
```bash
dotnet publish UniqueArtGenerate -c Release -r linux-x64
```

macOS:
```bash
dotnet publish UniqueArtGenerate -c Release -r osx-x64
```

Executable location: `UniqueArtGenerate\bin\Release\net8.0\{runtime}\publish\`

## For developers

### Creating a new release

Push a tag to trigger automatic build and release:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The GitHub Action will automatically:
- Build Native AOT binaries for Windows, Linux, and macOS
- Create a GitHub release
- Upload all binaries
