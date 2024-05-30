using System.Text.Json;

namespace MaskinportenTokenGetter;

public record CredentialSet(string Name, Guid ClientId, string EncodedJwk);

public static class CredentialsStore
{
    private static readonly List<CredentialSet> Store = new();
    private const string CredentialsFileName = "unicorns_and_rainbows.magic"; 
    public static bool HasPendingChanges { get; private set; }
    
    public static void Load()
    {
        if (!File.Exists(CredentialsFileName))
            return;

        using var fileStream = new FileStream(CredentialsFileName, FileMode.Open);
        var decryptedData = Encryptor.DecryptDataFromStream(fileStream);
        
        fileStream.Close();
        
        var credentialSets = JsonSerializer.Deserialize<List<CredentialSet>>(decryptedData);

        if (credentialSets is null)
            throw new Exception("Failed at deserializing credentials");
        
        Store.Clear();
        Store.AddRange(credentialSets);
    }

    public static void Save()
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(Store);
        using var fileStream = new FileStream(CredentialsFileName, FileMode.OpenOrCreate);
        Encryptor.EncryptDataToStream(data, fileStream);
        fileStream.Close();
    }
    
    public static bool TryAdd(string name, Guid clientId, string encodedJwk)
    {
        if (Store.Any(set => set.Name == name))
            return false;
        
        Store.Add(new CredentialSet(name, clientId, encodedJwk));
        HasPendingChanges = true;
        return true;
    }

    public static bool Remove(string name)
    {
        var success = Store.RemoveAll(set => set.Name == name) > 0;
        if (success)
            HasPendingChanges = true;
        return success;
    }

    public static List<CredentialSet> GetAll() => Store;

    public static CredentialSet? Get(string name)
    {
        return Store.FirstOrDefault(set => set.Name == name);
    }

}