using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultTest1.Model
{
    public class SessionData
    {
        public string request_id { get; set; }
        public Auth auth { get; set; }
    }

    public class Auth
    {
        public string client_token { get; set; }
        public string accessor { get; set; }
        public int lease_duration { get; set; }
    }

    public class Key
    {
        public bool exportable { get; set; }
        public string type { get; set; }
    }

    public class Payload
    {
        public string plaintext { get; set; }
        public string type { get; set; }
        public int key_version { get; set; }

        public Payload(string plaintext, string type, int key_version)
        {
            this.plaintext = plaintext;
            this.type = type;
            this.key_version = key_version;
        }
        public Payload(string plaintext, string type)
        {
            this.plaintext = plaintext;
            this.type = type;
        }

        public Payload(string plaintext)
        {
            this.plaintext = plaintext;
        }

        public Payload()
        {
        }
    }

    public class EncryptedPayload
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public string ciphertext { get; set; }

        public Data(string ciphertext)
        {
            this.ciphertext = ciphertext;
        }
    }

    public class DecryptedPayload
    {
        public Data2 data { get; set; }
    }

    public class Data2
    {
        public string plaintext { get; set; }

        public Data2(string plaintext)
        {
            this.plaintext = plaintext;
        }
    }

    public class ExportKeyResponse
    {
        public Data3 data { get; set; }
    }
    public class Data3
    {
        public string name { get; set; }
        public Dictionary <int, string> keys { get; set; }
    }
}
    
