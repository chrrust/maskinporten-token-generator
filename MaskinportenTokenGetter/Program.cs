using System.CommandLine;
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

var config = builder.Build();

var rootCommand = new RootCommand();

var generateCommand = new Command("generate");

var scopesOption = new Option<string[]>("--scopes")
{
    AllowMultipleArgumentsPerToken = true
};

var tokenTypeOption = new Option<string>("--type")
{
    IsRequired = true
};

tokenTypeOption.FromAmong("maskinporten", "altinn");

var environmentOption = new Option<string>("--environment")
{
    IsRequired = true
};

environmentOption.FromAmong("test", "prod");

generateCommand.AddOption(scopesOption);
generateCommand.AddOption(tokenTypeOption);
generateCommand.AddOption(environmentOption);

generateCommand.SetHandler(HandleTokenCommand, tokenTypeOption, scopesOption, environmentOption);

rootCommand.AddCommand(generateCommand);

rootCommand.Invoke(args);


void HandleTokenCommand(string tokenType, string[] scopes, string environment)
{
    string? encodedJwk;
    string? clientId;

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

    if (string.IsNullOrWhiteSpace(clientId))
    {
        Console.WriteLine("Client ID was null or whitespace.");
        return;
    }
    
    if (string.IsNullOrWhiteSpace(encodedJwk))
    {
        Console.WriteLine("Encoded JWK was null or whitespace");
        return;
    }
    var jti = Guid.NewGuid().ToString();

    var jwkString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedJwk));
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

    var claims = new List<Claim>()
    {
        new("aud", aud),
        new("iss", clientId!),
        new("exp", DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds().ToString()),
        new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
        new("jti", jti)
    };

    if (scopes.Length != 0)
        claims.Add(new Claim("scope", string.Join(" ", scopes)));

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

    var response = httpClient.Send(httpRequest);
    var responseContent = response.Content.ReadAsStringAsync().Result; // TODO: Fix this

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

    var response2 = httpClient.Send(httpRequest2);
    var responseContent2 = response2.Content.ReadAsStringAsync().Result; // TODO: Fix

    Console.WriteLine(responseContent2);
    
    
}