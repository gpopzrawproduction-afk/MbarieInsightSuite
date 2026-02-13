# ?? Assets Folder - Mbarie Intelligence Console v1.0.0

## Overview

This folder contains all visual assets (logos, icons, backgrounds, avatars) for the Mbarie Intelligence Console desktop application.

## Folder Structure

```
Assets/
??? Logo/              (4 PNG files - Application logos)
??? Backgrounds/       (2 PNG/JPG files - UI backgrounds)
??? Icons/
?   ??? Nav/          (10 PNG files - Navigation menu icons)
?   ??? Actions/      (10 PNG files - Action/button icons)
??? Images/           (2 PNG files - Avatar images)
```

## ?? File Manifest (21 Images)

### Logo Assets (4 files)
- **mic_logo_512.png** - Primary logo at 512x512 pixels
- **mic_logo_256.png** - Medium logo at 256x256 pixels (resized from 512)
- **mic_logo_128.png** - Small logo at 128x128 pixels (resized from 512)
- **mic_logo_horizontal.png** - Horizontal variant for headers and titles

### Background Assets (2 files)
- **bg_login.jpg** - Login page background image
- **bg_hex_pattern.png** - Dashboard hexagon pattern background

### Navigation Icons (10 files)
Used in the main menu and navigation sidebar:
- **ic_dashboard.png** - Dashboard view icon
- **ic_alerts.png** - Alerts view icon
- **ic_metrics.png** - Metrics dashboard icon
- **ic_chat.png** - Chat/AI assistant icon
- **ic_email.png** - Email inbox icon (Week 1)
- **ic_knowledge.png** - Knowledge Base icon (Week 3)
- **ic_predictions.png** - Predictions view icon (Week 4)
- **ic_reports.png** - Reports view icon (Week 4)
- **ic_profile.png** - User profile icon (Week 2)
- **ic_settings.png** - Settings view icon

### Action Icons (10 files)
Used for buttons and interactive elements:
- **ic_send.png** - Send message/email
- **ic_reply.png** - Reply to email
- **ic_delete.png** - Delete item
- **ic_attach.png** - Attach file
- **ic_upload.png** - Upload document
- **ic_search.png** - Search functionality
- **ic_notifications.png** - Notifications panel
- **ic_expand.png** - Expand/maximize
- **ic_collapse.png** - Collapse/minimize
- **ic_ai_sparkle.png** - AI feature indicator

### Avatar Images (2 files)
- **avatar_default.png** - Default user avatar
- **avatar_ai.png** - AI assistant avatar

## ?? Icon Design Guidelines

### Size Specifications
- **Navigation Icons**: 20x20 or 24x24 pixels (at 1x scale)
- **Action Icons**: 24x24 or 32x32 pixels (at 1x scale)
- **Logo**: 256x256 or 512x512 pixels
- **Backgrounds**: 1920x1080 or higher

### Design Standards
- **Format**: PNG (with transparency for icons), JPG (for backgrounds)
- **Transparency**: Always use PNG with alpha channel for icons
- **Style**: Consistent with Mbarie brand (minimal, modern, professional)
- **Color**: Follow BrandColors.cs for brand colors
- **Padding**: Leave 2-4px padding inside icon boundaries

### Naming Convention
```
ic_{module}_{action}.png        (e.g., ic_email_send.png)
bg_{usage}_{variant}.{ext}      (e.g., bg_login_dark.jpg)
{item_type}_{size}.png          (e.g., mic_logo_256.png)
```

## ?? Integration in XAML

### Reference Pattern
```xaml
<!-- Full path reference -->
<Image Source="/Assets/Icons/Nav/ic_dashboard.png" Width="24" Height="24" />

<!-- Via ResourceHelper -->
<Image Source="{Binding GetAssetPath('Icons/Nav/ic_dashboard.png')}" />
```

### In Code-Behind
```csharp
// C#
var imagePath = "/Assets/Logo/mic_logo_256.png";
var imageUri = new Uri(imagePath, UriKind.Relative);
var bitmap = new Bitmap(imageUri);
```

## ?? Cross-Platform Paths

### Windows
```
/Assets/Icons/Nav/ic_dashboard.png
```

### macOS
```
/Assets/Icons/Nav/ic_dashboard.png
```

**Note**: Avalonia XAML uses `/Assets/...` format for both platforms. The framework handles path resolution.

## ? Deployment

### MSIX Packaging (Windows)
Assets are embedded in the MSIX package. Ensure they're included:
```xml
<AvaloniaResource Include="../../Assets/**/*">
    <Link>Assets/%(Filename)%(Extension)</Link>
</AvaloniaResource>
```

### DMG Packaging (macOS)
Assets are bundled with the application bundle.

## ?? Asset Updates

To update or add new assets:

1. **Add/Replace Files** in appropriate `Assets/*/` folder
2. **Update References** in XAML files if needed
3. **Test on Both Platforms** (Windows & macOS)
4. **Rebuild** the application
5. **Commit** to GitHub with asset changes

## ?? Asset Checklist

- [ ] All 21 images in correct folders
- [ ] Images are properly sized
- [ ] PNG images have transparency where needed
- [ ] File names match the manifest
- [ ] XAML references updated
- [ ] Cross-platform paths verified
- [ ] No missing or broken image references
- [ ] Build includes all assets
- [ ] Tested on Windows 11
- [ ] Tested on macOS
- [ ] Ready for release packaging

## ?? Release Status

**Assets Created**: ? February 14, 2026  
**Integration Status**: ? Awaiting image placement  
**Build Status**: ? Pending asset integration  
**Release Status**: ? Ready after image integration  

---

**Questions or Issues?** Refer to ASSETS_INTEGRATION_GUIDE.md for detailed integration steps.

