using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

var builder = new ConfigurationBuilder()
    .AddUserSecrets<Program>();

var argList = args.ToList();
var envArgIndex = argList.FindIndex(x => x == "--env");


var tokenType = args.First();
var scopes = args.Skip(1).ToArray();

// Remove --env and the environment argument
scopes = scopes.Where(x => x != "--env" && x != args[envArgIndex + 1]).ToArray();

var config = builder.Build();

var environment = "dev";
if (envArgIndex != -1)
{
    environment = argList[envArgIndex + 1];
}

string? encodedJwk = null;
string? clientId = null;

if (environment == "prod")
{
    encodedJwk = config.GetValue<string>("ProdEncodedJwk");
    clientId = config.GetValue<string>("ProdClientId");
}
else
{
    encodedJwk = config.GetValue<string>("EncodedJwk");
    clientId = config.GetValue<string>("ClientId");
}


var scope = string.Join(' ', scopes);
var jti = Guid.NewGuid().ToString();

var jwkString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedJwk!));
var jwk = new JsonWebKey(jwkString);

var rsa = RSA.Create();
rsa.ImportParameters(new RSAParameters
{
    Modulus = Base64UrlEncoder.DecodeBytes(jwk.N),
    Exponent = Base64UrlEncoder.DecodeBytes(jwk.E),
    D = Base64UrlEncoder.DecodeBytes(jwk.D),
    P = Base64UrlEncoder.DecodeBytes(jwk.P),
    Q = Base64UrlEncoder.DecodeBytes(jwk.Q),
    DP = Base64UrlEncoder.DecodeBytes(jwk.DP),
    DQ = Base64UrlEncoder.DecodeBytes(jwk.DQ),
    InverseQ = Base64UrlEncoder.DecodeBytes(jwk.QI)
});

var key = new RsaSecurityKey(rsa) { KeyId = jwk.Kid };

var aud = environment == "prod"
    ? "https://maskinporten.no/"
    : "https://test.maskinporten.no/";

var claims = new[]
{
    new Claim("aud", aud),
    new Claim("iss", clientId!),
    new Claim("scope", scope),
    new Claim("exp", DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds().ToString()),
    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
    new Claim("jti", jti)
};

var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(claims),
    Expires = DateTime.UtcNow.AddMinutes(1),
    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
};

var tokenHandler = new JwtSecurityTokenHandler();

var token = tokenHandler.CreateToken(tokenDescriptor);
var tokenString = tokenHandler.WriteToken(token);

using var httpClient = new HttpClient();
var httpRequest = new HttpRequestMessage();

httpRequest.Method = HttpMethod.Post;
var requestUri = environment == "prod"
    ? "https://maskinporten.no/token"
    : "https://test.maskinporten.no/token";

httpRequest.RequestUri = new Uri(requestUri);
httpRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
{
    { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
    { "assertion", tokenString }
});

var response = await httpClient.SendAsync(httpRequest);
var responseContent = await response.Content.ReadAsStringAsync();

// Parse "access_token" property from json string response
var accessToken = JObject.Parse(responseContent)["access_token"]?.ToString();


if (tokenType == "maskinporten")
{
    Console.WriteLine(accessToken);
    return;
}

var httpRequest2 = new HttpRequestMessage();

httpRequest2.Method = HttpMethod.Get;

var requestUri2 = environment == "prod"
    ? "https://platform.altinn.no/authentication/api/v1/exchange/maskinporten"
    : "https://platform.tt02.altinn.no/authentication/api/v1/exchange/maskinporten";

httpRequest2.RequestUri = new Uri(requestUri2);
httpRequest2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

var response2 = await httpClient.SendAsync(httpRequest2);
var responseContent2 = await response2.Content.ReadAsStringAsync();

Console.WriteLine(responseContent2);


