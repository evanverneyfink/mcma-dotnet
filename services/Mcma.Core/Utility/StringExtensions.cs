
using System;
using System.Text;

namespace Mcma.Core.Utility
{
    public static class StringExtensions
    {
        public static string Replace(this string source, string toReplace, string replaceWith, StringComparison stringComparison)
        {
            var curIndex = 0;
            var indexOfNextReplacement = source.IndexOf(toReplace, curIndex, stringComparison);

            var result = new StringBuilder();

            while (indexOfNextReplacement >= 0 && curIndex < source.Length)
            {
                if (indexOfNextReplacement != 0)
                    result.Append(source.Substring(curIndex, indexOfNextReplacement));

                result.Append(replaceWith);

                curIndex = indexOfNextReplacement + toReplace.Length;
                indexOfNextReplacement = source.IndexOf(toReplace, curIndex, stringComparison);
            }

            if (curIndex < source.Length)
                result.Append(source.Substring(curIndex));

            return result.ToString();
        }
    }
}