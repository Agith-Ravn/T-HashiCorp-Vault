using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VaultTest1.Model
{

    class Commands
    {
        public ConsoleKey consoleKey;
        private VaultService vault { get; set; }

        public Commands(VaultService vault)
        {
            this.vault = vault;
        }

        public void LoginInfo()
        {
            Console.WriteLine("Press L = Log into vault");
            Console.WriteLine("Press ESC = End the app");
            Console.WriteLine("\nPlease press a key");
        }

        public void Options()
        {
            Console.WriteLine("Press C = Create new key");
            Console.WriteLine("Press E = Encrypt a message");
            Console.WriteLine("Press D = Decrypt a message");
            Console.WriteLine("Press R = Rotate a Key");
            Console.WriteLine("Press X = Export key");
            Console.WriteLine("Press W = Rotate a key and warpped the new Key with the old key");
            Console.WriteLine("Press ESC = End the app");
            Console.WriteLine("\nPlease press a key");
        }

        public async Task CheckKey()
        {
            switch (consoleKey)
            {
                case ConsoleKey.L:
                    await LoginHandler();
                    break;

                case ConsoleKey.C:
                    await CreateNewKeyHandler();
                    break;

                case ConsoleKey.E:
                    await EncryptMessageHandler();
                    break;

                case ConsoleKey.D:
                    await DecryptMessageHandler();
                    break;

                case ConsoleKey.R:
                    await RotateAKeyHandler();
                    break;

                case ConsoleKey.X:
                    await ExportAKeyHandler();
                    break;

                case ConsoleKey.W:
                    await RotateAndWrapHandler();
                    break;

                default:
                    break;
            }
        }

        public async Task LoginHandler()
        {
            if (vault._sessionData == null)
            {
                var sessionData = await vault.Login();
                if (sessionData.auth.lease_duration != null)
                {
                    double seconds = sessionData.auth.lease_duration;
                    var timespan = TimeSpan.FromSeconds(seconds).ToString();

                    Console.WriteLine("Logged in");
                    Console.WriteLine("You will be logged out in " + timespan + "\n");
                }
                else
                {
                    Console.WriteLine("Something went wrong..try again later");
                }
            }
            else
            {
                Console.WriteLine("Already logged in");
            }
        }

        //Making a key with a already existing name/id will return status code 204 as well.. need to find a solution for this
        public async Task CreateNewKeyHandler()
        {
            Console.WriteLine("Please write down a name or a id for the new key");
            string keyName = Console.ReadLine().ToLower();
            int statusCode = await vault.CreateNewKey(keyName);
            if (statusCode == 204)
            {
                Console.Clear();
                Console.WriteLine("Key named " + keyName + " was created\n");
            }
            else
            {
                Console.WriteLine("Error - No key was created\n");
                Thread.Sleep(300);
                Console.Clear();
            }
        }

        //Use key1 to encrypt
        public async Task EncryptMessageHandler()
        {
            Console.WriteLine("Please write down the key name/id (This key will be used to encrypt message)");
            string keyName = Console.ReadLine().ToLower();
            Console.WriteLine("Please write a message to encrypt)");
            string message = Console.ReadLine();

            Payload payload = new Payload(message, "aes256-gcm96");
            var encryptedPayload = await vault.EncryptMessage(keyName, payload);

            if (encryptedPayload.plaintext != null)
            {
                Console.Clear();
                Console.WriteLine("Your ciphertext is : " + encryptedPayload.plaintext);
                Console.WriteLine("Make sure to save this a place!\n");
                return;
            }

            Console.WriteLine("Error - message was not encrypted\n");
            Thread.Sleep(300);
            Console.Clear();
        }

        //Use key1 to decrypt
        public async Task DecryptMessageHandler()
        {
            Console.WriteLine("Please write down the key name/id (This key will be used to decrypt message)");
            string keyName = Console.ReadLine().ToLower();

            Console.WriteLine("Please write down ciphertext)");
            string ciphertext = Console.ReadLine();

            Data payload = new Data(ciphertext);

            var decryptedPayload = await vault.DecryptMessage(keyName, payload);

            if (decryptedPayload.plaintext != null)
            {
                Console.Clear();
                Console.WriteLine("Your plaintext is : " + decryptedPayload.plaintext + "\n");
                return;
            }

            Console.WriteLine("Error - message was not decrypted\n");
            Thread.Sleep(300);
            Console.Clear();
        }

        //Use key1
        public async Task RotateAKeyHandler()
        {
            Console.WriteLine("Please write down the name/id of the key you want to rotate");
            string keyName = Console.ReadLine().ToLower();

            int statusCode = await vault.RotateAKey(keyName);
            if (statusCode == 204)
            {
                Console.Clear();
                Console.WriteLine("Key named " + keyName + " is now rotated\n");
            }
            else
            {
                Console.WriteLine("Error - No key was not rotated\n");
                Thread.Sleep(300);
                Console.Clear();
            }
        }

        public async Task ExportAKeyHandler()
        {
            Console.WriteLine("Please write down the name/id of the key you want to export");
            string keyName = Console.ReadLine().ToLower();
            var exportedKey = await vault.ExportAKey(keyName);
            if (exportedKey == null)
            {
                Console.WriteLine("Error - No key was exported\n");
                Thread.Sleep(300);
                Console.Clear();
            }
            else
            {
                Console.Clear();
                Console.WriteLine("Exporting " + keyName + "\nThe key is: +" + exportedKey + "\n");
            }
        }

        public async Task RotateAndWrapHandler()
        {
            Console.WriteLine("Please write down the name/id of the key you want to rotate");
            string keyName = Console.ReadLine().ToLower();

            //Rotate key
            int statusCode = await vault.RotateAKey(keyName);
            Console.Clear();
            Console.WriteLine("Key named " + keyName + " is now rotated\n");

            await EncryptNewKeyWithOldKey(keyName);

        }

        public async Task EncryptNewKeyWithOldKey(string keyName)
        {
            //Get the rotated key
            var newKey = await vault.ExportAKey(keyName);
            Console.Clear();
            Console.WriteLine(keyName + "is now rotated\n");

            //Get version of prevoius key
            var oldKeyVersion = vault.GetOldKeyVersion();
            if (oldKeyVersion != 0)
            {
                //Encrypt new key with old key
                Payload payload = new Payload(newKey, "aes256-gcm96", oldKeyVersion);
                var encryptedPayload = await vault.EncryptNewKeyWithOldKey(keyName, payload);

                if (encryptedPayload.plaintext != null)
                {
                    Console.Clear();
                    Console.WriteLine("Your ciphertext is : " + encryptedPayload.plaintext);
                    Console.WriteLine("Make sure to save this a place!\n");
                    return;
                }

                Console.WriteLine("Error - message was not encrypted\n");
                Thread.Sleep(300);
                Console.Clear();
            }
        }
    }
}
