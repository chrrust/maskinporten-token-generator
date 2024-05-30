using System.CommandLine;

namespace MaskinportenTokenGetter.Commands.Generate;

public static class Extensions
{
    public static void AddGenerateCommand(this Command parentCommand)
    {
        var generateCommand = new Command("generate");

        // Add scopes option
        var scopesOption = new Option<string[]>("--scopes")
        {
            AllowMultipleArgumentsPerToken = true
        };
        scopesOption.AddAlias("-s");
        generateCommand.AddOption(scopesOption);

        // Add token type options
        var tokenTypeOption = new Option<string>("--type")
        {
            IsRequired = true
        };
        tokenTypeOption.AddAlias("-t");
        tokenTypeOption.FromAmong("maskinporten", "altinn");
        generateCommand.AddOption(tokenTypeOption);

        // Add credentials option
        var credentialsOption = new Option<string>("--credentials")
        {
            IsRequired = true
        };
        credentialsOption.AddAlias("-c");
        generateCommand.AddOption(credentialsOption);

        // Add environment option
        var environmentOption = new Option<string>("--environment")
        {
            IsRequired = true
        };
        environmentOption.AddAlias("-e");
        environmentOption.FromAmong("test", "prod");
        generateCommand.AddOption(environmentOption);
        
        generateCommand.SetHandler(GenerateCommand.Handle, tokenTypeOption, scopesOption, environmentOption, credentialsOption);
        
        parentCommand.AddCommand(generateCommand);
    }
}