using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Identity.Services;

/// <summary>
/// Implementation of ILocalizationService for multilingual support.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;
    private readonly Dictionary<UserLanguage, CultureInfo> _languageCultureMap;
    private readonly Dictionary<CultureInfo, UserLanguage> _cultureLanguageMap;

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;
        
        // Initialize resource manager for MIC.Desktop.Avalonia resources
        _resourceManager = new ResourceManager("MIC.Desktop.Avalonia.Resources.Resources", 
            typeof(LocalizationService).Assembly);
        
        // Initialize culture mappings
        _languageCultureMap = new Dictionary<UserLanguage, CultureInfo>
        {
            { UserLanguage.English, new CultureInfo("en-US") },
            { UserLanguage.French, new CultureInfo("fr-FR") },
            { UserLanguage.Spanish, new CultureInfo("es-ES") },
            { UserLanguage.Arabic, new CultureInfo("ar-SA") },
            { UserLanguage.Chinese, new CultureInfo("zh-CN") }
        };
        
        _cultureLanguageMap = _languageCultureMap.ToDictionary(
            kvp => kvp.Value,
            kvp => kvp.Key);
        
        // Set default culture
        _currentCulture = _languageCultureMap[UserLanguage.English];
        
        // Set thread culture
        Thread.CurrentThread.CurrentCulture = _currentCulture;
        Thread.CurrentThread.CurrentUICulture = _currentCulture;
        
        _logger.LogInformation("LocalizationService initialized with default culture: {Culture}", 
            _currentCulture.Name);
    }

    public string GetString(string key)
    {
        return GetString(key, _currentCulture);
    }

    public string GetString(string key, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        try
        {
            var localizedString = _resourceManager.GetString(key, culture);
            return localizedString ?? key;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get localized string for key '{Key}' with culture '{Culture}'", 
                key, culture.Name);
            return key;
        }
    }

    public string GetString(string key, UserLanguage language)
    {
        var culture = UserLanguageToCulture(language);
        return GetString(key, culture);
    }

    public void SetCurrentCulture(CultureInfo culture)
    {
        if (culture == null)
        {
            throw new ArgumentNullException(nameof(culture));
        }

        _currentCulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        
        _logger.LogInformation("Current culture set to: {Culture}", culture.Name);
    }

    public void SetCurrentCulture(UserLanguage language)
    {
        var culture = UserLanguageToCulture(language);
        SetCurrentCulture(culture);
    }

    public CultureInfo GetCurrentCulture()
    {
        return _currentCulture;
    }

    public UserLanguage GetCurrentUserLanguage()
    {
        return CultureToUserLanguage(_currentCulture);
    }

    public UserLanguage CultureToUserLanguage(CultureInfo culture)
    {
        if (culture == null)
        {
            return UserLanguage.English;
        }

        // Try exact match first
        if (_cultureLanguageMap.TryGetValue(culture, out var language))
        {
            return language;
        }

        // Try parent culture match (e.g., "en" for "en-US")
        var parentCulture = culture.Parent;
        if (parentCulture != null && parentCulture != CultureInfo.InvariantCulture)
        {
            foreach (var kvp in _cultureLanguageMap)
            {
                if (kvp.Key.Name.StartsWith(parentCulture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
        }

        // Default to English
        return UserLanguage.English;
    }

    public CultureInfo UserLanguageToCulture(UserLanguage language)
    {
        if (_languageCultureMap.TryGetValue(language, out var culture))
        {
            return culture;
        }

        // Default to English
        _logger.LogWarning("No culture mapping found for language {Language}, defaulting to English", language);
        return _languageCultureMap[UserLanguage.English];
    }

    public CultureInfo[] GetSupportedCultures()
    {
        return _languageCultureMap.Values.ToArray();
    }

    public UserLanguage[] GetSupportedUserLanguages()
    {
        return _languageCultureMap.Keys.ToArray();
    }

    public string GetLanguageDisplayName(UserLanguage language)
    {
        return GetLanguageDisplayName(language, _currentCulture);
    }

    public string GetLanguageDisplayName(UserLanguage language, CultureInfo culture)
    {
        var targetCulture = UserLanguageToCulture(language);
        
        // Get the native name of the language
        var nativeName = targetCulture.NativeName;
        
        // If the display culture is different from the target culture,
        // get the display name in the display culture
        if (culture.Name != targetCulture.Name)
        {
            try
            {
                var displayName = targetCulture.DisplayName;
                return $"{nativeName} ({displayName})";
            }
            catch
            {
                // Fallback to native name
                return nativeName;
            }
        }
        
        return nativeName;
    }

    public bool IsRtlCulture(CultureInfo culture)
    {
        if (culture == null)
        {
            return false;
        }

        // Arabic cultures are RTL
        return culture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsRtlLanguage(UserLanguage language)
    {
        return language == UserLanguage.Arabic;
    }

    public string FormatDate(DateTime date)
    {
        return FormatDate(date, _currentCulture);
    }

    public string FormatDate(DateTime date, CultureInfo culture)
    {
        try
        {
            return date.ToString("d", culture);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format date with culture '{Culture}'", culture.Name);
            return date.ToString("d", CultureInfo.InvariantCulture);
        }
    }

    public string FormatNumber(double number)
    {
        return FormatNumber(number, _currentCulture);
    }

    public string FormatNumber(double number, CultureInfo culture)
    {
        try
        {
            return number.ToString("N", culture);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format number with culture '{Culture}'", culture.Name);
            return number.ToString("N", CultureInfo.InvariantCulture);
        }
    }
}