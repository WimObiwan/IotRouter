using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace IotRouter.Util;

public static class StringPlaceholderReplacer
{
    public static string Replace(string value, ParsedData parsedData)
    {
        return
            Regex.Replace(value, @"{([^}]+)}",
                m =>
                {
                    if (m.Groups[1].Value == "DevEUI")
                        return parsedData.DevEUI;
                    var replaceValue = parsedData.KeyValues.FirstOrDefault(kv => kv.Key == m.Groups[1].Value)?.Value;
                    if (replaceValue == null)
                        return "Unknown";
                    return GetValue(replaceValue);
                });
    }
    
    private static string GetValue(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}