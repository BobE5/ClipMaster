using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ClipMaster.Helpers
{
    public static class TextTransformHelper
    {
        // Case Transformations
        public static string ToUpperCase(string text) => text?.ToUpper() ?? string.Empty;
        
        public static string ToLowerCase(string text) => text?.ToLower() ?? string.Empty;
        
        public static string ToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
        }
        
        public static string ToSentenceCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            var result = new StringBuilder();
            bool capitalizeNext = true;

            foreach (char c in text.ToLower())
            {
                if (capitalizeNext && char.IsLetter(c))
                {
                    result.Append(char.ToUpper(c));
                    capitalizeNext = false;
                }
                else
                {
                    result.Append(c);
                }

                if (c == '.' || c == '!' || c == '?')
                {
                    capitalizeNext = true;
                }
            }

            return result.ToString();
        }

        public static string InvertCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            return new string(text.Select(c => 
                char.IsUpper(c) ? char.ToLower(c) : 
                char.IsLower(c) ? char.ToUpper(c) : c).ToArray());
        }

        public static string ToAlternatingCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            var result = new StringBuilder();
            bool upper = true;
            
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    result.Append(upper ? char.ToUpper(c) : char.ToLower(c));
                    upper = !upper;
                }
                else
                {
                    result.Append(c);
                }
            }
            
            return result.ToString();
        }

        // Whitespace Operations
        public static string RemoveLineBreaks(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Regex.Replace(text, @"[\r\n]+", " ");
        }

        public static string TrimSpaces(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        public static string RemoveAllSpaces(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Regex.Replace(text, @"\s+", "");
        }

        public static string RemoveBlankLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return string.Join(Environment.NewLine, lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        }

        public static string TrimEachLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return string.Join(Environment.NewLine, lines.Select(l => l.Trim()));
        }

        // Line Operations
        public static string SortLines(string text, bool ascending = true)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var sorted = ascending ? lines.OrderBy(l => l) : lines.OrderByDescending(l => l);
            return string.Join(Environment.NewLine, sorted);
        }

        public static string ReverseLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return string.Join(Environment.NewLine, lines.Reverse());
        }

        public static string RemoveDuplicateLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return string.Join(Environment.NewLine, lines.Distinct());
        }

        public static string AddLineNumbers(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var numbered = lines.Select((line, index) => $"{index + 1}. {line}");
            return string.Join(Environment.NewLine, numbered);
        }

        public static string RemoveLineNumbers(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Regex.Replace(text, @"^\d+[\.\)\]\:]\s*", "", RegexOptions.Multiline);
        }

        public static string ShuffleLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            var random = new Random();
            return string.Join(Environment.NewLine, lines.OrderBy(x => random.Next()));
        }

        // Extraction Operations
        public static string ExtractUrls(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var urlPattern = @"https?://[^\s<>""']+|www\.[^\s<>""']+";
            var matches = Regex.Matches(text, urlPattern, RegexOptions.IgnoreCase);
            return string.Join(Environment.NewLine, matches.Select(m => m.Value).Distinct());
        }

        public static string ExtractEmails(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
            var matches = Regex.Matches(text, emailPattern);
            return string.Join(Environment.NewLine, matches.Select(m => m.Value).Distinct());
        }

        public static string ExtractNumbers(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var numberPattern = @"-?\d+\.?\d*";
            var matches = Regex.Matches(text, numberPattern);
            return string.Join(Environment.NewLine, matches.Select(m => m.Value));
        }

        public static string ExtractPhoneNumbers(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var phonePattern = @"[\+]?[(]?[0-9]{1,3}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,4}[-\s\.]?[0-9]{1,9}";
            var matches = Regex.Matches(text, phonePattern);
            return string.Join(Environment.NewLine, matches.Select(m => m.Value.Trim()).Where(p => p.Length >= 7).Distinct());
        }

        // Encoding/Decoding
        public static string ToBase64(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        public static string FromBase64(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return string.Empty;
                return Encoding.UTF8.GetString(Convert.FromBase64String(text.Trim()));
            }
            catch
            {
                return "Invalid Base64 string";
            }
        }

        public static string UrlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Uri.EscapeDataString(text);
        }

        public static string UrlDecode(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Uri.UnescapeDataString(text);
        }

        public static string HtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return System.Net.WebUtility.HtmlEncode(text);
        }

        public static string HtmlDecode(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return System.Net.WebUtility.HtmlDecode(text);
        }

        // Special Operations
        public static string ReverseText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return new string(text.Reverse().ToArray());
        }

        public static string RemoveHtmlTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Regex.Replace(text, @"<[^>]+>", "");
        }

        public static string WrapLines(string text, int maxWidth = 80)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0) return text ?? string.Empty;
            
            var result = new StringBuilder();
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (line.Length <= maxWidth)
                {
                    result.AppendLine(line);
                }
                else
                {
                    var words = line.Split(' ');
                    var currentLine = new StringBuilder();

                    foreach (var word in words)
                    {
                        if (currentLine.Length + word.Length + 1 > maxWidth && currentLine.Length > 0)
                        {
                            result.AppendLine(currentLine.ToString().TrimEnd());
                            currentLine.Clear();
                        }
                        
                        if (currentLine.Length > 0) currentLine.Append(' ');
                        currentLine.Append(word);
                    }

                    if (currentLine.Length > 0)
                        result.AppendLine(currentLine.ToString());
                }
            }

            return result.ToString().TrimEnd();
        }

        public static string JsonPrettify(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return string.Empty;
                var obj = System.Text.Json.JsonDocument.Parse(text);
                return System.Text.Json.JsonSerializer.Serialize(obj, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return "Invalid JSON";
            }
        }

        public static string JsonMinify(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return string.Empty;
                var obj = System.Text.Json.JsonDocument.Parse(text);
                return System.Text.Json.JsonSerializer.Serialize(obj);
            }
            catch
            {
                return "Invalid JSON";
            }
        }

        // Statistics
        public static (int chars, int words, int lines, int sentences) GetStatistics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return (0, 0, 0, 0);

            int chars = text.Length;
            int words = text.Split(new[] { ' ', '\t', '\n', '\r' }, 
                StringSplitOptions.RemoveEmptyEntries).Length;
            int lines = text.Split('\n').Length;
            int sentences = Regex.Matches(text, @"[.!?]+").Count;

            return (chars, words, lines, sentences);
        }
    }
}
