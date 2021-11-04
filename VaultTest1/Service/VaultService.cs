using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VaultTest1.Model;

namespace VaultTest1
{
    class VaultService
    {
        private LoginData _loginData;
        public bool login { get; set; }
        private HttpClient _httpClient { get; set; }
        public SessionData _sessionData { get; private set; }

        private ExportKeyResponse _EKR { get; set; }

        public VaultService(string filepath)
        {
            _loginData = JsonToObject(filepath);
            login = false;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_loginData.baseURI);

        }

        public LoginData JsonToObject(string filePath)
        {
            var jsonString = File.ReadAllText(filePath);
            var loginData = JsonSerializer.Deserialize<LoginData>(jsonString);
            return loginData;
        }

        public async Task<SessionData> Login()
        {
            var loginData = new LoginData()
            {
                role_id = _loginData.role_id,
                secret_id = _loginData.secret_id
            };

            var serializedLoginData = JsonSerializer.Serialize(loginData);

            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/auth/approle/login");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(serializedLoginData);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _sessionData = JsonSerializer.Deserialize<SessionData>(content);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _sessionData.auth.client_token);
                login = true;
                return _sessionData;
            }
            else
            {
                var errormessage = await response.Content.ReadAsStringAsync();
                var errorcode = response.StatusCode;
                Console.WriteLine($"Error {(int)errorcode}: \nMessage: {errormessage}");
                return new SessionData();
            }
        }

        public async Task<int> CreateNewKey(string keyName)
        {

            var key = new Key()
            {
                exportable = true,
                type = "aes256-gcm96"
                //May need to change this later..
            };

            var serializedKey = JsonSerializer.Serialize(key);

            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/transit/keys/" + keyName);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(serializedKey);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(request);
            int statusCode = (int)response.StatusCode;
            return statusCode;
        }

        public async Task<Payload> EncryptMessage(string keyName, Payload payload)
        {
            var plaintext64 = Base64Encode(payload.plaintext);
            payload.plaintext = plaintext64;

            var serializedPayload = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/transit/encrypt/" + keyName);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(serializedPayload);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                EncryptedPayload er = JsonSerializer.Deserialize<EncryptedPayload>(content);
                return new Payload(er.data.ciphertext, payload.type);
            }
            else
            {
                var errormessage = await response.Content.ReadAsStringAsync();
                var errorcode = response.StatusCode;
                Console.WriteLine($"Error {(int)errorcode}: \nMessage: {errormessage}");
                return new Payload();
            }

        }
        private string Base64Encode(string plain)
        {
            var plaintextbytes = Encoding.UTF8.GetBytes(plain);
            return Convert.ToBase64String(plaintextbytes);
        }

        public async Task<Payload> DecryptMessage(string keyName, Data payload)
        {
            var serializedPayload = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/transit/decrypt/" + keyName);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(serializedPayload);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                DecryptedPayload dp = JsonSerializer.Deserialize<DecryptedPayload>(content);
                return new Payload(Base64Decode(dp.data.plaintext));
            }
            else
            {
                var errormessage = await response.Content.ReadAsStringAsync();
                var errorcode = response.StatusCode;
                Console.WriteLine($"Error {(int)errorcode}: \nMessage: {errormessage}");
                return new Payload();
            }
        }

        private string Base64Decode(string code)
        {
            var base64EncodedBytes = Convert.FromBase64String(code);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public async Task<int> RotateAKey(string keyName)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "v1/transit/keys/"+ keyName +"/rotate");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            int statusCode = (int)response.StatusCode;
            return statusCode;
        }

        public async Task<string> ExportAKey(string keyName)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/v1/transit/export/encryption-key/" + keyName);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _EKR = JsonSerializer.Deserialize<ExportKeyResponse>(content);

                //Hente den nyeste nøkkelen fra dictionary
                string Exportedkey = GetNewestKey(_EKR.data.keys);
                return Exportedkey;
            }
            else
            {
                var errormessage = await response.Content.ReadAsStringAsync();
                var errorcode = response.StatusCode;
                Console.WriteLine($"Error {(int)errorcode}: \nMessage: {errormessage}");
                return "";
            }
        }

        public static V GetNewestKey<K, V>(Dictionary<K, V> dict)
        {
            for (int i = 0; i < dict.Count; i++)
            {
                KeyValuePair<K, V> entry = dict.ElementAt(i);
                if (i == (dict.Count - 1))
                {
                    return entry.Value;
                }
            }
            Dictionary<K, V> emptyDicitionary = new Dictionary<K, V>();
            return emptyDicitionary.ElementAt(0).Value;
        }

        public int GetOldKeyVersion()
        {
            for (int i = 0; i < _EKR.data.keys.Count; i++)
            {
                if (i == (_EKR.data.keys.Count - 2))
                {
                    return i;
                }
            }
            return 0;

        }


        public async Task<Payload> EncryptNewKeyWithOldKey(string keyName, Payload payload)
        {
            var newKey64 = Base64Encode(payload.plaintext);
            payload.plaintext = newKey64;

            var serializedPayload = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/transit/encrypt/" + keyName);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(serializedPayload);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                EncryptedPayload er = JsonSerializer.Deserialize<EncryptedPayload>(content);
                return new Payload(er.data.ciphertext, payload.type);
            }
            else
            {
                var errormessage = await response.Content.ReadAsStringAsync();
                var errorcode = response.StatusCode;
                Console.WriteLine($"Error {(int)errorcode}: \nMessage: {errormessage}");
                return new Payload();
            }

        }

    }
}
