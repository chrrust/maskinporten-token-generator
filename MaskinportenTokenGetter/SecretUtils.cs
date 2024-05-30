using System.Text;

namespace MaskinportenTokenGetter;

public static class SecretUtils
{
    public static string ReadSecret()
    {
        var secretBuilder = new StringBuilder();
        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (keyInfo.Key == ConsoleKey.Backspace && secretBuilder.Length > 0)
            {
                secretBuilder.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                secretBuilder.Append(keyInfo.KeyChar);
                Console.Write('*');
            }
        }

        return secretBuilder.ToString();
    }
}