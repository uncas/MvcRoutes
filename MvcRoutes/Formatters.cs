using System;
using System.Collections.Specialized;
using System.Linq;

namespace MvcRoutes
{
    /// <remarks>http://haacked.com/archive/2005/09/23/splitting-pascalcamel-cased-strings.aspx</remarks>
    public static class Formatters
    {
        public static string SplitUpperCaseToString(this string source)
        {
            string[] parts = SplitUpperCase(source);
            if (parts.Length == 0)
                return string.Empty;
            if (parts.Length == 1)
                return parts[0];
            return parts[0] + " " + string.Join(" ", parts.Skip(1).Select(x => x.ToLower()));
        }

        public static string[] SplitUpperCase(this string source)
        {
            if (source == null)
            {
                return new string[] {}; //Return empty array.
            }
            if (source.Length == 0)
            {
                return new[] {""};
            }

            var words = new StringCollection();
            int wordStartIndex = 0;

            char[] letters = source.ToCharArray();
            char previousChar = char.MinValue;

            // Skip the first letter. we don't care what case it is.
            for (int i = 1; i < letters.Length; i++)
            {
                if (char.IsUpper(letters[i]) && !char.IsWhiteSpace(previousChar))
                {
                    //Grab everything before the current character.
                    words.Add(new String(letters, wordStartIndex, i - wordStartIndex));
                    wordStartIndex = i;
                }
                previousChar = letters[i];
            }

            //We need to have the last word.
            words.Add(new String(letters, wordStartIndex,
                                 letters.Length - wordStartIndex));

            var wordArray = new string[words.Count];
            words.CopyTo(wordArray, 0);
            return wordArray;
        }
    }
}