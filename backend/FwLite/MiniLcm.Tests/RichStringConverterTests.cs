using System.Text.Json;

namespace MiniLcm.Tests;

public class RichStringConverterTests
{
    [Fact]
    public void Deserialize_ReturnsNull_WhenJsonIsNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ReturnsNull_WhenJsonIsEmptyString()
    {
        // Arrange
        var json = "\"\"";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ReturnsNull_WhenJsonIsWhitespaceString()
    {
        // Arrange
        var json = "\"  \\t\\n  \"";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ReturnsNull_WhenSpansIsNull()
    {
        // Arrange
        //lang=json
        var json = """{"Spans": null}""";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ReturnsNull_WhenSpansIsEmptyArray()
    {
        // Arrange
        //lang=json
        var json = """{"Spans": []}""";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ReturnsRichString_WhenSpansHasOneItem()
    {
        // Arrange
        //lang=json
        var json = """{"Spans": [{"Text": "test"}]}""";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().NotBeNull();
        result\!.Spans.Should().HaveCount(1);
        result.Spans[0].Text.Should().Be("test");
    }

    [Fact]
    public void Deserialize_ReturnsRichString_WhenJsonIsSimpleString()
    {
        // Arrange
        var json = "\"test\"";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().NotBeNull();
        result\!.Spans.Should().HaveCount(1);
        result.Spans[0].Text.Should().Be("test");
    }

    [Fact]
    public void Deserialize_ReturnsRichString_WhenSpansHasMultipleItems()
    {
        // Arrange
        //lang=json
        var json = """{"Spans": [{"Text": "test1"}, {"Text": "test2"}]}""";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().NotBeNull();
        result\!.Spans.Should().HaveCount(2);
        result.Spans[0].Text.Should().Be("test1");
        result.Spans[1].Text.Should().Be("test2");
    }

    [Fact]
    public void Deserialize_PreservesSpanProperties()
    {
        // Arrange
        //lang=json
        var json = """{"Spans": [{"Text": "test", "Bold": "On", "Italic": "On"}]}""";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().NotBeNull();
        result\!.Spans.Should().HaveCount(1);
        result.Spans[0].Text.Should().Be("test");
        result.Spans[0].Bold.Should().Be(RichTextToggle.On);
        result.Spans[0].Italic.Should().Be(RichTextToggle.On);
    }

    [Fact]
    public void Serialize_WritesSpansProperty()
    {
        // Arrange
        var richString = new RichString("test", "en");

        // Act
        var json = JsonSerializer.Serialize(richString);

        // Assert
        json.Should().Contain("\"Spans\"");
        json.Should().Contain("\"Text\"");
        json.Should().Contain("test");
    }

    [Fact]
    public void Serialize_PreservesSpanProperties()
    {
        // Arrange
        var richString = new RichString([new RichSpan
        {
            Text = "test",
            Ws = "en",
            Bold = RichTextToggle.On,
            Italic = RichTextToggle.On
        }]);

        // Act
        var json = JsonSerializer.Serialize(richString);

        // Assert
        json.Should().Contain("\"Text\":\"test\"");
        json.Should().Contain("\"Bold\":\"On\"");
        json.Should().Contain("\"Italic\":\"On\"");
    }

    [Fact]
    public void RoundTrip_PreservesRichString()
    {
        // Arrange
        var original = new RichString([new RichSpan
        {
            Text = "test",
            Ws = "en",
            Bold = RichTextToggle.On
        }]);

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void RoundTrip_PreservesMultipleSpans()
    {
        // Arrange
        var original = new RichString([
            new RichSpan { Text = "test1", Ws = "en" },
            new RichSpan { Text = "test2", Ws = "en", Bold = RichTextToggle.On }
        ]);

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Deserialize_HandlesSpanWithEmptyText()
    {
        // Arrange
        //lang=json
        var json = """{"Spans": [{"Text": ""}]}""";

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().NotBeNull();
        result\!.Spans.Should().HaveCount(1);
        result.Spans[0].Text.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_MergesConsecutiveSpansWithSameProperties()
    {
        // Arrange - RichString constructor merges spans with same properties
        var richString = new RichString([
            new RichSpan { Text = "test1", Ws = "en", Bold = RichTextToggle.On },
            new RichSpan { Text = "test2", Ws = "en", Bold = RichTextToggle.On }
        ]);

        // Assert
        richString.Spans.Should().HaveCount(1);
        richString.Spans[0].Text.Should().Be("test1test2");
    }

    [Theory]
    [InlineData("\"   \"")]
    [InlineData("\"\\t\"")]
    [InlineData("\"\\n\"")]
    [InlineData("\" \\t\\n \"")]
    public void Deserialize_ReturnsNull_ForVariousWhitespaceStrings(string json)
    {
        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_PreservesComplexFormatting()
    {
        // Arrange
        //lang=json
        var json = """
        {
          "Spans": [{
            "Text": "formatted text",
            "Ws": "en",
            "Bold": "On",
            "Italic": "On",
            "FontFamily": "Arial",
            "FontSize": 12,
            "ForeColor": "#FF0000"
          }]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<RichString>(json);

        // Assert
        result.Should().NotBeNull();
        result\!.Spans.Should().HaveCount(1);
        var span = result.Spans[0];
        span.Text.Should().Be("formatted text");
        span.Bold.Should().Be(RichTextToggle.On);
        span.Italic.Should().Be(RichTextToggle.On);
        span.FontFamily.Should().Be("Arial");
        span.FontSize.Should().Be(12);
    }

    [Fact]
    public void Deserialize_ConsistentWithEmptySpansList()
    {
        // This test ensures the null mapping logic is consistent between
        // empty string and empty spans array
        
        // Arrange
        var emptyStringJson = "\"\"";
        var emptySpansJson = """{"Spans": []}""";

        // Act
        var fromEmptyString = JsonSerializer.Deserialize<RichString>(emptyStringJson);
        var fromEmptySpans = JsonSerializer.Deserialize<RichString>(emptySpansJson);

        // Assert
        fromEmptyString.Should().BeNull();
        fromEmptySpans.Should().BeNull();
    }
}