namespace Testing.Fixtures;

public static class AssertHttpResponse
{
    public static void ShouldBeSuccessful(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var stream = response.Content.ReadAsStream();
        var content = new StreamReader(stream).ReadToEnd();
        Assert.Fail($"Expected response to be successful, but was {response.StatusCode}, response body was: {content}");
    }
}
