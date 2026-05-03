using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization.Metadata;

namespace StoneStorySaveEditor.Services
{
    public static class SlimJsonConverter
    {
        public static object? ToObject(string slimJsonText)
        {
            return Parse(slimJsonText);
        }

        public static string ToPrettyJson(object? obj)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            return JsonSerializer.Serialize(obj, options);
        }

        public static string ToPrettyJson(string slimJsonText)
        {
            object? parsed = Parse(slimJsonText);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            return JsonSerializer.Serialize(parsed, options);
        }

        public static object? Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            int index = 0;
            object? value = ParseValue(text, ref index);
            index = SkipWhitespace(text, index);

            if (index != text.Length)
            {
                throw new Exception("Extra text after SlimJson parse at index " + index);
            }

            return value;
        }

        private static object? ParseValue(string text, ref int index)
        {
            index = SkipWhitespace(text, index);

            if (index >= text.Length)
            {
                return null;
            }

            char c = text[index];

            if (c == '{')
            {
                return ParseObject(text, ref index);
            }

            if (c == '[')
            {
                return ParseArray(text, ref index);
            }

            if (c == '"')
            {
                return ParseQuotedString(text, ref index);
            }

            return ParseBareValue(text, ref index);
        }

        private static Dictionary<string, object?> ParseObject(string text, ref int index)
        {
            Dictionary<string, object?> obj = new Dictionary<string, object?>();

            // skip {
            index++;

            while (true)
            {
                index = SkipWhitespace(text, index);

                if (index >= text.Length)
                {
                    throw new Exception("Unclosed object");
                }

                if (text[index] == '}')
                {
                    index++;
                    return obj;
                }

                string key = ParseKey(text, ref index);

                index = SkipWhitespace(text, index);

                if (index >= text.Length || text[index] != ':')
                {
                    throw new Exception("Expected ':' after key '" + key + "' at index " + index);
                }

                index++;

                object? value = ParseValue(text, ref index);
                obj[key] = value;

                index = SkipWhitespace(text, index);

                if (index < text.Length && text[index] == ',')
                {
                    index++;
                    continue;
                }

                if (index < text.Length && text[index] == '}')
                {
                    index++;
                    return obj;
                }

                throw new Exception("Expected ',' or '}' at index " + index);
            }
        }

        private static List<object?> ParseArray(string text, ref int index)
        {
            List<object?> arr = new List<object?>();

            // skip [
            index++;

            while (true)
            {
                index = SkipWhitespace(text, index);

                if (index >= text.Length)
                {
                    throw new Exception("Unclosed array");
                }

                if (text[index] == ']')
                {
                    index++;
                    return arr;
                }

                object? value = ParseValue(text, ref index);
                arr.Add(value);

                index = SkipWhitespace(text, index);

                if (index < text.Length && text[index] == ',')
                {
                    index++;
                    continue;
                }

                if (index < text.Length && text[index] == ']')
                {
                    index++;
                    return arr;
                }

                throw new Exception("Expected ',' or ']' at index " + index);
            }
        }

        private static string ParseKey(string text, ref int index)
        {
            index = SkipWhitespace(text, index);

            if (index < text.Length && text[index] == '"')
            {
                return ParseQuotedString(text, ref index);
            }

            int start = index;

            while (index < text.Length)
            {
                char c = text[index];

                if (c == ':' || c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    break;
                }

                index++;
            }

            return text.Substring(start, index - start);
        }

        private static string ParseQuotedString(string text, ref int index)
        {
            // skip opening quote
            index++;

            var chars = new List<char>();

            while (index < text.Length)
            {
                char c = text[index];

                if (c == '\\' && index + 1 < text.Length)
                {
                    char next = text[index + 1];

                    if (next == 'n')
                    {
                        chars.Add('\n');
                    }
                    else if (next == 't')
                    {
                        chars.Add('\t');
                    }
                    else if (next == 'r')
                    {
                        chars.Add('\r');
                    }
                    else
                    {
                        // SlimJson escapes things like \, \: \{ \}
                        chars.Add(next);
                    }

                    index += 2;
                    continue;
                }

                if (c == '"')
                {
                    index++;
                    return new string(chars.ToArray());
                }

                chars.Add(c);
                index++;
            }

            throw new Exception("Unclosed quoted string");
        }

        private static object? ParseBareValue(string text, ref int index)
        {
            int start = index;
            bool escaped = false;

            while (index < text.Length)
            {
                char c = text[index];

                if (escaped)
                {
                    escaped = false;
                    index++;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    index++;
                    continue;
                }

                if (c == ',' || c == '}' || c == ']')
                {
                    break;
                }

                index++;
            }

            string raw = text.Substring(start, index - start).Trim();
            raw = UnescapeSlim(raw);

            return ConvertScalar(raw);
        }

        private static object? ConvertScalar(string raw)
        {
            string value = raw.Trim();

            if (value.Length == 0)
            {
                return "";
            }

            if (value == "null")
            {
                return null;
            }

            if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (Regex.IsMatch(value, @"^-?\d+$"))
            {
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
                {
                    return longValue;
                }
            }

            // Float / scientific notation, like 3.814697E-05
            if (
                Regex.IsMatch(value, @"^-?(?:\d+\.\d*|\d*\.\d+)(?:[eE][+-]?\d+)?$") ||
                Regex.IsMatch(value, @"^-?\d+[eE][+-]?\d+$")
            )
            {
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
                {
                    return doubleValue;
                }
            }

            // Important: values like 4.27.2 stay strings.
            return value;
        }

        private static string UnescapeSlim(string value)
        {
            var chars = new List<char>();
            bool escaped = false;

            foreach (char c in value)
            {
                if (escaped)
                {
                    chars.Add(c);
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else
                {
                    chars.Add(c);
                }
            }

            if (escaped)
            {
                chars.Add('\\');
            }

            return new string(chars.ToArray());
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length)
            {
                char c = text[index];

                if (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\u2002')
                {
                    index++;
                    continue;
                }

                break;
            }

            return index;
        }
    }
}