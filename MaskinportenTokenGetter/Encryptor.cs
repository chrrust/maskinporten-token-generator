using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace MaskinportenTokenGetter;

public static class Encryptor
{
    private const string ApplicationName = "MaskinportenTokenGetter";
    private const string MacOsProtectionPurpose = "Store credentials for Maskinporten";
    
    public static void EncryptDataToStream(byte[] buffer, Stream s)
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
        var dataProtectorProvider = DataProtectionProvider.Create(ApplicationName);
        var dataProtector = dataProtectorProvider.CreateProtector(MacOsProtectionPurpose);


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
    
    public static byte[] DecryptDataFromStream(Stream s)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return DecryptOnWindows(s);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return DecryptOnOsx(s);
        
        throw new InvalidOperationException("You OS is not supported");
    }

    private static byte[] DecryptOnOsx(Stream stream)
    {
        var dataProtectorProvider = DataProtectionProvider.Create(ApplicationName);

        var dataProtector = dataProtectorProvider.CreateProtector(MacOsProtectionPurpose);

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
}