using System.Net;
using System.Text;
using LifestyleChecker.Api.Interfaces;
using LifestyleChecker.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LifestyleChecker.Tests.Integration;

internal sealed class LifestyleAppFactory(
    Func<HttpRequestMessage, HttpResponseMessage> respond) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PatientApi:SubscriptionKey"] = "integration-test-key"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<IPatientApiClient>();
            services.AddHttpClient<IPatientApiClient, PatientApiClient>(client =>
                client.BaseAddress = new Uri("https://al-tech-test-apim.azure-api.net/"))
                .ConfigurePrimaryHttpMessageHandler(() => new MockPatientApiHandler(respond));
        });
    }

    public HttpClient CreateBrowserClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private sealed class MockPatientApiHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(respond(request));
        }
    }
}
