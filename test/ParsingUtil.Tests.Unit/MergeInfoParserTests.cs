using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading;
using Xunit;

namespace ParsingUtil.Tests.Unit
{
    public class MergeInfoParserTests
    {
        [Theory]
        [InlineData("!bot 22:00 test")]
        public void TestCommentParser(string input)
        {
            var parser = MergeInfoParser.GetCommentParser();
            var result = parser.Parse(input);
            Assert.Equal("test", result.BranchName);
            // assume the date is fine, because I am lazy
        }

        [Theory]
        [InlineData("automerge: 22:00 test")]

        public void TestLabelParser(string input)
        {
            var parser = MergeInfoParser.GetLabelParser();
            var result = parser.Parse(input);
            Assert.Equal("test", result.BranchName);
            // assume the date is fine, because I am lazy
        }

        [Theory]
        [InlineData("!bot 01/01/2001,01:01 test", "2001-01-01T01:01:00.0000000Z", "test")]
        [InlineData("!bot 22:00 test", "22:00", "test")]
        [InlineData("!bot 7/22/19,22:00 test", "7/22/19 22:00", "test")]
        [InlineData("this is some text that is going before it !bot 7/22/19,22:00 test this is some text going after", "7/22/19 22:00", "test")]
        public void TestExpected(string input, string expectedDate, string expectedBranch)
        {
            var date = DateTime.Parse(expectedDate, new CultureInfo("en-US"), DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            var parser = new MergeInfoParser("!bot");
            var result = parser.Parse(input);

            Assert.NotNull(result);
            Assert.Equal(date, result.MergeTime);
            Assert.Equal(expectedBranch, result.BranchName);
        }

        [Theory]
        [InlineData("this doesn't have the mention at all")]
        [InlineData("!bot do something")]
        [InlineData("!bot 01/01/11")] // missing branch name
        [InlineData("!bot 01:11")] // missing branch name
        [InlineData("!bot branch 01:11")]
        public void TestUnexpected(string input)
        {
            var parser = new MergeInfoParser("!bot");
            var result = parser.Parse(input);
            Assert.Null(result);
        }

        [Theory]
        [InlineData("01:01", 1, 1)]
        [InlineData("22:33", 22, 33)]
        [InlineData("1:01", 1, 1)]
        public void TestParseHourMinuteDateTime(string input, int hour, int minute)
        {
            // the date component of the result will be the current date
            var expectedDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, hour, minute, 0);

            var result = MergeInfoParser.ParseDateTime(input);
            Assert.NotNull(result);
            Assert.Equal(expectedDate, result.Value);
        }

        [Theory]
        [InlineData("01/01/2001,01:01")]
        [InlineData("1/01/2001,1:01")]
        public void TestParseFullDateTime(string input)
        {
            var expectedDate = new DateTime(2001, 1, 1, 1, 1, 0);

            var result = MergeInfoParser.ParseDateTime(input);

            Assert.NotNull(result);
            Assert.Equal(expectedDate, result.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("this is a test")]
        public void TestInvalidParseDateTime(string input)
        {
            var result = MergeInfoParser.ParseDateTime(input);
            Assert.Null(result);
        }
    }
}
