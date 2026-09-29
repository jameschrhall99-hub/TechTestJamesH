using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using LifestyleChecker.Api.Interfaces;
using Xunit;

namespace LifestyleChecker.Tests.Integration;

public class LifestyleFlowTests
{
    private const string NhsNumber = "123456789";
    private const string Surname = "DOE";
    private const string TestKey = "integration-test-key";
    private const string LowResult = "Thank you for answering our questions, we don't need to see you at this time. Keep up the good work!";
    private const string HighResult = "We think there are some simple things you could do to improve your quality of life, please phone to book an appointment";

    [Theory]
    [InlineData(true, true, true, LowResult)]
    [InlineData(true, true, false, HighResult)]
    public async Task CompleteFlow_ShowsExpectedResult_AndConsumesVerification(
        bool q1Yes,
        bool q2Yes,
        bool q3Yes,
        string expectedResult)
    {
        var birthDate = DateOnly.FromDateTime(DateTime.Today).AddYears(-18);
        string? requestedPath = null;
        string? subscriptionKey = null;
        using var factory = new LifestyleAppFactory(request =>
        {
            requestedPath = request.RequestUri?.AbsolutePath;
            subscriptionKey = request.Headers.GetValues("Ocp-Apim-Subscription-Key").Single();
            return PatientResponse(HttpStatusCode.OK, PatientJson(NhsNumber, $"{Surname}, Jane", birthDate));
        });
        using var client = factory.CreateBrowserClient();

        var form = await GetFormAsync(client, "/");
        using var partOne = await client.PostAsync("/", Form(
            form.Token,
            ("NhsNumber", NhsNumber),
            ("Surname", Surname),
            ("DateOfBirth", birthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))));

        AssertRedirect(partOne, "/PartTwo");
        Assert.Equal("/tech-test/t2/patients/" + NhsNumber, requestedPath);
        Assert.Equal(TestKey, subscriptionKey);

        var partTwoForm = await GetFormAsync(client, "/PartTwo");
        using var partTwo = await client.PostAsync("/PartTwo", Form(
            partTwoForm.Token,
            ("Q1Yes", q1Yes ? "true" : "false"),
            ("Q2Yes", q2Yes ? "true" : "false"),
            ("Q3Yes", q3Yes ? "true" : "false")));

        AssertRedirect(partTwo, "/Result");

        using var result = await client.GetAsync("/Result");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains(expectedResult, HttpUtility.HtmlDecode(await result.Content.ReadAsStringAsync()));

        using var repeatedPost = await client.PostAsync("/PartTwo", Form(
            partTwoForm.Token,
            ("Q1Yes", q1Yes ? "true" : "false"),
            ("Q2Yes", q2Yes ? "true" : "false"),
            ("Q3Yes", q3Yes ? "true" : "false")));
        AssertRedirect(repeatedPost, "/");
    }

    [Theory]
    [InlineData("not-found")]
    [InlineData("wrong-surname")]
    [InlineData("under-16")]
    [InlineData("unavailable")]
    public async Task PatientLookupOutcome_ShowsMessageAndDoesNotEnterPartTwo(string scenario)
    {
        var adultBirthDate = DateOnly.FromDateTime(DateTime.Today).AddYears(-18);
        var underAgeBirthDate = DateOnly.FromDateTime(DateTime.Today).AddYears(-15);
        using var factory = new LifestyleAppFactory(_ => scenario switch
        {
            "not-found" => PatientResponse(HttpStatusCode.NotFound),
            "wrong-surname" => PatientResponse(HttpStatusCode.OK, PatientJson(NhsNumber, "SMITH, Jane", adultBirthDate)),
            "under-16" => PatientResponse(HttpStatusCode.OK, PatientJson(NhsNumber, $"{Surname}, Jane", underAgeBirthDate)),
            "unavailable" => PatientResponse(HttpStatusCode.ServiceUnavailable),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        });
        using var client = factory.CreateBrowserClient();
        var form = await GetFormAsync(client, "/");
        var enteredDate = scenario == "under-16" ? underAgeBirthDate : adultBirthDate;

        using var response = await client.PostAsync("/", Form(
            form.Token,
            ("NhsNumber", NhsNumber),
            ("Surname", Surname),
            ("DateOfBirth", enteredDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        var expected = scenario switch
        {
            "not-found" or "wrong-surname" => "Your details could not be found",
            "under-16" => "You are not eligble for this service",
            "unavailable" => "The patient service is currently unavailable. Please try again later.",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        Assert.Contains(expected, page);

        using var guardedPartTwo = await client.GetAsync("/PartTwo");
        AssertRedirect(guardedPartTwo, "/");
    }

    [Fact]
    public async Task InvalidPatientForm_RendersValidationAndPreservesEnteredValues()
    {
        using var factory = new LifestyleAppFactory(_ =>
            throw new InvalidOperationException("The API must not be called for invalid form input."));
        using var client = factory.CreateBrowserClient();
        var form = await GetFormAsync(client, "/");

        using var response = await client.PostAsync("/", Form(
            form.Token,
            ("NhsNumber", NhsNumber),
            ("Surname", ""),
            ("DateOfBirth", "not-a-date")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains("Enter your surname.", page);
        Assert.Contains("Enter a valid date of birth.", page);
        Assert.Contains("value=\"" + NhsNumber + "\"", page);
        Assert.Contains("value=\"not-a-date\"", page);
    }

    [Fact]
    public async Task MissingLifestyleAnswer_RendersValidationAndKeepsSelectedAnswers()
    {
        var birthDate = DateOnly.FromDateTime(DateTime.Today).AddYears(-18);
        using var factory = new LifestyleAppFactory(_ =>
            PatientResponse(HttpStatusCode.OK, PatientJson(NhsNumber, $"{Surname}, Jane", birthDate)));
        using var client = factory.CreateBrowserClient();
        var startForm = await GetFormAsync(client, "/");
        using var partOne = await client.PostAsync("/", Form(
            startForm.Token,
            ("NhsNumber", NhsNumber),
            ("Surname", Surname),
            ("DateOfBirth", birthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))));
        AssertRedirect(partOne, "/PartTwo");

        var questions = await GetFormAsync(client, "/PartTwo");
        using var response = await client.PostAsync("/PartTwo", Form(
            questions.Token,
            ("Q1Yes", "true"),
            ("Q2Yes", "false")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains("Please provide an answer to question 3.", page);
        AssertInputChecked(page, "q1-yes");
        AssertInputChecked(page, "q2-no");
        Assert.DoesNotContain("Your result", page);
    }

    [Fact]
    public async Task PartTwoAndResultRoutes_RequireTheirSessionState()
    {
        using var factory = new LifestyleAppFactory(_ =>
            throw new InvalidOperationException("The API must not be called in this route guard test."));
        using var client = factory.CreateBrowserClient();

        using var partTwoGet = await client.GetAsync("/PartTwo");
        AssertRedirect(partTwoGet, "/");
        using var resultGet = await client.GetAsync("/Result");
        AssertRedirect(resultGet, "/");

        var startForm = await GetFormAsync(client, "/");
        using var partTwoPost = await client.PostAsync("/PartTwo", Form(
            startForm.Token,
            ("Q1Yes", "true"),
            ("Q2Yes", "true"),
            ("Q3Yes", "true")));
        AssertRedirect(partTwoPost, "/");
    }

    [Fact]
    public async Task StartAgain_ClearsCompletedFlowFromSession()
    {
        var birthDate = DateOnly.FromDateTime(DateTime.Today).AddYears(-18);
        using var factory = new LifestyleAppFactory(_ =>
            PatientResponse(HttpStatusCode.OK, PatientJson(NhsNumber, $"{Surname}, Jane", birthDate)));
        using var client = factory.CreateBrowserClient();
        var startForm = await GetFormAsync(client, "/");
        using var partOne = await client.PostAsync("/", Form(
            startForm.Token,
            ("NhsNumber", NhsNumber),
            ("Surname", Surname),
            ("DateOfBirth", birthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))));
        AssertRedirect(partOne, "/PartTwo");

        var questions = await GetFormAsync(client, "/PartTwo");
        using var partTwo = await client.PostAsync("/PartTwo", Form(
            questions.Token,
            ("Q1Yes", "true"),
            ("Q2Yes", "true"),
            ("Q3Yes", "true")));
        AssertRedirect(partTwo, "/Result");

        using var result = await client.GetAsync("/Result");
        Assert.Contains(LowResult, HttpUtility.HtmlDecode(await result.Content.ReadAsStringAsync()));
        using var startAgain = await client.GetAsync("/Index");
        Assert.Equal(HttpStatusCode.OK, startAgain.StatusCode);

        using var oldResult = await client.GetAsync("/Result");
        AssertRedirect(oldResult, "/");
        using var oldPartTwo = await client.GetAsync("/PartTwo");
        AssertRedirect(oldPartTwo, "/");
        using var repeatedPost = await client.PostAsync("/PartTwo", Form(
            questions.Token,
            ("Q1Yes", "true"),
            ("Q2Yes", "true"),
            ("Q3Yes", "true")));
        AssertRedirect(repeatedPost, "/");
    }

    private static async Task<(string Token, string Html)> GetFormAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected GET {path} to return 200 but received {(int)response.StatusCode}. Response: {html}");
        var match = Regex.Match(
            html,
            "<input\\b(?=[^>]*\\bname=\"__RequestVerificationToken\")(?=[^>]*\\bvalue=\"([^\"]+)\")[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Assert.True(match.Success, "The form should include an antiforgery token.");
        return (HttpUtility.HtmlDecode(match.Groups[1].Value), html);
    }

    private static FormUrlEncodedContent Form(string token, params (string Name, string Value)[] fields)
    {
        return new FormUrlEncodedContent(fields
            .Prepend((Name: "__RequestVerificationToken", Value: token))
            .Select(field => new KeyValuePair<string, string>(field.Name, field.Value)));
    }

    private static void AssertRedirect(HttpResponseMessage response, string path)
    {
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var redirectUri = response.Headers.Location.IsAbsoluteUri
            ? response.Headers.Location
            : new Uri(new Uri("http://localhost"), response.Headers.Location);
        Assert.Equal(path, redirectUri.AbsolutePath);
    }

    private static void AssertInputChecked(string html, string id)
    {
        var input = Regex.Match(html, $"<input\\b(?=[^>]*\\bid=\"{Regex.Escape(id)}\")(?=[^>]*\\bchecked(?:=\"checked\")?)[^>]*>", RegexOptions.IgnoreCase);
        Assert.True(input.Success, $"Expected radio input '{id}' to remain selected.");
    }

    private static HttpResponseMessage PatientResponse(HttpStatusCode statusCode, string? json = null)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = json is null
                ? new StringContent(string.Empty)
                : new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string PatientJson(string nhsNumber, string name, DateOnly born)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            nhsNumber,
            name,
            born = born.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
        });
    }
}
