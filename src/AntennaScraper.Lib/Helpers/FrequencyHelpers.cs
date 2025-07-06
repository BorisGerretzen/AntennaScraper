using System.Globalization;

namespace AntennaScraper.Lib.Helpers;

public class FrequencyHelpers
{
    public static long ParseFrequency(string frequencyString)
    {
        frequencyString = frequencyString.ToLower().Trim().Replace(" ", "");
        if(!frequencyString.Contains("mhz")) throw new FormatException("Only MHz frequencies are supported in this method.");
        frequencyString = frequencyString.Replace("mhz", "");

        double parsedFrequency;
        if (frequencyString.Contains('-'))
        {
            var split = frequencyString.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (split.Length != 2) throw new FormatException("Invalid frequency range format.");
            
            var freq1 = NormalizeAndParse(split[0]);
            var freq2 = NormalizeAndParse(split[1]);
            parsedFrequency = (freq1 + freq2) / 2;
        }
        else
        {
            parsedFrequency = NormalizeAndParse(frequencyString);
        }
        
        if(parsedFrequency < 100) // Likely should be GHz instead of MHz
        {
            parsedFrequency *= 1000;
        }
        
        return (long)(parsedFrequency * 1_000_000);
    }

    private static double NormalizeAndParse(string numberString)
    {
        if (numberString.Contains('.'))
        {
            var lastDotIndex = numberString.LastIndexOf('.');
            
            // If there are exactly three digits after the last dot, assume it's a thousands separator
            var numDigitsAfterDot = numberString.Length - lastDotIndex - 1;
            if (numDigitsAfterDot == 3)
            {
                numberString = numberString.Remove(lastDotIndex, 1);
            }
        }
        
        return double.Parse(numberString, CultureInfo.InvariantCulture);
    }
}