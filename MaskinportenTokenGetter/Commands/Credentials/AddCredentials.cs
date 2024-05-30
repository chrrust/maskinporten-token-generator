namespace MaskinportenTokenGetter.Commands.Credentials;

public static class AddCredentialsCommand
{
    public static void Handle(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    
        Console.WriteLine("Enter client id:");
    
        if (!Guid.TryParse(Console.ReadLine(), out var clientId))
            throw new ArgumentException("Failed to parse client id", nameof(clientId));
    
        Console.WriteLine("Enter encoded JWK:");
        var encodedJwk = SecretUtils.ReadSecret();

        if (clientId == Guid.Empty)
            throw new ArgumentException("clientId must be an initialized guid.", nameof(clientId));
    
        ArgumentException.ThrowIfNullOrWhiteSpace(encodedJwk);
    
        var success = CredentialsStore.TryAdd(name, clientId, encodedJwk);

        if (!success)
            Console.WriteLine("Failed to set credentials");
    }
}