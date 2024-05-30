namespace MaskinportenTokenGetter.Commands.Credentials;

public static class RemoveCredentialsCommand
{
    public static void Handle(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var success = CredentialsStore.Remove(name);
    
        if (!success)
            Console.WriteLine("Failed to remove credentials");
    }
}