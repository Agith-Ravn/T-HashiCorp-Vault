using System;
using System.Threading;
using System.Threading.Tasks;
using VaultTest1.Model;

namespace VaultTest1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string path = @"..\..\..\Model\LoginData.json";
            var vault = new VaultService(path);
            var command = new Commands(vault);

            Console.WriteLine("Ziot Solutions - Vault API test console app\n");
            do
            {
                Console.WriteLine("You have the following options:");

                if (!vault.login)
                {
                    command.LoginInfo();
                }
                else
                {
                    command.Options();

                }

                command.consoleKey = Console.ReadKey().Key;
                Console.Clear();
                await command.CheckKey();

            } while (command.consoleKey != ConsoleKey.Escape);
        }
    }
}
