using System.Net;
using Umea.se.TestToolkit.Exceptions;

namespace Umea.se.TestToolkit.Mocks;

public class HttpMessageHandlerMock : HttpMessageHandler
{
    private readonly Dictionary<Uri, (HttpStatusCode responseCode, string? responseMessage)> _getEndpoints = [];
    private readonly Dictionary<(Uri url, string? body), (HttpStatusCode responseCode, string? responseMessage)> _postEndpoints = [];
    private readonly Dictionary<(Uri url, string? body), (HttpStatusCode responseCode, string? responseMessage)> _putEndpoints = [];
    private readonly List<Uri> _dummyEndpoints = [];

    private readonly string _baseUrl;

    public HttpMessageHandlerMock(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public List<(HttpMethod method, Uri url, string? body)> Calls { get; } = [];

    public HttpMessageHandlerMock SetupGetEndpoint(string url, HttpStatusCode responseCode, string? responseMessage)
    {
        _getEndpoints.Add(new Uri(_baseUrl + url), (responseCode, responseMessage));
        return this;
    }

    public HttpMessageHandlerMock SetupPostEndpoint(string url, string body, HttpStatusCode responseCode, string? responseMessage)
    {
        _postEndpoints.Add((new Uri(_baseUrl + url), body), (responseCode, responseMessage));
        return this;
    }

    public HttpMessageHandlerMock SetupPutEndpoint(string url, string body, HttpStatusCode responseCode, string? responseMessage)
    {
        _putEndpoints.Add((new Uri(_baseUrl + url), body), (responseCode, responseMessage));
        return this;
    }

    public HttpMessageHandlerMock SetupDummyEndpoint(string url)
    {
        _dummyEndpoints.Add(new Uri(_baseUrl + url));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
        {
            throw new ArgumentNullException(nameof(request.RequestUri));
        }

        string? content = request.Content == null ? null : await request.Content.ReadAsStringAsync(CancellationToken.None);
        Calls.Add((request.Method, request.RequestUri, content));

        if (_dummyEndpoints.Any(str => str.ToString().Contains(request.RequestUri.ToString())))
        {
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
        }

        try
        {
            (HttpStatusCode responseCode, string? responseMessage) =
                request.Method == HttpMethod.Get ? _getEndpoints[request.RequestUri] :
                request.Method == HttpMethod.Post ? _postEndpoints[(request.RequestUri, content)] :
                request.Method == HttpMethod.Put ? _putEndpoints[(request.RequestUri, content)] :
                throw new MockNotSetupException("Http Method not supported!");

            return new()
            {
                StatusCode = responseCode,
                Content = responseMessage == null ? null : new StringContent(responseMessage)
            };
        }
        catch (Exception e)
        {
            throw new MockNotSetupException("Mock is not set up for this endpoint!", e);
        }
    }
}
