# MaskinportenTokenGetter

This application can be used to create a MaskinportenToken.

Steps to create a token:

1. Add user secrets for the client you want to use to create the Maskinporten token
   1. `dotnet user-secrets set "ClientId" "<value>"`. This is the identifier (Integrasjonsid) for the Maskinporten client which is set up in samarbeidsportalen
   2. `dotnet user-secrets set "EncodedJwk" "<value>"`. This should be the public and private keypair base64 encoded
2. `dotnet build`
3. `dotnet run`