using System.Text.Json;

namespace MaskinportenTokenGetter;

public record CredentialSet(string Name, Guid ClientId, string EncodedJwk);

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
        var decryptedData = Encryptor.DecryptDataFromStream(fileStream);
        
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
        Encryptor.EncryptDataToStream(data, fileStream);
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

}