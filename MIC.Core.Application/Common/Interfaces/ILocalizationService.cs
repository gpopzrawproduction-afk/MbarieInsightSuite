using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Interface for localization and multilingual support services.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string for the given key using the current culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string or the key if not found.</returns>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string for the given key using the specified culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The target culture.</param>
    /// <returns>The localized string or the key if not found.</returns>
    string GetString(string key, CultureInfo culture);

    /// <summary>
    /// Gets a localized string for the given key using the specified user language.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="language">The user language.</param>
    /// <returns>The localized string or the key if not found.</returns>
    string GetString(string key, UserLanguage language);

    /// <summary>
    /// Sets the current UI culture for the application.
    /// </summary>
    /// <param name="culture">The culture to set.</param>
    void SetCurrentCulture(CultureInfo culture);

    /// <summary>
    /// Sets the current UI culture based on user language.
    /// </summary>
    /// <param name="language">The user language.</param>
    void SetCurrentCulture(UserLanguage language);

    /// <summary>
    /// Gets the current UI culture.
    /// </summary>
    /// <returns>The current culture.</returns>
    CultureInfo GetCurrentCulture();

    /// <summary>
    /// Gets the current user language based on culture.
    /// </summary>
    /// <returns>The current user language.</returns>
    UserLanguage GetCurrentUserLanguage();

    /// <summary>
    /// Converts a CultureInfo to UserLanguage.
    /// </summary>
    /// <param name="culture">The culture to convert.</param>
    /// <returns>The corresponding UserLanguage.</returns>
    UserLanguage CultureToUserLanguage(CultureInfo culture);

    /// <summary>
    /// Converts a UserLanguage to CultureInfo.
    /// </summary>
    /// <param name="language">The user language to convert.</param>
    /// <returns>The corresponding CultureInfo.</returns>
    CultureInfo UserLanguageToCulture(UserLanguage language);

    /// <summary>
    /// Gets all supported cultures.
    /// </summary>
    /// <returns>Array of supported cultures.</returns>
    CultureInfo[] GetSupportedCultures();

    /// <summary>
    /// Gets all supported user languages.
    /// </summary>
    /// <returns>Array of supported user languages.</returns>
    UserLanguage[] GetSupportedUserLanguages();

    /// <summary>
    /// Gets the display name for a user language in the current culture.
    /// </summary>
    /// <param name="language">The user language.</param>
    /// <returns>The display name.</returns>
    string GetLanguageDisplayName(UserLanguage language);

    /// <summary>
    /// Gets the display name for a user language in the specified culture.
    /// </summary>
    /// <param name="language">The user language.</param>
    /// <param name="culture">The target culture.</param>
    /// <returns>The display name.</returns>
    string GetLanguageDisplayName(UserLanguage language, CultureInfo culture);

    /// <summary>
    /// Checks if a culture is RTL (Right-to-Left).
    /// </summary>
    /// <param name="culture">The culture to check.</param>
    /// <returns>True if the culture is RTL.</returns>
    bool IsRtlCulture(CultureInfo culture);

    /// <summary>
    /// Checks if a user language is RTL (Right-to-Left).
    /// </summary>
    /// <param name="language">The user language to check.</param>
    /// <returns>True if the language is RTL.</returns>
    bool IsRtlLanguage(UserLanguage language);

    /// <summary>
    /// Formats a date according to the current culture.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>The formatted date string.</returns>
    string FormatDate(DateTime date);

    /// <summary>
    /// Formats a date according to the specified culture.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <param name="culture">The target culture.</param>
    /// <returns>The formatted date string.</returns>
    string FormatDate(DateTime date, CultureInfo culture);

    /// <summary>
    /// Formats a number according to the current culture.
    /// </summary>
    /// <param name="number">The number to format.</param>
    /// <returns>The formatted number string.</returns>
    string FormatNumber(double number);

    /// <summary>
    /// Formats a number according to the specified culture.
    /// </summary>
    /// <param name="number">The number to format.</param>
    /// <param name="culture">The target culture.</param>
    /// <returns>The formatted number string.</returns>
    string FormatNumber(double number, CultureInfo culture);
}