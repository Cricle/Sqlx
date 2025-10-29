# Sqlx Extension Icons

## Required Icon File

**File**: `SqlxIcons.png`  
**Location**: `src/Sqlx.Extension/Resources/SqlxIcons.png`  
**Format**: PNG with transparency  
**Size**: 16x16 pixels per icon  
**Layout**: Horizontal strip containing 4 icons

---

## Icon Strip Layout

The `SqlxIcons.png` file should contain 5 icons in a horizontal strip (80x16 pixels total):

```
┌───────┬───────┬───────┬───────┬───────┐
│   1   │   2   │   3   │   4   │   5   │
│  SQL  │ Code  │ Test  │ Explr │  Log  │
└───────┴───────┴───────┴───────┴───────┘
 16x16   16x16   16x16   16x16   16x16
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

---

## Creating the Icon File

### Option 1: Create with Image Editor
1. Open your preferred image editor (Photoshop, GIMP, Paint.NET, etc.)
2. Create a new image: 80x16 pixels, transparent background
3. Draw 5 icons at positions:
   - Icon 1: 0-15px
   - Icon 2: 16-31px
   - Icon 3: 32-47px
   - Icon 4: 48-63px
   - Icon 5: 64-79px
4. Save as `SqlxIcons.png` with transparency

### Option 2: Use Visual Studio Icon Library
1. Extract icons from: `%ProgramFiles%\Microsoft Visual Studio\2022\<Edition>\Common7\VS\ImageLibrary`
2. Find suitable 16x16 icons
3. Combine into a horizontal strip
4. Save as `SqlxIcons.png`

### Option 3: Use Placeholder (For Testing)
Create a simple 80x16 PNG with colored squares:
- Square 1 (0-15): Blue
- Square 2 (16-31): Purple
- Square 3 (32-47): Green
- Square 4 (48-63): Orange
- Square 5 (64-79): Magenta

---

## Alternative: Remove Icons

If you don't want to create icons, you can remove icon references from `.vsct` file:

```xml
<!-- Remove this line from each Button element -->
<Icon guid="guidImages" id="bmpSql" />
```

The menu items will still work, just without custom icons.

---

## Icon Guidelines

### Visual Studio Standards
- **Size**: 16x16 pixels (standard menu icon size)
- **Format**: 32-bit PNG with alpha channel
- **Style**: Flat, monochromatic or themed
- **DPI**: 96 DPI (standard), provide @2x for high-DPI if needed

### Design Tips
- Keep designs simple and recognizable
- Use VS color palette for consistency
- Ensure good contrast on both light and dark themes
- Test with VS light and dark themes

---

## Testing

After creating the icon file:

1. Place `SqlxIcons.png` in `src/Sqlx.Extension/Resources/`
2. Build the extension
3. Test in VS Experimental Instance
4. Check menu items in `Tools > Sqlx`

---

## Troubleshooting

### Icons not showing
- Verify file path: `Resources/SqlxIcons.png`
- Check file is included in project
- Ensure file is set to `Content` or `Resource`
- Rebuild extension

### Icons look blurry
- Use exact 16x16 pixel size
- Don't resize from larger images
- Save as PNG, not JPEG

### Icons wrong colors
- Ensure transparency is preserved
- Use RGB color mode
- Test on both light/dark VS themes

---

## Current Status

⚠️ **Placeholder Needed**: The `SqlxIcons.png` file needs to be created.

**Options**:
1. Create custom icons (recommended)
2. Use VS icon library
3. Use simple colored squares for testing
4. Remove icon references from .vsct


