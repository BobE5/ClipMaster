using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClipMaster.Models;

namespace ClipMaster.Services
{
    public class RegexLibraryService
    {
        private static readonly string LibraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipMaster", "regex_library.json");

        private List<RegexPattern> _patterns;

        public RegexLibraryService()
        {
            _patterns = Load();
            if (_patterns.Count == 0)
            {
                InitializeDefaultPatterns();
            }
        }

        public IReadOnlyList<RegexPattern> Patterns => _patterns.AsReadOnly();

        public void AddPattern(RegexPattern pattern)
        {
            _patterns.Add(pattern);
            Save();
        }

        public void UpdatePattern(RegexPattern pattern)
        {
            var index = _patterns.FindIndex(p => p.Id == pattern.Id);
            if (index >= 0)
            {
                _patterns[index] = pattern;
                Save();
            }
        }

        public void DeletePattern(string id)
        {
            _patterns.RemoveAll(p => p.Id == id);
            Save();
        }

        public RegexPattern? GetPattern(string id)
        {
            return _patterns.FirstOrDefault(p => p.Id == id);
        }

        public string ApplyPattern(string input, RegexPattern pattern)
        {
            try
            {
                var options = RegexOptions.None;
                if (!pattern.IsCaseSensitive)
                    options |= RegexOptions.IgnoreCase;
                if (pattern.IsMultiline)
                    options |= RegexOptions.Multiline;

                var regex = new Regex(pattern.Pattern, options);

                string result;
                if (pattern.IsGlobal)
                {
                    result = regex.Replace(input, pattern.Replacement);
                }
                else
                {
                    result = regex.Replace(input, pattern.Replacement, 1);
                }

                // Update usage stats
                pattern.LastUsedAt = DateTime.Now;
                pattern.UseCount++;
                UpdatePattern(pattern);

                return result;
            }
            catch (Exception ex)
            {
                return $"Regex Error: {ex.Message}";
            }
        }

        public string ApplyCustomRegex(string input, string pattern, string replacement,
            bool isGlobal = true, bool isCaseSensitive = true, bool isMultiline = false)
        {
            try
            {
                var options = RegexOptions.None;
                if (!isCaseSensitive)
                    options |= RegexOptions.IgnoreCase;
                if (isMultiline)
                    options |= RegexOptions.Multiline;

                var regex = new Regex(pattern, options);

                if (isGlobal)
                {
                    return regex.Replace(input, replacement);
                }
                else
                {
                    return regex.Replace(input, replacement, 1);
                }
            }
            catch (Exception ex)
            {
                return $"Regex Error: {ex.Message}";
            }
        }

        public (bool isValid, string error) ValidatePattern(string pattern)
        {
            try
            {
                _ = new Regex(pattern);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public List<Match> GetMatches(string input, string pattern, bool isCaseSensitive = true, bool isMultiline = false)
        {
            try
            {
                var options = RegexOptions.None;
                if (!isCaseSensitive)
                    options |= RegexOptions.IgnoreCase;
                if (isMultiline)
                    options |= RegexOptions.Multiline;

                var regex = new Regex(pattern, options);
                return regex.Matches(input).Cast<Match>().ToList();
            }
            catch
            {
                return new List<Match>();
            }
        }

        private void InitializeDefaultPatterns()
        {
            _patterns = new List<RegexPattern>
            {
                new RegexPattern
                {
                    Name = "Remove HTML Tags",
                    Pattern = @"<[^>]+>",
                    Replacement = "",
                    Description = "Strip all HTML tags from text"
                },
                new RegexPattern
                {
                    Name = "Normalize Whitespace",
                    Pattern = @"\s+",
                    Replacement = " ",
                    Description = "Replace multiple spaces/tabs/newlines with single space"
                },
                new RegexPattern
                {
                    Name = "Remove Empty Lines",
                    Pattern = @"^\s*$\n?",
                    Replacement = "",
                    Description = "Remove blank lines",
                    IsMultiline = true
                },
                new RegexPattern
                {
                    Name = "Extract Email Addresses",
                    Pattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
                    Replacement = "$0\n",
                    Description = "Extract all email addresses (one per line)"
                },
                new RegexPattern
                {
                    Name = "Convert to Snake Case",
                    Pattern = @"([a-z])([A-Z])",
                    Replacement = "$1_$2",
                    Description = "Convert camelCase to snake_case"
                },
                new RegexPattern
                {
                    Name = "Remove Comments (C-style)",
                    Pattern = @"//.*?$|/\*.*?\*/",
                    Replacement = "",
                    Description = "Remove C/C++/Java style comments",
                    IsMultiline = true
                },
                new RegexPattern
                {
                    Name = "Format Phone Numbers",
                    Pattern = @"(\d{3})(\d{3})(\d{4})",
                    Replacement = "($1) $2-$3",
                    Description = "Format 10-digit phone numbers as (xxx) xxx-xxxx"
                },
                new RegexPattern
                {
                    Name = "Remove Leading Zeros",
                    Pattern = @"\b0+(\d+)",
                    Replacement = "$1",
                    Description = "Remove leading zeros from numbers"
                },
                new RegexPattern
                {
                    Name = "Trim Each Line",
                    Pattern = @"^[ \t]+|[ \t]+$",
                    Replacement = "",
                    Description = "Remove leading/trailing whitespace from each line",
                    IsMultiline = true
                },
                new RegexPattern
                {
                    Name = "Double to Single Quotes",
                    Pattern = "\"([^\"]*)\"",
                    Replacement = "'$1'",
                    Description = "Convert double quotes to single quotes"
                },
                new RegexPattern
                {
                    Name = "Add Line Numbers",
                    Pattern = @"^(.*)$",
                    Replacement = "{{LINE}} $1",
                    Description = "Prefix each line with line number (use with counter)",
                    IsMultiline = true
                },
                new RegexPattern
                {
                    Name = "Extract URLs",
                    Pattern = @"https?://[^\s<>""']+",
                    Replacement = "$0\n",
                    Description = "Extract all URLs (one per line)"
                }
            };
            Save();
        }

        private List<RegexPattern> Load()
        {
            try
            {
                if (File.Exists(LibraryPath))
                {
                    var json = File.ReadAllText(LibraryPath);
                    return JsonSerializer.Deserialize<List<RegexPattern>>(json) ?? new List<RegexPattern>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading regex library: {ex.Message}");
            }
            return new List<RegexPattern>();
        }

        private void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(LibraryPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_patterns, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(LibraryPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving regex library: {ex.Message}");
            }
        }
    }
}
