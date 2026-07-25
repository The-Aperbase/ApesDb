using System.Net.Http.Json;
using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ApesDb.Api.Tests.Infrastructure.Http;

public sealed class ApiTestClient : IDisposable
{
    private readonly HttpClient _client;

    private ApiTestClient(ApiTestWebApplicationFactory factory, TestUser? identity)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
            }
        );

        if (identity is not null)
        {
            _client.DefaultRequestHeaders.Add(FakeAuthenticationHandler.HeaderName, identity.Key);
        }
    }

    public static ApiTestClient CreateAuthenticated(ApiTestWebApplicationFactory factory, TestUser identity)
    {
        return new ApiTestClient(factory, identity);
    }

    public static ApiTestClient CreateAnonymous(ApiTestWebApplicationFactory factory)
    {
        return new ApiTestClient(factory, null);
    }

    public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return _client.GetAsync(requestUri, cancellationToken);
    }

    public Task<HttpResponseMessage> GetAsync(
        string requestUri,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken = default
    )
    {
        return _client.GetAsync(requestUri, completionOption, cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return _client.PostAsync(requestUri, null, cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsync(
        string requestUri,
        HttpContent content,
        CancellationToken cancellationToken = default
    )
    {
        return _client.PostAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsJsonAsync<T>(
        string requestUri,
        T value,
        CancellationToken cancellationToken = default
    )
    {
        return _client.PostAsJsonAsync(requestUri, value, cancellationToken);
    }

    public Task<HttpResponseMessage> PostMultipartAsync(
        string requestUri,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default
    )
    {
        return _client.PostAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PutAsync(
        string requestUri,
        HttpContent content,
        CancellationToken cancellationToken = default
    )
    {
        return _client.PutAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PutAsJsonAsync<T>(
        string requestUri,
        T value,
        CancellationToken cancellationToken = default
    )
    {
        return _client.PutAsJsonAsync(requestUri, value, cancellationToken);
    }

    public Task<HttpResponseMessage> PutMultipartAsync(
        string requestUri,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default
    )
    {
        return _client.PutAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return _client.DeleteAsync(requestUri, cancellationToken);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
