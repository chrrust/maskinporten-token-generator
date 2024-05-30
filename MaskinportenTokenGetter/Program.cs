using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using MaskinportenTokenGetter;
using MaskinportenTokenGetter.Commands.Credentials;
using MaskinportenTokenGetter.Commands.Generate;

CredentialsStore.Load();

var rootCommand = new RootCommand();

rootCommand.AddGenerateCommand();
rootCommand.AddCredentialsCommands();

var commandLineBuilder = new CommandLineBuilder(rootCommand);

commandLineBuilder.UseDefaults();

var parser = commandLineBuilder.Build();

await parser.InvokeAsync(args);

if (CredentialsStore.HasPendingChanges)
    CredentialsStore.Save();
    