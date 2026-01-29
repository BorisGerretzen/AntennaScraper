using AntennaScraper.Lib.Helpers;

namespace AntennaScraper.Tests.Lib.Tests.Helpers;

public class FrequencyHelpersTests
{
    [Theory]
    [InlineData("900 MHz", 900_000_000)]
    [InlineData("900MHz", 900_000_000)]
    [InlineData("900mhz", 900_000_000)]
    [InlineData("900 MHZ", 900_000_000)]
    [InlineData("  900 MHz  ", 900_000_000)]
    public void ParseFrequency_SimpleMhzValues_ReturnsCorrectHertz(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1800 MHz", 1_800_000_000)]
    [InlineData("2100 MHz", 2_100_000_000)]
    [InlineData("2600 MHz", 2_600_000_000)]
    [InlineData("3500 MHz", 3_500_000_000)]
    public void ParseFrequency_CommonBandFrequencies_ReturnsCorrectHertz(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("925.5 MHz", 925_500_000)]
    [InlineData("1805.2 MHz", 1_805_200_000)]
    [InlineData("2110.75 MHz", 2_110_750_000)]
    public void ParseFrequency_DecimalMhzValues_ReturnsCorrectHertz(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("758-768 MHz", 763_000_000)] // Average of 758 and 768
    [InlineData("791-801 MHz", 796_000_000)] // Average of 791 and 801
    [InlineData("925-935 MHz", 930_000_000)] // Average of 925 and 935
    public void ParseFrequency_FrequencyRange_ReturnsAverageInHertz(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1805.5-1825.5 MHz", 1_815_500_000)] // Average of 1805.5 and 1825.5
    public void ParseFrequency_DecimalRange_ReturnsAverageInHertz(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("3.5 MHz", 3_500_000_000)] // Interpreted as 3.5 GHz
    [InlineData("2.6 MHz", 2_600_000_000)] // Interpreted as 2.6 GHz
    [InlineData("26 MHz", 26_000_000_000)] // Interpreted as 26 GHz
    public void ParseFrequency_ValueBelow100_ConvertsAsGhz(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("100 MHz", 100_000_000)] // Exactly 100, NOT converted
    [InlineData("101 MHz", 101_000_000)] // Above 100, NOT converted
    public void ParseFrequency_ValueAtOrAbove100_DoesNotConvert(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1.800 MHz", 1_800_000_000)] // Dot as thousands separator (3 digits after)
    [InlineData("2.100 MHz", 2_100_000_000)]
    [InlineData("2.600 MHz", 2_600_000_000)]
    public void ParseFrequency_DotAsThousandsSeparator_ParsesCorrectly(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1.80 MHz", 1_800_000_000)] // Only 2 digits after dot - decimal, but < 100 so GHz conversion
    [InlineData("1.8 MHz", 1_800_000_000)] // 1 digit after dot - decimal, but < 100 so GHz conversion
    public void ParseFrequency_DotAsDecimal_WithGhzConversion(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("900")]
    [InlineData("900 Hz")]
    [InlineData("900 GHz")]
    [InlineData("900 kHz")]
    [InlineData("invalid")]
    [InlineData("")]
    public void ParseFrequency_MissingOrWrongUnit_ThrowsFormatException(string input)
    {
        // Act
        var act = () => FrequencyHelpers.ParseFrequency(input);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*MHz*");
    }

    [Fact]
    public void ParseFrequency_InvalidRangeFormat_ThrowsFormatException()
    {
        // Arrange
        var input = "758-768-800 MHz"; // Three values instead of two

        // Act
        var act = () => FrequencyHelpers.ParseFrequency(input);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*range*");
    }

    [Theory]
    [InlineData("abc MHz")]
    [InlineData("12.34.56 MHz")]
    public void ParseFrequency_InvalidNumber_ThrowsFormatException(string input)
    {
        // Act
        var act = () => FrequencyHelpers.ParseFrequency(input);

        // Assert
        act.Should().Throw<FormatException>();
    }


    [Fact]
    public void ParseFrequency_WhitespaceInRange_ParsesCorrectly()
    {
        // Arrange
        var input = "758 - 768 MHz";

        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(763_000_000);
    }

    [Fact]
    public void ParseFrequency_MultipleSpaces_ParsesCorrectly()
    {
        // Arrange
        var input = "  900    MHz  ";

        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(900_000_000);
    }

    [Theory]
    [InlineData("450 MHz", 450_000_000)] // PAMR band
    [InlineData("700 MHz", 700_000_000)] // Band 28
    [InlineData("800 MHz", 800_000_000)] // Band 20
    public void ParseFrequency_LowerBandFrequencies_ReturnsCorrectHertz(string input, long expected)
    {
        // Act
        var result = FrequencyHelpers.ParseFrequency(input);

        // Assert
        result.Should().Be(expected);
    }
}