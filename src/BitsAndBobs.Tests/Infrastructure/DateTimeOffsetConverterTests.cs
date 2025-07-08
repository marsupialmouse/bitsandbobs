using System.Globalization;
using Amazon.DynamoDBv2.DocumentModel;
using BitsAndBobs.Infrastructure;
using NUnit.Framework;
using Shouldly;

namespace BitsAndBobs.Tests.Infrastructure;

[TestFixture]
public class DateTimeOffsetConverterTests
{
    [Test]
    public void ShouldThrowExceptionWhenValueIsNotDateTimeOffset()
    {
        var converter = new DateTimeOffsetConverter();

        Should.Throw<ArgumentException>(() => converter.ToEntry(DateTime.Now));
    }

    [Test]
    public void ShouldReturnValueAsString()
    {
        var value = DateTimeOffset.Now.AddMinutes(301).AddSeconds(43);
        var converter = new DateTimeOffsetConverter();

        var result = converter.ToEntry(value);

        result.AsString().ShouldBe(value.ToString("o"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("10/12/2005 12:00:00")]
    [TestCase("Hello!")]
    public void ShouldThrowExceptionWhenSerializedValueIsNotValidDateTime(string? value)
    {
        var converter = new DateTimeOffsetConverter();

        Should.Throw<ArgumentException>(() => converter.FromEntry(new Primitive(value, false)));
    }

    [Test]
    public void ShouldConvertSerializedDateToDateTime()
    {
        var value = DateTimeOffset.Now.AddMinutes(301).AddSeconds(43);
        var converter = new DateTimeOffsetConverter();

        var result = converter.FromEntry(new Primitive(value.ToString("o", CultureInfo.InvariantCulture), false));

        result.ShouldBe(value);
    }
}
