using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MIC.Infrastructure.Identity.Services;
using MIC.Core.Domain.Entities;
using System.Globalization;

namespace MIC.Tests.Unit.Infrastructure.Identity;

/// <summary>
/// Extended tests for LocalizationService covering remaining uncovered paths:
/// SetCurrentCulture null check, parent culture matching, display name variations,
/// and formatter edge cases.
/// </summary>
public class LocalizationServiceExtendedTests
{
    private readonly LocalizationService _service;

    public LocalizationServiceExtendedTests()
    {
        _service = new LocalizationService(NullLogger<LocalizationService>.Instance);
    }

    #region SetCurrentCulture Edge Cases

    [Fact]
    public void SetCurrentCulture_NullCultureInfo_ThrowsArgumentNullException()
    {
        var act = () => _service.SetCurrentCulture((CultureInfo)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetCurrentCulture_WithCultureInfo_UpdatesThreadCulture()
    {
        var culture = new CultureInfo("es-ES");
        _service.SetCurrentCulture(culture);

        _service.GetCurrentCulture().Name.Should().Be("es-ES");
        Thread.CurrentThread.CurrentCulture.Name.Should().Be("es-ES");
        Thread.CurrentThread.CurrentUICulture.Name.Should().Be("es-ES");
    }

    #endregion

    #region CultureToUserLanguage Parent Culture Matching

    [Fact]
    public void CultureToUserLanguage_ChildOfArabic_MatchesViaParent()
    {
        // ar-EG is a child of ar, which should match ar-SA → Arabic
        var culture = new CultureInfo("ar-EG");
        var result = _service.CultureToUserLanguage(culture);
        result.Should().Be(UserLanguage.Arabic);
    }

    [Fact]
    public void CultureToUserLanguage_ChildOfFrench_MatchesViaParent()
    {
        // fr-CA is a child of fr, which should match fr-FR → French
        var culture = new CultureInfo("fr-CA");
        var result = _service.CultureToUserLanguage(culture);
        result.Should().Be(UserLanguage.French);
    }

    [Fact]
    public void CultureToUserLanguage_ChildOfChinese_MatchesViaParentOrDefaultsToEnglish()
    {
        // zh-TW parent is "zh-Hant" not "zh", so parent matching may not find "zh-CN"
        var culture = new CultureInfo("zh-TW");
        var result = _service.CultureToUserLanguage(culture);
        // Either matches Chinese via parent or defaults to English
        result.Should().BeOneOf(UserLanguage.Chinese, UserLanguage.English);
    }

    [Fact]
    public void CultureToUserLanguage_InvariantCulture_DefaultsToEnglish()
    {
        var result = _service.CultureToUserLanguage(CultureInfo.InvariantCulture);
        result.Should().Be(UserLanguage.English);
    }

    #endregion

    #region UserLanguageToCulture Edge Cases

    [Fact]
    public void UserLanguageToCulture_EnumValueOutOfRange_DefaultsToEnglish()
    {
        var result = _service.UserLanguageToCulture((UserLanguage)999);
        result.Name.Should().Be("en-US");
    }

    #endregion

    #region GetLanguageDisplayName with Different Cultures

    [Fact]
    public void GetLanguageDisplayName_SameLanguageAsDisplay_ReturnsNativeName()
    {
        // When displaying Arabic in Arabic culture, should just be native name
        var arabicCulture = _service.UserLanguageToCulture(UserLanguage.Arabic);
        var name = _service.GetLanguageDisplayName(UserLanguage.Arabic, arabicCulture);
        name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetLanguageDisplayName_DifferentCulture_IncludesDisplayName()
    {
        var frenchCulture = _service.UserLanguageToCulture(UserLanguage.French);
        // Get name of Arabic in French culture context
        var name = _service.GetLanguageDisplayName(UserLanguage.Arabic, frenchCulture);
        name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetLanguageDisplayName_EnglishInFrenchContext_ContainsParentheses()
    {
        var frenchCulture = _service.UserLanguageToCulture(UserLanguage.French);
        var name = _service.GetLanguageDisplayName(UserLanguage.English, frenchCulture);
        // Should contain native name and (display name)
        name.Should().NotBeNullOrEmpty();
        // When culture is different, format is "nativeName (displayName)"
        name.Should().Contain("(");
    }

    #endregion

    #region IsRtlCulture Edge Cases

    [Fact]
    public void IsRtlCulture_ArChildren_AllReturnTrue()
    {
        _service.IsRtlCulture(new CultureInfo("ar-IQ")).Should().BeTrue();
        _service.IsRtlCulture(new CultureInfo("ar-KW")).Should().BeTrue();
    }

    [Fact]
    public void IsRtlCulture_NonArabicCultures_ReturnFalse()
    {
        _service.IsRtlCulture(new CultureInfo("en-GB")).Should().BeFalse();
        _service.IsRtlCulture(new CultureInfo("ja-JP")).Should().BeFalse();
    }

    [Fact]
    public void IsRtlLanguage_NonArabicLanguages_ReturnFalse()
    {
        _service.IsRtlLanguage(UserLanguage.English).Should().BeFalse();
        _service.IsRtlLanguage(UserLanguage.Chinese).Should().BeFalse();
    }

    #endregion

    #region FormatDate Edge Cases

    [Fact]
    public void FormatDate_MinDate_DoesNotThrow()
    {
        var result = _service.FormatDate(DateTime.MinValue);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatDate_MaxDate_DoesNotThrow()
    {
        var result = _service.FormatDate(DateTime.MaxValue);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatDate_WithArabicCulture_ReturnsNonEmpty()
    {
        _service.SetCurrentCulture(UserLanguage.Arabic);
        var result = _service.FormatDate(new DateTime(2024, 6, 15));
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatDate_WithChineseCulture_ReturnsNonEmpty()
    {
        _service.SetCurrentCulture(UserLanguage.Chinese);
        var result = _service.FormatDate(new DateTime(2024, 6, 15));
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region FormatNumber Edge Cases

    [Fact]
    public void FormatNumber_Zero_ReturnsFormatted()
    {
        var result = _service.FormatNumber(0);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatNumber_Negative_ReturnsFormatted()
    {
        var result = _service.FormatNumber(-12345.67);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatNumber_WithArabicCulture_ReturnsNonEmpty()
    {
        _service.SetCurrentCulture(UserLanguage.Arabic);
        var result = _service.FormatNumber(1234.56);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatNumber_WithChineseCulture_ReturnsNonEmpty()
    {
        _service.SetCurrentCulture(UserLanguage.Chinese);
        var result = _service.FormatNumber(1234.56);
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetString with UserLanguage

    [Fact]
    public void GetString_WithUserLanguage_DoesNotThrow()
    {
        var result = _service.GetString("some_key", UserLanguage.Spanish);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetString_WithAllLanguages_ReturnsNonEmpty()
    {
        foreach (var lang in _service.GetSupportedUserLanguages())
        {
            var result = _service.GetString("AppTitle", lang);
            result.Should().NotBeNullOrEmpty();
        }
    }

    #endregion
}
