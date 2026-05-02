using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace StoneStorySaveEditor.Services
{
    public static class JsonToSlimJsonConverter
    {
        public static string FromPrettyJson(string jsonText)
        {
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            return WriteElement(doc.RootElement);
        }

        private static string WriteElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return WriteObject(element);

                case JsonValueKind.Array:
                    return WriteArray(element);

                case JsonValueKind.String:
                    return WriteSlimString(element.GetString() ?? "");

                case JsonValueKind.Number:
                    return element.GetRawText();

                case JsonValueKind.True:
                    return "True";

                case JsonValueKind.False:
                    return "False";

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return "null";

                default:
                    return "";
            }
        }

        private static string WriteObject(JsonElement obj)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            bool first = true;

            foreach (JsonProperty prop in obj.EnumerateObject())
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;

                // SlimJson keys are normally unquoted.
                sb.Append(prop.Name);
                sb.Append(':');
                sb.Append(WriteElement(prop.Value));
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static string WriteArray(JsonElement arr)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');

            bool first = true;

            foreach (JsonElement item in arr.EnumerateArray())
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(WriteElement(item));
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteSlimString(string value)
        {
            if (NeedsQuotes(value))
            {
                return "\"" + EscapeQuotedString(value) + "\"";
            }

            return EscapeBareString(value);
        }

        private static bool NeedsQuotes(string value)
        {
            if (value.Length == 0)
            {
                return true;
            }

            if (value[0] == ' ' || value[value.Length - 1] == ' ')
            {
                return true;
            }

            // If it starts with object/array syntax but is meant to be a string,
            // force quotes.
            if (value[0] == '{' || value[0] == '[')
            {
                return true;
            }

            // Mimic SlimJson's behavior: quote strings that would break parsing.
            if (value.Contains(",") ||
                value.Contains("{") ||
                value.Contains("}") ||
                value.Contains("[") ||
                value.Contains("]") ||
                value.Contains("\n") ||
                value.Contains("\r") ||
                value.Contains("\""))
            {
                return true;
            }

            return false;
        }

        private static string EscapeQuotedString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string EscapeBareString(string value)
        {
            // Bare SlimJson strings usually do not need escaping.
            // Keep this conservative.
            return value;
        }
    }
}