using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace PublishScheduler
{
    public class MergeInfoParser
    {
        public readonly string Prefix;

        public MergeInfoParser(string prefix = "!bot")
        {
            Prefix = prefix;
        }

        public static MergeInfoParser GetCommentParser()
            => new MergeInfoParser("!bot");

        public static MergeInfoParser GetLabelParser()
            => new MergeInfoParser("automerge:");

        public MergeData Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentNullException(paramName: nameof(input), message: "Input string was null or whitespace.");

            // normalize input
            var splitInput = input.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // use select to get the value and the index
            foreach (var t in splitInput.Select((value, index) => new { value, index}))
            {
                if (Prefix.Equals(t.value, StringComparison.OrdinalIgnoreCase))
                {
                    int prefixIndex = t.index;

                    // out of bounds check
                    if ((prefixIndex + 3) > splitInput.Length)
                        return null;

                    // get the next two tokens, one should have the datetime in UTC, next should have the name of the branch
                    var date = splitInput[prefixIndex + 1];
                    var branch = splitInput[prefixIndex + 2];

                    var parsedDate = ParseDateTime(date);
                    if (!parsedDate.HasValue)
                        return null;

                    return new MergeData()
                    {
                        MergeTime = parsedDate.Value,
                        BranchName = branch
                    };
                }
            }

            return null;
        }
        internal static DateTime? ParseDateTime(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string[] formats =
            {
                "HH:mm",
                "H:mm",
                "MM/dd/yy,HH:mm",
                "M/d/yy,HH:mm",
                "M/d/yy,H:mm",
                "MM/dd/yyyy,HH:mm",
                "M/d/yyyy,HH:mm",
                "M/d/yyyy,H:mm"
            };
            var formatProvider = new CultureInfo("en-US");
            // let this throw on error
            if (DateTime.TryParseExact(input, formats, formatProvider, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dateValue))
            {
                return dateValue;
            }
            // did not match the input
            return null;
        }
    }
}
