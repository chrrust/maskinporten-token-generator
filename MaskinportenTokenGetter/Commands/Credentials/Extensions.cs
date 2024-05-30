using System.CommandLine;

namespace MaskinportenTokenGetter.Commands.Credentials;

public static class Extensions
{
    public static void AddCredentialsCommands(this Command parentCommand)
    {
        // Make credentials root command 
        var credentialsCommand = new Command("credentials");

        // Setup "credentials add" command
        var addCredentialsCommand = new Command("add");
        var addCredentialsNameArgument = new Argument<string>("name");
        addCredentialsCommand.AddArgument(addCredentialsNameArgument);
        addCredentialsCommand.SetHandler(AddCredentialsCommand.Handle, addCredentialsNameArgument);
        credentialsCommand.AddCommand(addCredentialsCommand);

        // Setup "credentials remove" command
        var removeCredentialsCommand = new Command("remove");
        var removeCredentialsNameArgument = new Argument<string>("name");
        removeCredentialsCommand.AddArgument(removeCredentialsNameArgument);
        removeCredentialsCommand.SetHandler(RemoveCredentialsCommand.Handle, removeCredentialsNameArgument);
        credentialsCommand.AddCommand(removeCredentialsCommand);

        // Setup "credentials list" command
        var listCredentialsCommand = new Command("list");
        listCredentialsCommand.SetHandler(ListCredentialsCommand.Handle);
        credentialsCommand.AddCommand(listCredentialsCommand);
        
        // Add sub commands to "credentials" command
        parentCommand.AddCommand(credentialsCommand);
    }
}