using System.Net.Http.Headers;

namespace Foundry.Tests.Common.Helpers;

public static class HttpClientExtensions
{
    public static HttpClient WithAuth(this HttpClient client, string userId, string[]? roles = null)
    {
        string token = JwtTokenHelper.GenerateToken(userId, roles: roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
