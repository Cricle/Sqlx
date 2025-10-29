# Sqlx Extension Icons

## Required Icon File

**File**: `SqlxIcons.png`  
**Location**: `src/Sqlx.Extension/Resources/SqlxIcons.png`  
**Format**: PNG with transparency  
**Size**: 16x16 pixels per icon  
**Layout**: Horizontal strip containing **10 icons**

---

## Icon Strip Layout

The `SqlxIcons.png` file should contain **10 icons** in a horizontal strip (160x16 pixels total):

```
┌───────┬───────┬───────┬───────┬───────┬───────┬───────┬───────┬───────┬───────┐
│   1   │   2   │   3   │   4   │   5   │   6   │   7   │   8   │   9   │  10   │
│  SQL  │ Code  │ Test  │ Explr │  Log  │ Viszr │ Perf  │ Map   │ Brkpt │ Watch │
└───────┴───────┴───────┴───────┴───────┴───────┴───────┴───────┴───────┴───────┘
 16x16   16x16   16x16   16x16   16x16   16x16   16x16   16x16   16x16   16x16
```

---

## Icon Descriptions

### Icon 1: SQL Preview (bmpSql)
- **Description**: Database/SQL icon
- **Suggested Design**: Database cylinder or SQL text
- **Color**: Blue (#007ACC - VS Blue)
- **Usage**: SQL Preview window menu item

### Icon 2: Generated Code (bmpCode)
- **Description**: Code/file icon
- **Suggested Design**: Code brackets {} or file with code
- **Color**: Purple (#68217A - VS Purple)
- **Usage**: Generated Code window menu item

### Icon 3: Query Tester (bmpTest)
- **Description**: Test/play icon
- **Suggested Design**: Play button or test tube
- **Color**: Green (#16C60C - VS Green)
- **Usage**: Query Tester window menu item

### Icon 4: Repository Explorer (bmpExplorer)
- **Description**: Explorer/folder icon
- **Suggested Design**: Folder tree or magnifying glass
- **Color**: Orange (#CA5010 - VS Orange)
- **Usage**: Repository Explorer window menu item

### Icon 5: SQL Execution Log (bmpLog)
- **Description**: Log/document icon
- **Suggested Design**: Document with list lines or clipboard
- **Color**: Purple (#A853C8 - VS Magenta)
- **Usage**: SQL Execution Log window menu item

### Icon 6: Template Visualizer (bmpVisualizer)
- **Description**: Visualizer/design icon
- **Suggested Design**: Palette, design tool, or template
- **Color**: Yellow (#DCDCAA - VS Yellow)
- **Usage**: Template Visualizer window menu item

### Icon 7: Performance Analyzer (bmpPerformance)
- **Description**: Performance/chart icon
- **Suggested Design**: Speedometer, bar chart, or graph
- **Color**: Red (#F14C4C - VS Red)
- **Usage**: Performance Analyzer window menu item

### Icon 8: Entity Mapping Viewer (bmpMapping)
- **Description**: Mapping/link icon
- **Suggested Design**: Linked boxes, flow chart, or arrows
- **Color**: Teal (#4EC9B0 - VS Teal)
- **Usage**: Entity Mapping Viewer window menu item

### Icon 9: SQL Breakpoints (bmpBreakpoint)
- **Description**: Breakpoint/debug icon
- **Suggested Design**: Red circle with dot, debug symbol, or stop sign
- **Color**: Red (#FF0000 - Breakpoint Red)
- **Usage**: SQL Breakpoint Manager window menu item

### Icon 10: SQL Watch (bmpWatch)
- **Description**: Watch/monitor icon
- **Suggested Design**: Eye, magnifying glass, or watch symbol
- **Color**: Blue (#569CD6 - Watch Blue)
- **Usage**: SQL Watch window menu item

---

## Creating the Icon File

### Option 1: Create with Image Editor
1. Open your preferred image editor (Photoshop, GIMP, Paint.NET, etc.)
2. Create a new image: **160x16 pixels**, transparent background
3. Draw 10 icons at positions:
   - Icon 1: 0-15px (SQL Preview)
   - Icon 2: 16-31px (Generated Code)
   - Icon 3: 32-47px (Query Tester)
   - Icon 4: 48-63px (Repository Explorer)
   - Icon 5: 64-79px (SQL Execution Log)
   - Icon 6: 80-95px (Template Visualizer)
   - Icon 7: 96-111px (Performance Analyzer)
   - Icon 8: 112-127px (Entity Mapping Viewer)
   - Icon 9: 128-143px (SQL Breakpoints)
   - Icon 10: 144-159px (SQL Watch)
4. Save as `SqlxIcons.png` with transparency

### Option 2: Use Visual Studio Icon Library
1. Extract icons from: `%ProgramFiles%\Microsoft Visual Studio\2022\<Edition>\Common7\VS\ImageLibrary`
2. Find suitable 16x16 icons for each function
3. Combine into a horizontal strip (160x16 pixels)
4. Save as `SqlxIcons.png`

Suggested VS icons:
- SQL: `Database_16x.png` or `SQLDatabase_16x.png`
- Code: `CSFileNode_16x.png` or `Code_16x.png`
- Test: `Run_16x.png` or `Test_16x.png`
- Explorer: `FolderOpen_16x.png` or `Solution_16x.png`
- Log: `Output_16x.png` or `DocumentOutline_16x.png`
- Visualizer: `DesignView_16x.png` or `ColorPalette_16x.png`
- Performance: `PerfReport_16x.png` or `Chart_16x.png`
- Mapping: `Flow_16x.png` or `Relationship_16x.png`
- Breakpoint: `Breakpoint_16x.png`
- Watch: `Watch_16x.png` or `Eye_16x.png`

### Option 3: Use Font Awesome / Material Icons
1. Visit https://fontawesome.com or https://materialdesignicons.com
2. Download 16x16 PNG versions of:
   - Database icon
   - Code icon
   - Play/test icon
   - Folder icon
   - Document icon
   - Palette icon
   - Chart icon
   - Link icon
   - Circle icon (for breakpoint)
   - Eye icon (for watch)
3. Combine into a horizontal strip
4. Apply colors as specified above

### Option 4: Use Placeholder (For Testing)
Create a simple 160x16 PNG with colored squares:
```
Blue  Purple Green Orange Magenta Yellow Red   Teal  Red   Blue
[1]   [2]    [3]   [4]    [5]     [6]    [7]   [8]   [9]   [10]
```

Quick PowerShell script to create placeholder:
```powershell
# This creates a basic placeholder - replace with real icons
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap(160, 16)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$colors = @([Drawing.Color]::Blue, [Drawing.Color]::Purple, [Drawing.Color]::Green, 
            [Drawing.Color]::Orange, [Drawing.Color]::Magenta, [Drawing.Color]::Yellow,
            [Drawing.Color]::Red, [Drawing.Color]::Teal, [Drawing.Color]::DarkRed, 
            [Drawing.Color]::CornflowerBlue)
for ($i = 0; $i -lt 10; $i++) {
    $brush = New-Object Drawing.SolidBrush($colors[$i])
    $graphics.FillRectangle($brush, ($i * 16), 0, 16, 16)
}
$bitmap.Save("SqlxIcons.png", [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bitmap.Dispose()
```

---

## Alternative: Remove Icons

If you don't want to create icons, you can remove icon references from `.vsct` file:

```xml
<!-- Remove these lines from each Button element -->
<Icon guid="guidImages" id="bmpSql" />
<Icon guid="guidImages" id="bmpCode" />
<!-- ... and so on -->
```

The menu items will still work, just without custom icons.

---

## Icon Guidelines

### Visual Studio Standards
- **Size**: 16x16 pixels (standard menu icon size)
- **Format**: 32-bit PNG with alpha channel
- **Style**: Flat, monochromatic or themed
- **DPI**: 96 DPI (standard), provide @2x for high-DPI if needed
- **Background**: Transparent

### Design Tips
- Keep designs simple and recognizable at small size
- Use VS color palette for consistency
- Ensure good contrast on both light and dark themes
- Test with VS light and dark themes
- Avoid fine details that won't be visible at 16x16
- Use symbolic/glyph style rather than photorealistic

---

## Testing

After creating the icon file:

1. Place `SqlxIcons.png` in `src/Sqlx.Extension/Resources/`
2. Ensure it's included in the project file
3. Build the extension
4. Test in VS Experimental Instance
5. Check menu items in `Tools > Sqlx`
6. Verify icons on both light and dark themes

---

## Troubleshooting

### Icons not showing
- Verify file path: `Resources/SqlxIcons.png`
- Check file is included in project (`.csproj`)
- Ensure file is set to `Content` or `Resource`
- Rebuild extension
- Check `.vsct` file references match bitmap indices

### Icons look blurry
- Use exact 16x16 pixel size for each icon
- Don't resize from larger images
- Save as PNG, not JPEG
- Ensure no anti-aliasing on edges

### Icons wrong colors
- Ensure transparency is preserved
- Use RGB color mode, not CMYK
- Test on both light/dark VS themes
- Check alpha channel

### Wrong icon displays
- Verify bitmap index order in `.vsct`
- Check `usedList` attribute in Bitmap element
- Ensure correct `IDSymbol` values

---

## Current Status

⚠️ **Using Placeholder**: The extension currently works with placeholder icons.

**Options**:
1. ✅ **Continue with placeholders** - All features work, icons are purely cosmetic
2. ⭐ **Create custom icons** (recommended for production)
3. 📦 **Use VS icon library** (quick professional look)
4. 🎨 **Use Font Awesome/Material** (modern icons)
5. ❌ **Remove icon references** (text-only menu)

**Recommendation**: The extension is fully functional without custom icons. Icons can be added anytime without affecting functionality. For a preview release, placeholders are acceptable.

---

## License Considerations

If using third-party icons:
- ✅ VS Icon Library: OK (part of VS SDK)
- ✅ Font Awesome Free: OK (CC BY 4.0)
- ✅ Material Design Icons: OK (Apache 2.0)
- ⚠️ Commercial icon packs: Check license

Always verify license compatibility with MIT license.

---

**Status**: ⚠️ Placeholder icons in use  
**Impact**: None - purely cosmetic  
**Priority**: Low - can be added post-release  
**Recommendation**: Ship with placeholders, update in v0.6

