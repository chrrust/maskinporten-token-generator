namespace MaskinportenTokenGetter.Commands.Credentials;

public static class ListCredentialsCommand
{
    public static void Handle()
    {
        var credentialSets = CredentialsStore.GetAll().ToArray();
    
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
}