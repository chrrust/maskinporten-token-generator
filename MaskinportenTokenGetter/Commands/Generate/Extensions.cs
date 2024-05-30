using System.CommandLine;

namespace MaskinportenTokenGetter.Commands.Generate;

public static class Extensions
{
    public static void AddGenerateCommand(this Command parentCommand)
    {
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

        generateCommand.SetHandler(GenerateCommand.Handle, tokenTypeOption, scopesOption, environmentOption, credentialsOption);
        
        parentCommand.AddCommand(generateCommand);
    }
}