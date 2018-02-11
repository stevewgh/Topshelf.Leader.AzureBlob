using System;
using Xunit;

namespace Topshelf.Leader.AzureBlob.Tests
{
    public class StringToGuidConverterShould
    {
        [Theory]
        [InlineData("")]
        [InlineData("a0eb5a79-4ef5-4790-8dba-29dd3f40288f")]
        [InlineData("a")]
        [InlineData("abcdefghijklmnopqrstuvwxyz1234567890")]
        [InlineData("!")]
        public void convert_a_string_to_a_guid(string s)
        {
            var guid = StringToGuidConverter.Convert(s);
            Assert.NotEqual(Guid.Empty, guid);
        }

        [Fact]
        public void convert_a_guid_without_any_hashing()
        {
            const string guidStr = "a0eb5a79-4ef5-4790-8dba-29dd3f40288f";
            var guid = StringToGuidConverter.Convert(guidStr);

            Assert.Equal(guidStr, guid.ToString());
        }

        [Theory]
        [InlineData("a0eb5a79-4ef5-4790-8dba-29dd3f40288f")]
        [InlineData("a")]
        [InlineData("abcdefghijklmnopqrstuvwxyz1234567890")]
        [InlineData("!")]
        public void produce_the_same_guid_for_a_given_input(string s)
        {
            var guid1 = StringToGuidConverter.Convert(s);
            var guid2 = StringToGuidConverter.Convert(s);

            Assert.Equal(guid1, guid2);
        }

        [Theory]
        [InlineData("a", "b")]
        [InlineData("1", "2")]
        [InlineData("", " ")]
        public void produce_a_different_guid_for_two_given_inputs(string s1, string s2)
        {
            var guid1 = StringToGuidConverter.Convert(s1);
            var guid2 = StringToGuidConverter.Convert(s2);

            Assert.NotEqual(guid1, guid2);
        }

    }
}