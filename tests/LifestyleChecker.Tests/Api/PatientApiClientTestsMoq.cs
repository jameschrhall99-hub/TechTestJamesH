using System.Net;
using System.Text;
using LifestyleChecker.Api;
using LifestyleChecker.Models;
using Moq;
using Moq.Protected;

namespace LifestyleChecker.Tests.Api;

public class PatientApiClientTestsMoq
{
    private const string BaseAddress = "https://al-tech-test-apim.azure-api.net/";
    private const string NhsNumber = "123456789";
    private const string ValidPatientJson =
        "{\"nhsNumber\":\"123456789\",\"name\":\"DOE, John\",\"born\":\"25-12-1990\"}";

    [Fact]
    public async Task PatientApiTest_AllReturns()
    {
        //setup test constants and mocked HTTP response
        var handler = MockResponse(HttpStatusCode.OK, ValidPatientJson);
        using var httpClient = new HttpClient(handler.Object)
        {
            //test builds full request uri, api key not used though
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        //call client with nhs number
        var result = await apiClient.GetPatientAsync(NhsNumber);

        //check both result status and parsed patient details
        Assert.Equal(PatientLookupStatus.Success, result.Status);
        Assert.NotNull(result.Patient);
        Assert.Equal(NhsNumber, result.Patient.NhsNumber);
        Assert.Equal("DOE, John", result.Patient.Name);
        Assert.Equal(new DateOnly(1990, 12, 25), result.Patient.Born);
    }

    [Fact]
    public async Task PatientApiTest_404ReturnsNotFound()
    {
        //setup test constants and mocked HTTP response
        var handler = MockResponse(HttpStatusCode.NotFound);
        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        //call client with nhs number
        var result = await apiClient.GetPatientAsync(NhsNumber);

        //check both result status and parsed patient details
        Assert.Equal(PatientLookupStatus.NotFound, result.Status);
        Assert.Null(result.Patient);
    }

    [Fact]
    public async Task PatientApiTest_UnavailableReturnsUnavailable()
    {
        //setup test constants and mocked HTTP response
        var handler = MockResponse(HttpStatusCode.ServiceUnavailable);
        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        //call client with nhs number
        var result = await apiClient.GetPatientAsync(NhsNumber);

        //check both result status and parsed patient details
        Assert.Equal(PatientLookupStatus.Unavailable, result.Status);
        Assert.Null(result.Patient);
    }

    [Fact]
    public async Task PatientApiTest_MalformedJson()
    {
        //setup test constants, missing comma
        var handler = MockResponse(
            HttpStatusCode.OK,
            "{\"nhsNumber\":\"123456789\" \"name\":\"DOE, John\",\"born\":\"25-12-1990\"}");
        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        //call client with nhs number
        var result = await apiClient.GetPatientAsync(NhsNumber);

        //check both result status and parsed patient details
        Assert.Equal(PatientLookupStatus.Unavailable, result.Status);
        Assert.Null(result.Patient);
    }

    [Fact]
    public async Task PatientApiTest_MissingNHSNumber()
    {
        //setup test constants, valid json empty nhs number
        var handler = MockResponse(
            HttpStatusCode.OK,
            "{\"nhsNumber\":\"\",\"name\":\"DOE, John\",\"born\":\"25-12-1990\"}");
        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        //call client with nhs number
        var result = await apiClient.GetPatientAsync(NhsNumber);

        //check both result status and parsed patient details
        Assert.Equal(PatientLookupStatus.Unavailable, result.Status);
        Assert.Null(result.Patient);
    }

    [Fact]
    public async Task PatientApiTest_InvalidDOB()
    {
        //setup test constants, DOB month = 15
        var handler = MockResponse(
            HttpStatusCode.OK,
            "{\"nhsNumber\":\"123456789\",\"name\":\"DOE, John\",\"born\":\"25-15-1990\"}");
        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        //call client with nhs number
        var result = await apiClient.GetPatientAsync(NhsNumber);

        //check both result status and parsed patient details
        Assert.Equal(PatientLookupStatus.Unavailable, result.Status);
        Assert.Null(result.Patient);
    }

    [Fact]
    public async Task PatientApiTest_Timeout()
    {
        //setup test constants, 504 gateway timeout
        var handler = MockResponse(HttpStatusCode.GatewayTimeout, ValidPatientJson);
        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        //call client with nhs number
        var result = await apiClient.GetPatientAsync(NhsNumber);

        //check both result status and parsed patient details
        Assert.Equal(PatientLookupStatus.Unavailable, result.Status);
        Assert.Null(result.Patient);
    }

    [Fact]
    public async Task PatientApiTest_HttpRequestException()
    {
        //setup test constants, network failure
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("network failure"));
        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        //call client with nhs number
        var result = await apiClient.GetPatientAsync(NhsNumber);

        //check both result status and parsed patient details
        Assert.Equal(PatientLookupStatus.Unavailable, result.Status);
        Assert.Null(result.Patient);
    }

    [Fact]
    public async Task PatientApiTest_SendsExpectedRequestDetails()
    {
        //setup test constants
        const string testKey = "test-key";
        HttpRequestMessage? sentRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => sentRequest = request)
            .ReturnsAsync(JsonResponse(ValidPatientJson));

        using var httpClient = new HttpClient(handler.Object)
        {
            //test builds full request uri and includes API key (mock key, not real one)
            BaseAddress = new Uri(BaseAddress)
        };

        // Use a mock key, doesn't call network
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", testKey);

        var apiClient = new PatientApiClient(httpClient);
        var result = await apiClient.GetPatientAsync(NhsNumber);

        Assert.Equal(PatientLookupStatus.Success, result.Status);
        Assert.NotNull(sentRequest);
        Assert.Equal(HttpMethod.Get, sentRequest.Method);
        Assert.Equal(
            "https://al-tech-test-apim.azure-api.net/tech-test/t2/patients/123456789",
            sentRequest.RequestUri?.AbsoluteUri);
        Assert.Equal(
            testKey,
            Assert.Single(sentRequest.Headers.GetValues("Ocp-Apim-Subscription-Key")));
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PatientApiTest_TaskCanceledException()
    {
        //setup test constants, HTTP request timeout
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timeout"));
        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        //call client with nhs number
        var result = await apiClient.GetPatientAsync(NhsNumber);

        //check both result status and parsed patient details
        Assert.Equal(PatientLookupStatus.Unavailable, result.Status);
        Assert.Null(result.Patient);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345x789")]
    public async Task PatientApiTest_InvalidNhsNumber(string nhsNumber)
    {
        //No response configured, invalid input rejected before SendAsync
        var handler = new Mock<HttpMessageHandler>();
        using var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        var apiClient = new PatientApiClient(httpClient);

        var result = await apiClient.GetPatientAsync(nhsNumber);

        Assert.Equal(PatientLookupStatus.Unavailable, result.Status);
        handler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    private static Mock<HttpMessageHandler> MockResponse(
        HttpStatusCode statusCode,
        string? json = null)
    {
        var response = json is null
            ? new HttpResponseMessage(statusCode)
            : JsonResponse(json, statusCode);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return handler;
    }

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
