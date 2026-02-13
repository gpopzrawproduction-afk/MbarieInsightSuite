# ?? ASSETS FOLDER STRUCTURE & INTEGRATION GUIDE

## ? FOLDER STRUCTURE CREATED

```
MIC (project root)/
??? Assets/                          ? Created ?
?   ??? Logo/                        ? 4 files (Logo variants)
?   ?   ??? mic_logo_512.png        
?   ?   ??? mic_logo_256.png        (resized from 512)
?   ?   ??? mic_logo_128.png        (resized from 512)
?   ?   ??? mic_logo_horizontal.png 
?   ?
?   ??? Backgrounds/                ? 2 files (UI backgrounds)
?   ?   ??? bg_login.jpg            
?   ?   ??? bg_hex_pattern.png      
?   ?
?   ??? Icons/                      ? 20 Navigation & Action icons
?   ?   ??? Nav/                    ? 10 Navigation icons
?   ?   ?   ??? ic_dashboard.png    
?   ?   ?   ??? ic_alerts.png       
?   ?   ?   ??? ic_metrics.png      
?   ?   ?   ??? ic_chat.png         
?   ?   ?   ??? ic_email.png        
?   ?   ?   ??? ic_knowledge.png    
?   ?   ?   ??? ic_predictions.png  
?   ?   ?   ??? ic_reports.png      
?   ?   ?   ??? ic_profile.png      
?   ?   ?   ??? ic_settings.png     
?   ?   ?
?   ?   ??? Actions/                ? 10 Action icons
?   ?       ??? ic_send.png         
?   ?       ??? ic_reply.png        
?   ?       ??? ic_delete.png       
?   ?       ??? ic_attach.png       
?   ?       ??? ic_upload.png       
?   ?       ??? ic_search.png       
?   ?       ??? ic_notifications.png
?   ?       ??? ic_expand.png       
?   ?       ??? ic_collapse.png     
?   ?       ??? ic_ai_sparkle.png   
?   ?
?   ??? Images/                     ? 2 Avatar images
?       ??? avatar_default.png      
?       ??? avatar_ai.png           
?
??? MIC.Desktop.Avalonia/           ? Link to Assets folder in XAML
    ??? Resources/
        ??? Assets ? symlink to ../Assets
```

---

## ?? FILE INVENTORY (28 ITEMS)

### **Logos (4 files)**
- ? `mic_logo_512.png` - Primary logo (512x512)
- ? `mic_logo_256.png` - Medium logo (256x256)
- ? `mic_logo_128.png` - Small logo (128x128)
- ? `mic_logo_horizontal.png` - Horizontal variant for headers

### **Backgrounds (2 files)**
- ? `bg_login.jpg` - Login page background
- ? `bg_hex_pattern.png` - Dashboard hex pattern background

### **Navigation Icons (10 files)**
- ? `ic_dashboard.png` - Dashboard menu icon
- ? `ic_alerts.png` - Alerts menu icon
- ? `ic_metrics.png` - Metrics menu icon
- ? `ic_chat.png` - Chat menu icon
- ? `ic_email.png` - Email menu icon (Week 1 module)
- ? `ic_knowledge.png` - Knowledge Base menu icon (Week 3 module)
- ? `ic_predictions.png` - Predictions menu icon (Week 4 module)
- ? `ic_reports.png` - Reports menu icon (Week 4 module)
- ? `ic_profile.png` - User Profile menu icon (Week 2 module)
- ? `ic_settings.png` - Settings menu icon

### **Action Icons (10 files)**
- ? `ic_send.png` - Send action (Email)
- ? `ic_reply.png` - Reply action (Email)
- ? `ic_delete.png` - Delete action (General)
- ? `ic_attach.png` - Attach file action (Email)
- ? `ic_upload.png` - Upload document action (Knowledge Base)
- ? `ic_search.png` - Search action (General)
- ? `ic_notifications.png` - Notifications action
- ? `ic_expand.png` - Expand action (General)
- ? `ic_collapse.png` - Collapse action (General)
- ? `ic_ai_sparkle.png` - AI sparkle action (AI features)

### **Avatar Images (2 files)**
- ? `avatar_default.png` - Default user avatar
- ? `avatar_ai.png` - AI assistant avatar

---

## ?? INTEGRATION STEPS

### **Step 1: Copy Images to Folders**
Place your 21 generated images into the corresponding folders:

```
Assets/
??? Logo/ ? Place 4 logo images here
??? Backgrounds/ ? Place 2 background images here
??? Icons/Nav/ ? Place 10 navigation icons here
??? Icons/Actions/ ? Place 10 action icons here
??? Images/ ? Place 2 avatar images here
```

### **Step 2: Create Symlink (Optional but Recommended)**
```powershell
# In Visual Studio Terminal (from project root)
cd MIC.Desktop.Avalonia\Resources
New-Item -ItemType SymbolicLink -Name Assets -Target "../../Assets"
```

### **Step 3: Update XAML Resource References**

**Option A: Direct File Path**
```xaml
<Image Source="/Assets/Logo/mic_logo_256.png" />
```

**Option B: Resource String (Recommended)**
Add to `ResourceHelper.cs`:
```csharp
public static string GetAssetPath(string assetName) =>
    $"/Assets/{assetName}";
```

Then use:
```xaml
<Image Source="{Binding GetAssetPath('Logo/mic_logo_256.png')}" />
```

### **Step 4: Update Project File**

**MIC.Desktop.Avalonia.csproj**
```xml
<ItemGroup>
    <AvaloniaResource Include="../../Assets/**/*">
        <Link>Assets/%(Filename)%(Extension)</Link>
    </AvaloniaResource>
</ItemGroup>
```

### **Step 5: Verify in Build**

```bash
# Rebuild to include resources
dotnet build MIC.slnx --configuration Debug
```

---

## ?? USAGE IN XAML FILES

### **Logo Usage (MainWindow, Dialogs)**
```xaml
<Image Source="/Assets/Logo/mic_logo_256.png" 
       Width="256" Height="256" />
```

### **Navigation Icons (MainWindow Menu)**
```xaml
<!-- Email Navigation Item -->
<MenuItem Header="Email">
    <Image Source="/Assets/Icons/Nav/ic_email.png" Width="20" Height="20" />
</MenuItem>

<!-- Knowledge Base Navigation Item -->
<MenuItem Header="Knowledge Base">
    <Image Source="/Assets/Icons/Nav/ic_knowledge.png" Width="20" Height="20" />
</MenuItem>

<!-- Predictions Navigation Item -->
<MenuItem Header="Predictions">
    <Image Source="/Assets/Icons/Nav/ic_predictions.png" Width="20" Height="20" />
</MenuItem>

<!-- Reports Navigation Item -->
<MenuItem Header="Reports">
    <Image Source="/Assets/Icons/Nav/ic_reports.png" Width="20" Height="20" />
</MenuItem>
```

### **Action Icons (Buttons, Controls)**
```xaml
<!-- Send Button (Email) -->
<Button Command="{Binding SendEmailCommand}">
    <Image Source="/Assets/Icons/Actions/ic_send.png" Width="24" Height="24" />
</Button>

<!-- Upload Button (Knowledge Base) -->
<Button Command="{Binding UploadDocumentCommand}">
    <Image Source="/Assets/Icons/Actions/ic_upload.png" Width="24" Height="24" />
</Button>

<!-- Search Button -->
<Button Command="{Binding SearchCommand}">
    <Image Source="/Assets/Icons/Actions/ic_search.png" Width="24" Height="24" />
</Button>
```

### **Background Images**
```xaml
<!-- Login Page Background -->
<Border Background="ImageBrush {Source=/Assets/Backgrounds/bg_login.jpg}" />

<!-- Dashboard Background -->
<Border Background="ImageBrush {Source=/Assets/Backgrounds/bg_hex_pattern.png}" />
```

### **Avatar Images**
```xaml
<!-- User Profile Avatar -->
<Image Source="/Assets/Images/avatar_default.png" 
       Width="64" Height="64" 
       CornerRadius="32" />

<!-- AI Assistant Avatar -->
<Image Source="/Assets/Images/avatar_ai.png" 
       Width="48" Height="48" 
       CornerRadius="24" />
```

---

## ?? ASSET MAPPING TO MODULES

| Module | Icon Files | Usage |
|--------|-----------|-------|
| **Week 1: Email** | ic_email.png, ic_send.png, ic_reply.png, ic_delete.png, ic_attach.png | Navigation & email actions |
| **Week 2: Users** | ic_profile.png, ic_settings.png, avatar_default.png | User menu & profile |
| **Week 3: Knowledge Base** | ic_knowledge.png, ic_upload.png, ic_search.png | KB navigation & actions |
| **Week 4: Predictions** | ic_predictions.png, ic_reports.png | Predictions & reports nav |
| **Common** | ic_dashboard.png, ic_alerts.png, ic_metrics.png, ic_chat.png, ic_notifications.png, ic_expand.png, ic_collapse.png, ic_ai_sparkle.png, avatar_ai.png | Shared UI elements |

---

## ?? NEXT STEPS

### **When Images are Ready:**
1. ? Copy all 21 PNG/JPG images to appropriate folders
2. ? Update XAML files to reference images
3. ? Test on Windows 11
4. ? Test on macOS (verify path resolution)
5. ? Build Release configuration
6. ? Ready for packaging (MSIX + DMG)

### **Testing Commands:**
```bash
# Verify folder structure
dir Assets /s

# Build with resources
dotnet build MIC.slnx --configuration Release

# Test application
MIC.Desktop.Avalonia.exe
```

---

## ? CHECKLIST

- [x] Folder structure created
- [ ] 21 images copied to folders
- [ ] XAML files updated with image references
- [ ] ResourceHelper.cs updated
- [ ] Project file updated with AvaloniaResource items
- [ ] Release build succeeds
- [ ] Images display correctly in UI
- [ ] Cross-platform verified (Windows + macOS)
- [ ] Ready for MSIX/DMG packaging

---

## ?? ASSET INTEGRATION SUPPORT

**Questions about asset placement?**
- All images go in `C:\MbarieIntelligenceConsole\src\MIC\Assets\`
- Structure mirrors the folder organization above
- XAML references use `/Assets/FolderName/FileName.png`

**Ready to integrate your 21 images!** ??

