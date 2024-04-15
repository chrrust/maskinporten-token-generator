# MaskinportenTokenGetter

This application can be used to create a MaskinportenToken.

Steps to create a token:

1. `cd MaskinportenTokenGetter`
2. Add user secrets for the client you want to use to create the Maskinporten token
   1. `dotnet user-secrets set "ClientId" "<value>"`. This is the identifier (Integrasjonsid) for the Maskinporten client which is set up in samarbeidsportalen
   2. `dotnet user-secrets set "EncodedJwk" "<value>"`. This should be the public and private keypair base64 encoded
3. `dotnet build`
4. `dotnet run maskinporten <scope>` add wanted scope e.g `dgm:admin`