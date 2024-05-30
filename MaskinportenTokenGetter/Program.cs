using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

var credentialsStore = new CredentialsStore();

credentialsStore.Load();

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

var credentialsOption = new Option<string>("--credentials")
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
generateCommand.AddOption(credentialsOption);

generateCommand.SetHandler(HandleTokenCommand, tokenTypeOption, scopesOption, environmentOption, credentialsOption);

rootCommand.AddCommand(generateCommand);

var credentialsCommand = new Command("credentials");

var addCredentialsCommand = new Command("add");

var addCredentialsNameArgument = new Argument<string>("credential-set name");

addCredentialsCommand.AddArgument(addCredentialsNameArgument);

addCredentialsCommand.SetHandler(HandleAddCredentialsCommand, addCredentialsNameArgument);

credentialsCommand.AddCommand(addCredentialsCommand);

var removeCredentialsCommand = new Command("remove");

var removeCredentialsNameArgument = new Argument<string>("credential-set name");

removeCredentialsCommand.AddArgument(removeCredentialsNameArgument);

removeCredentialsCommand.SetHandler(HandleRemoveCredentialsCommand, removeCredentialsNameArgument);

credentialsCommand.AddCommand(removeCredentialsCommand);

var listCredentialsCommand = new Command("list");

listCredentialsCommand.SetHandler(HandleListCredentialsCommand);

credentialsCommand.AddCommand(listCredentialsCommand);

rootCommand.AddCommand(credentialsCommand);

var commandLineBuilder = new CommandLineBuilder(rootCommand);

commandLineBuilder.UseDefaults();

var parser = commandLineBuilder.Build();

await parser.InvokeAsync(args);

if (credentialsStore.HasPendingChanges)
    credentialsStore.Save();

void HandleTokenCommand(string tokenType, string[] scopes, string environment, string credentialsSetName)
{
    var credentialSet = credentialsStore.Get(credentialsSetName);
    if (credentialSet is null)
    {
        Console.WriteLine($"Credentials set with name {credentialsSetName} could not be found");
        return;
    }
    
    var jti = Guid.NewGuid().ToString();

    var jwkString = Encoding.UTF8.GetString(Convert.FromBase64String(credentialSet.EncodedJwk));
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
        new("iss", credentialSet.ClientId.ToString()),
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

void HandleAddCredentialsCommand(string name)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    
    Console.WriteLine("Enter client id:");
    
    if (!Guid.TryParse(Console.ReadLine(), out var clientId))
        throw new ArgumentException("Failed to parse client id", nameof(clientId));
    
    Console.WriteLine("Enter encoded JWK:");
    var encodedJwk = ReadSecret();

    if (clientId == Guid.Empty)
        throw new ArgumentException("clientId must be an initialized guid.", nameof(clientId));
    
    ArgumentException.ThrowIfNullOrWhiteSpace(encodedJwk);
    
    var success = credentialsStore.TryAdd(name, clientId, encodedJwk);

    if (!success)
        Console.WriteLine("Failed to set credentials");
}

void HandleRemoveCredentialsCommand(string name)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(name);

    var success = credentialsStore.Remove(name);
    
    if (!success)
        Console.WriteLine("Failed to remove credentials");
}

void HandleListCredentialsCommand()
{
    var credentialSets = credentialsStore.GetAll().ToArray();
    
    for (var i = 0; i < credentialSets.Length; i++)
    {
        var credentialSet = credentialSets[i];
        Console.WriteLine(credentialSet.Name);
        Console.WriteLine(credentialSet.ClientId);
        Console.WriteLine(credentialSet.EncodedJwk);
        
        if (i != credentialSets.Length - 1)
            Console.WriteLine("-------------------");
    }
}

static string ReadSecret()
{
    var secretBuilder = new StringBuilder();
    while (true)
    {
        var keyInfo = Console.ReadKey(intercept: true);
        if (keyInfo.Key == ConsoleKey.Enter)
        {
            break;
        }

        if (keyInfo.Key == ConsoleKey.Backspace && secretBuilder.Length > 0)
        {
            secretBuilder.Length--;
            Console.Write("\b \b");
        }
        else if (!char.IsControl(keyInfo.KeyChar))
        {
            secretBuilder.Append(keyInfo.KeyChar);
            Console.Write('*');
        }
    }

    return secretBuilder.ToString();
}

public class CredentialsStore
{
    private readonly List<CredentialSet> _store = new();
    private const string CredentialsFileName = "unicorns_and_rainbows.magic"; 
    public bool HasPendingChanges { get; private set; }
    
    public void Load()
    {
        if (!File.Exists(CredentialsFileName))
            return;

        using var fileStream = new FileStream(CredentialsFileName, FileMode.Open);
        byte[] decryptedData = DecryptDataFromStream(fileStream);
        
        fileStream.Close();
        
        var credentialSets = JsonSerializer.Deserialize<List<CredentialSet>>(decryptedData);

        if (credentialSets is null)
            throw new Exception("Failed at deserializing credentials");
        
        _store.Clear();
        _store.AddRange(credentialSets);
    }

    public void Save()
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(_store);
        using var fileStream = new FileStream(CredentialsFileName, FileMode.OpenOrCreate);
        EncryptDataToStream(data, fileStream);
        fileStream.Close();
    }
    
    public bool TryAdd(string name, Guid clientId, string encodedJwk)
    {
        if (_store.Any(set => set.Name == name))
            return false;
        
        _store.Add(new CredentialSet(name, clientId, encodedJwk));
        HasPendingChanges = true;
        return true;
    }

    public bool Remove(string name)
    {
        var success = _store.RemoveAll(set => set.Name == name) > 0;
        if (success)
            HasPendingChanges = true;
        return success;
    }

    public List<CredentialSet> GetAll() => _store;

    public CredentialSet? Get(string name)
    {
        return _store.FirstOrDefault(set => set.Name == name);
    }

    private static byte[] DecryptDataFromStream(Stream s)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return DecryptOnWindows(s);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return DecryptOnOsx(s);
        
        throw new InvalidOperationException("You OS is not supported");
    }

    private static byte[] DecryptOnOsx(Stream stream)
    {
        var dataProtectorProvider = DataProtectionProvider.Create("MaskinportenTokenGetter");

        var dataProtector = dataProtectorProvider.CreateProtector("MacOS");

        var inBuffer = new byte[stream.Length];

        var length = inBuffer.Length;

        if (!stream.CanRead)
            throw new IOException("Could not read the stream.");
        
        var readBytesCount = stream.Read(inBuffer, 0, length);

        if (readBytesCount != length)
            throw new Exception("Failed to read the stream");

        return dataProtector.Unprotect(inBuffer);
    }

    private static byte[] DecryptOnWindows(Stream s)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new InvalidOperationException("You OS is not supported");
        
        ArgumentNullException.ThrowIfNull(s);

        var inBuffer = new byte[s.Length];

        var length = inBuffer.Length;

        if (!s.CanRead)
            throw new IOException("Could not read the stream.");
        
        var readBytesCount = s.Read(inBuffer, 0, length);

        if (readBytesCount != length)
            throw new Exception("Failed to read the stream");
            
        return ProtectedData.Unprotect(inBuffer, null, DataProtectionScope.CurrentUser);
    }

    private static void EncryptDataToStream(byte[] buffer, Stream s)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            EncryptOnWindows(buffer, s);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            EncryptOnOsx(buffer, s);
        else
            throw new InvalidOperationException("You OS is not supported");
    }

    private static void EncryptOnOsx(byte[] buffer, Stream stream)
    {
        var dataProtectorProvider = DataProtectionProvider.Create("MaskinportenTokenGetter");
        var dataProtector = dataProtectorProvider.CreateProtector("MacOS");


        var encrypted = dataProtector.Protect(buffer);
        stream.Write(encrypted, 0, encrypted.Length);
    }

    private static void EncryptOnWindows(byte[] buffer, Stream s)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new InvalidOperationException("You OS is not supported");
        
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(s);
        
        if (buffer.Length <= 0)
            throw new ArgumentException("The buffer length was 0.", nameof(buffer));

        // Encrypt the data and store the result in a new byte array. The original data remains unchanged.
        var encryptedData = ProtectedData.Protect(buffer, null, DataProtectionScope.CurrentUser);
        
        if (!s.CanWrite) 
            return;

        // Write the encrypted data to a stream.
        s.Write(encryptedData, 0, encryptedData.Length);
    }
}

public record CredentialSet(string Name, Guid ClientId, string EncodedJwk);


