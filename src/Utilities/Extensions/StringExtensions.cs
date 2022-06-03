using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CloutCast
{
    public static class StringExtensions
    {
        public static string EncodeBase64(this string plainText) => 
            Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

        public static string DecodeBase64(this string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public static bool IsEmpty(this string source, params string[] removableChars)
        {
            if (source == null) return true;
            var cleaned = Strip(source, removableChars);

            return cleaned.Length == 0 || string.IsNullOrWhiteSpace(cleaned);
        }

        public static bool IsNotEmpty(this string source, params string[] removableChars) => !IsEmpty(source, removableChars);

        public static string MustEndWith(this string source, string postFix)
        {
            if (!source.IsEmpty() && !source.EndsWith(postFix)) source += postFix;
            return source;
        }

        public static string RemoveFromEnd(this string source, int amountToRemove=1) => 
            string.IsNullOrEmpty(source) ? source : source.Remove(source.Length - amountToRemove);

        public static string RemoveLineBreaks(this string str, string replacement = "<<newline>>") => 
            Regex.Replace(str.Trim(), @"\r\n?|\n", replacement);

        public static string Repeat(this string source, int repeat) => string.Concat(Enumerable.Repeat(source, repeat));

        public static string Strip(this string source, params string[] removableChars)
        {
            if (removableChars.None()) removableChars = new[] {" ", "\t", "\r", "\n"};

            var sb = new StringBuilder(source ?? "");
            foreach (var removable in removableChars)
                sb.Replace(removable, "");

            return sb.ToString();
        }

        public static string Truncate(this string source, int length) =>
            $"{source ?? string.Empty}".PadRight(length).Substring(0, length).Trim();
        
    }
}