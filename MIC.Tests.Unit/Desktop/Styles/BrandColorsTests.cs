using FluentAssertions;
using MIC.Desktop.Avalonia.Styles;
using Xunit;

namespace MIC.Tests.Unit.Desktop.Styles;

/// <summary>
/// Tests for <see cref="BrandColors"/> static class.
/// Validates color constants and severity helper methods.
/// </summary>
public class BrandColorsTests
{
    #region Color Constants Validation

    [Theory]
    [InlineData(nameof(BrandColors.Primary), "#1a237e")]
    [InlineData(nameof(BrandColors.PrimaryDark), "#0B0C10")]
    [InlineData(nameof(BrandColors.PrimaryLight), "#0D1117")]
    [InlineData(nameof(BrandColors.AccentCyan), "#00E5FF")]
    [InlineData(nameof(BrandColors.AccentGold), "#FFC107")]
    [InlineData(nameof(BrandColors.AccentMagenta), "#BF40FF")]
    [InlineData(nameof(BrandColors.AccentGreen), "#39FF14")]
    [InlineData(nameof(BrandColors.Success), "#39FF14")]
    [InlineData(nameof(BrandColors.Warning), "#FF6B00")]
    [InlineData(nameof(BrandColors.Error), "#FF0055")]
    [InlineData(nameof(BrandColors.Info), "#00E5FF")]
    [InlineData(nameof(BrandColors.TextPrimary), "#FFFFFF")]
    [InlineData(nameof(BrandColors.TextSecondary), "#B0BEC5")]
    [InlineData(nameof(BrandColors.TextTertiary), "#607D8B")]
    [InlineData(nameof(BrandColors.TextDisabled), "#455A64")]
    public void ColorConstant_HasExpectedValue(string fieldName, string expectedValue)
    {
        var field = typeof(BrandColors).GetField(fieldName);
        field.Should().NotBeNull();
        var value = field!.GetValue(null) as string;
        value.Should().Be(expectedValue);
    }

    [Fact]
    public void Surface_IsValidHexColor()
    {
        BrandColors.Surface.Should().NotBeNullOrWhiteSpace();
        BrandColors.Surface.Should().StartWith("#");
    }

    [Fact]
    public void SurfaceBorder_IsValidHexColor()
    {
        BrandColors.SurfaceBorder.Should().NotBeNullOrWhiteSpace();
        BrandColors.SurfaceBorder.Should().StartWith("#");
    }

    [Fact]
    public void Divider_IsValidHexColor()
    {
        BrandColors.Divider.Should().NotBeNullOrWhiteSpace();
        BrandColors.Divider.Should().StartWith("#");
    }

    [Fact]
    public void GlowCyan_IsValidHexColor()
    {
        BrandColors.GlowCyan.Should().NotBeNullOrWhiteSpace();
        BrandColors.GlowCyan.Should().StartWith("#");
    }

    [Fact]
    public void GlowGreen_IsValidHexColor()
    {
        BrandColors.GlowGreen.Should().NotBeNullOrWhiteSpace();
        BrandColors.GlowGreen.Should().StartWith("#");
    }

    [Fact]
    public void GlowRed_IsValidHexColor()
    {
        BrandColors.GlowRed.Should().NotBeNullOrWhiteSpace();
        BrandColors.GlowRed.Should().StartWith("#");
    }

    [Fact]
    public void GlowGold_IsValidHexColor()
    {
        BrandColors.GlowGold.Should().NotBeNullOrWhiteSpace();
        BrandColors.GlowGold.Should().StartWith("#");
    }

    #endregion

    #region GetSeverityColor Tests

    [Theory]
    [InlineData("critical", "#FF0055")]
    [InlineData("Critical", "#FF0055")]
    [InlineData("CRITICAL", "#FF0055")]
    [InlineData("high", "#FF0055")]
    [InlineData("High", "#FF0055")]
    [InlineData("HIGH", "#FF0055")]
    [InlineData("warning", "#FF6B00")]
    [InlineData("Warning", "#FF6B00")]
    [InlineData("WARNING", "#FF6B00")]
    [InlineData("medium", "#FF6B00")]
    [InlineData("Medium", "#FF6B00")]
    [InlineData("MEDIUM", "#FF6B00")]
    [InlineData("info", "#00E5FF")]
    [InlineData("Info", "#00E5FF")]
    [InlineData("INFO", "#00E5FF")]
    [InlineData("low", "#00E5FF")]
    [InlineData("Low", "#00E5FF")]
    [InlineData("LOW", "#00E5FF")]
    public void GetSeverityColor_KnownSeverity_ReturnsExpectedColor(string severity, string expectedColor)
    {
        BrandColors.GetSeverityColor(severity).Should().Be(expectedColor);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("none")]
    [InlineData("something")]
    public void GetSeverityColor_UnknownSeverity_ReturnsTextSecondary(string severity)
    {
        BrandColors.GetSeverityColor(severity).Should().Be(BrandColors.TextSecondary);
    }

    [Fact]
    public void GetSeverityColor_NullSeverity_ReturnsTextSecondary()
    {
        BrandColors.GetSeverityColor(null!).Should().Be(BrandColors.TextSecondary);
    }

    #endregion

    #region GetSeverityGlow Tests

    [Theory]
    [InlineData("critical", "#40FF0055")]
    [InlineData("Critical", "#40FF0055")]
    [InlineData("high", "#40FF0055")]
    [InlineData("High", "#40FF0055")]
    public void GetSeverityGlow_CriticalOrHigh_ReturnsGlowRed(string severity, string expected)
    {
        BrandColors.GetSeverityGlow(severity).Should().Be(expected);
    }

    [Theory]
    [InlineData("warning", "#40FFC107")]
    [InlineData("Warning", "#40FFC107")]
    [InlineData("medium", "#40FFC107")]
    [InlineData("Medium", "#40FFC107")]
    public void GetSeverityGlow_WarningOrMedium_ReturnsGlowGold(string severity, string expected)
    {
        BrandColors.GetSeverityGlow(severity).Should().Be(expected);
    }

    [Theory]
    [InlineData("info", "#4000E5FF")]
    [InlineData("Info", "#4000E5FF")]
    [InlineData("low", "#4000E5FF")]
    [InlineData("Low", "#4000E5FF")]
    public void GetSeverityGlow_InfoOrLow_ReturnsGlowCyan(string severity, string expected)
    {
        BrandColors.GetSeverityGlow(severity).Should().Be(expected);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("none")]
    public void GetSeverityGlow_UnknownSeverity_ReturnsTransparent(string severity)
    {
        BrandColors.GetSeverityGlow(severity).Should().Be("#00000000");
    }

    [Fact]
    public void GetSeverityGlow_NullSeverity_ReturnsTransparent()
    {
        BrandColors.GetSeverityGlow(null!).Should().Be("#00000000");
    }

    #endregion

    #region Color Consistency Tests

    [Fact]
    public void AccentGreen_EqualsSucess()
    {
        BrandColors.AccentGreen.Should().Be(BrandColors.Success);
    }

    [Fact]
    public void AccentCyan_EqualsInfo()
    {
        BrandColors.AccentCyan.Should().Be(BrandColors.Info);
    }

    [Fact]
    public void AllConstColors_AreValidHexFormat()
    {
        var colorFields = typeof(BrandColors)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.IsLiteral);

        foreach (var field in colorFields)
        {
            var value = (string)field.GetValue(null)!;
            value.Should().StartWith("#", $"{field.Name} should be a hex color");
            value.Length.Should().BeOneOf(new[] { 7, 9 }, $"{field.Name} should be #RRGGBB or #AARRGGBB");
        }
    }

    #endregion
}
