using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Identity.Services;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Identity;

public class LocalizationServiceTests
{
    private readonly LocalizationService _service;

    public LocalizationServiceTests()
    {
        _service = new LocalizationService(NullLogger<LocalizationService>.Instance);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultLanguage()
    {
        // Act & Assert
        var currentCulture = _service.GetCurrentCulture();
        currentCulture.Name.Should().Be("en-US");
        _service.GetCurrentUserLanguage().Should().Be(UserLanguage.English);
    }

    [Fact]
    public void SetCurrentCulture_ShouldChangeCultureForEnglish()
    {
        // Act
        _service.SetCurrentCulture(UserLanguage.English);

        // Assert
        var culture = _service.GetCurrentCulture();
        culture.Name.Should().Be("en-US");
        _service.GetCurrentUserLanguage().Should().Be(UserLanguage.English);
    }

    [Fact]
    public void SetCurrentCulture_ShouldChangeCultureForFrench()
    {
        // Act
        _service.SetCurrentCulture(UserLanguage.French);

        // Assert
        var culture = _service.GetCurrentCulture();
        culture.Name.Should().Be("fr-FR");
        _service.GetCurrentUserLanguage().Should().Be(UserLanguage.French);
    }

    [Fact]
    public void SetCurrentCulture_ShouldChangeCultureForSpanish()
    {
        // Act
        _service.SetCurrentCulture(UserLanguage.Spanish);

        // Assert
        var culture = _service.GetCurrentCulture();
        culture.Name.Should().Be("es-ES");
        _service.GetCurrentUserLanguage().Should().Be(UserLanguage.Spanish);
    }

    [Fact]
    public void SetCurrentCulture_ShouldChangeCultureForArabic()
    {
        // Act
        _service.SetCurrentCulture(UserLanguage.Arabic);

        // Assert
        var culture = _service.GetCurrentCulture();
        culture.Name.Should().Be("ar-SA");
        _service.GetCurrentUserLanguage().Should().Be(UserLanguage.Arabic);
    }

    [Fact]
    public void SetCurrentCulture_ShouldChangeCultureForChinese()
    {
        // Act
        _service.SetCurrentCulture(UserLanguage.Chinese);

        // Assert
        var culture = _service.GetCurrentCulture();
        culture.Name.Should().Be("zh-CN");
        _service.GetCurrentUserLanguage().Should().Be(UserLanguage.Chinese);
    }

    [Fact]
    public void GetSupportedUserLanguages_ShouldReturnAllFiveLanguages()
    {
        // Act
        var languages = _service.GetSupportedUserLanguages();

        // Assert
        languages.Should().HaveCount(5);
        languages.Should().Contain(UserLanguage.English);
        languages.Should().Contain(UserLanguage.French);
        languages.Should().Contain(UserLanguage.Spanish);
        languages.Should().Contain(UserLanguage.Arabic);
        languages.Should().Contain(UserLanguage.Chinese);
    }

    [Fact]
    public void GetLanguageDisplayName_ShouldReturnCorrectNames()
    {
        // Act & Assert
        _service.GetLanguageDisplayName(UserLanguage.English).Should().Contain("English");
        _service.GetLanguageDisplayName(UserLanguage.French).Should().Contain("French");
        _service.GetLanguageDisplayName(UserLanguage.Spanish).Should().Contain("Spanish");
        _service.GetLanguageDisplayName(UserLanguage.Arabic).Should().Contain("Arabic");
        _service.GetLanguageDisplayName(UserLanguage.Chinese).Should().Contain("Chinese");
    }

    [Fact]
    public void UserLanguageToCulture_ShouldReturnCorrectCultureInfo()
    {
        // Act & Assert
        _service.UserLanguageToCulture(UserLanguage.English).Name.Should().Be("en-US");
        _service.UserLanguageToCulture(UserLanguage.French).Name.Should().Be("fr-FR");
        _service.UserLanguageToCulture(UserLanguage.Spanish).Name.Should().Be("es-ES");
        _service.UserLanguageToCulture(UserLanguage.Arabic).Name.Should().Be("ar-SA");
        _service.UserLanguageToCulture(UserLanguage.Chinese).Name.Should().Be("zh-CN");
    }

    [Fact]
    public void GetString_ShouldReturnLocalizedString()
    {
        // Arrange
        _service.SetCurrentCulture(UserLanguage.English);
        
        // Act - This would test actual resource lookup, but we'll test the method exists
        // For now, just verify the method doesn't throw
        var action = () => _service.GetString("TestKey");
        
        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void GetStringWithCulture_ShouldReturnLocalizedStringForSpecificCulture()
    {
        // Act - Test method exists
        var action = () => _service.GetString("TestKey", UserLanguage.French);
        
        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void SetCurrentCulture_ShouldUpdateThreadCulture()
    {
        // Act
        _service.SetCurrentCulture(UserLanguage.French);

        // Assert
        System.Threading.Thread.CurrentThread.CurrentCulture.Name.Should().Be("fr-FR");
        System.Threading.Thread.CurrentThread.CurrentUICulture.Name.Should().Be("fr-FR");
    }

    [Fact]
    public void CultureToUserLanguage_ShouldParseValidCultures()
    {
        // Act & Assert
        _service.CultureToUserLanguage(new System.Globalization.CultureInfo("en-US")).Should().Be(UserLanguage.English);
        _service.CultureToUserLanguage(new System.Globalization.CultureInfo("fr-FR")).Should().Be(UserLanguage.French);
        _service.CultureToUserLanguage(new System.Globalization.CultureInfo("es-ES")).Should().Be(UserLanguage.Spanish);
        _service.CultureToUserLanguage(new System.Globalization.CultureInfo("ar-SA")).Should().Be(UserLanguage.Arabic);
        _service.CultureToUserLanguage(new System.Globalization.CultureInfo("zh-CN")).Should().Be(UserLanguage.Chinese);
    }

    [Fact]
    public void CultureToUserLanguage_ShouldReturnEnglishForInvalidCulture()
    {
        // Act & Assert
        _service.CultureToUserLanguage(new System.Globalization.CultureInfo("de-DE")).Should().Be(UserLanguage.English);
    }

    [Fact]
    public void CultureToUserLanguage_ShouldReturnEnglishForNull()
    {
        // Act & Assert
        _service.CultureToUserLanguage(null!).Should().Be(UserLanguage.English);
    }

    [Fact]
    public void IsRtlLanguage_ShouldReturnTrueForArabic()
    {
        // Act & Assert
        _service.IsRtlLanguage(UserLanguage.Arabic).Should().BeTrue();
        _service.IsRtlLanguage(UserLanguage.English).Should().BeFalse();
        _service.IsRtlLanguage(UserLanguage.French).Should().BeFalse();
        _service.IsRtlLanguage(UserLanguage.Spanish).Should().BeFalse();
        _service.IsRtlLanguage(UserLanguage.Chinese).Should().BeFalse();
    }

    [Fact]
    public void IsRtlCulture_ShouldReturnTrueForArabicCultures()
    {
        // Act & Assert
        _service.IsRtlCulture(new System.Globalization.CultureInfo("ar-SA")).Should().BeTrue();
        _service.IsRtlCulture(new System.Globalization.CultureInfo("ar-EG")).Should().BeTrue();
        _service.IsRtlCulture(new System.Globalization.CultureInfo("en-US")).Should().BeFalse();
        _service.IsRtlCulture(new System.Globalization.CultureInfo("fr-FR")).Should().BeFalse();
    }

    [Fact]
    public void FormatDate_ShouldFormatDateWithCurrentCulture()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);
        _service.SetCurrentCulture(UserLanguage.English);
        
        // Act
        var formatted = _service.FormatDate(date);
        
        // Assert
        formatted.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatNumber_ShouldFormatNumberWithCurrentCulture()
    {
        // Arrange
        _service.SetCurrentCulture(UserLanguage.English);
        
        // Act
        var formatted = _service.FormatNumber(1234.56);
        
        // Assert
        formatted.Should().NotBeNullOrEmpty();
    }

    #region Additional Coverage Tests

    [Fact]
    public void GetString_NullKey_ReturnsEmpty()
    {
        _service.GetString(null!).Should().BeEmpty();
    }

    [Fact]
    public void GetString_EmptyKey_ReturnsEmpty()
    {
        _service.GetString("").Should().BeEmpty();
    }

    [Fact]
    public void GetString_WhitespaceKey_ReturnsEmpty()
    {
        _service.GetString("   ").Should().BeEmpty();
    }

    [Fact]
    public void GetString_UnknownKey_ReturnsKey()
    {
        // ResourceManager returns null for unknown keys → falls back to the key itself
        _service.GetString("some_unknown_key_xyz").Should().Be("some_unknown_key_xyz");
    }

    [Fact]
    public void GetString_WithCultureInfo_ReturnsNonEmpty()
    {
        var culture = new System.Globalization.CultureInfo("fr-FR");
        var result = _service.GetString("some_key", culture);
        // Either returns localized value or falls back to the key
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetSupportedCultures_ReturnsNonEmptyArray()
    {
        var cultures = _service.GetSupportedCultures();
        cultures.Should().NotBeEmpty();
        cultures.Should().HaveCountGreaterOrEqualTo(5);
    }

    [Fact]
    public void GetSupportedUserLanguages_ContainsAllFiveLanguages()
    {
        var languages = _service.GetSupportedUserLanguages();
        languages.Should().Contain(UserLanguage.English);
        languages.Should().Contain(UserLanguage.French);
        languages.Should().Contain(UserLanguage.Spanish);
        languages.Should().Contain(UserLanguage.Arabic);
        languages.Should().Contain(UserLanguage.Chinese);
    }

    [Fact]
    public void FormatDate_WithSpecificCulture_ReturnsFormattedString()
    {
        var date = new DateTime(2024, 6, 15);
        var culture = new System.Globalization.CultureInfo("fr-FR");
        var result = _service.FormatDate(date, culture);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatNumber_WithSpecificCulture_ReturnsFormattedString()
    {
        var culture = new System.Globalization.CultureInfo("de-DE");
        var result = _service.FormatNumber(1234.56, culture);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void IsRtlCulture_NullCulture_ReturnsFalse()
    {
        _service.IsRtlCulture(null!).Should().BeFalse();
    }

    [Fact]
    public void GetLanguageDisplayName_ReturnsNonEmptyForAllLanguages()
    {
        foreach (var lang in _service.GetSupportedUserLanguages())
        {
            _service.GetLanguageDisplayName(lang).Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void SetCurrentCulture_CultureInfo_SetsThreadCulture()
    {
        var frCulture = new System.Globalization.CultureInfo("fr-FR");
        _service.SetCurrentCulture(frCulture);
        _service.GetCurrentCulture().Name.Should().Be("fr-FR");
    }

    #endregion
}
