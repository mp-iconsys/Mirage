using System;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace Mirage
{   
    class Program
    {
        // Debug Level for logging purposes
        static int debugLevel = 0;
        const string URL = "http://localhost/api/v2.0.0/";
        
        // Open and read a config file???

        // Authentication String
        //var authValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

        // By declaring a single shared static we reduce the number of sockets
        private static HttpClient Client = new HttpClient();

        public static async Task Main(string[] args)
        {
            //============================================= 
            //  Authentication Area
            //=============================================
            // The authentication type is basic. Set up as follows:
            // BASE64( username: sha256(pass) )
            // So, first get sha256 of the pass
            // Concat to "username:"
            // Do base64 encoding
            string username = "admin";
            string password = "admin";
            string hashedPassword = ComputeSha256Hash(password);
            string userAndPass = username + ":" + hashedPassword;
            string authString = Convert.ToBase64String(Encoding.UTF8.GetBytes(userAndPass));
            var authValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{ComputeSha256Hash(password)}")));

            //============================================= 
            // Default HttpClient Connection Details
            //============================================= 
            //string connectionIP = "localhost";
            //string uri = connectionIP + "/api/v2.0.0/";

            Client.BaseAddress = new Uri(URL);
            
            //Client.BaseAddress = new Uri("http://localhost/api/v2.0.0/"); // insert base address here
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestVersion = HttpVersion.Version11;
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Add("Accept-Language", "en_US");
            Client.DefaultRequestHeaders.Authorization = authValue;
            string urlParameters = "?api_key=123";

            Client.Timeout = TimeSpan.FromMinutes(10);

            /*
             * Depreciated - create authenticationheader outside 
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(userAndPass))
                    );
            */

            if (debugLevel > 0)
            {
                Console.WriteLine("Username and Password: %s, %s", username, password);
                Console.WriteLine("After Hashing:");
                Console.WriteLine(authString);
            }

            if (debugLevel > 0)
            {
                Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{ComputeSha256Hash(password)}")));
            }

            /* 
             * 
             * Checks connection works
             * 
             * 
            Console.WriteLine("Starting connections");
            for (int i = 0; i < 10; i++)
            {
                var result = await Client.GetAsync("http://aspnetmonsters.com");
                Console.WriteLine(result.StatusCode);
            }
            Console.WriteLine("Connections done");
            */

            try
            {
                var call = await Client.GetAsync(urlParameters);
                if (call.IsSuccessStatusCode)
                {
                    // Take the returned data and save in DB

                    SaveCustomObjectToDB();
                    // Decode Here
                    // Save
                }
                else
                {
                    // Print error here
                }
            }
            catch (WebException e)
            {
                Console.WriteLine($"Connection Problems: '{e}'");
            }


            /*
            HttpResponseMessage response = Client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body.
                var dataObjects = response.Content.ReadAsAsync<IEnumerable<DataObject>>().Result;  //Make sure to add a reference to System.Net.Http.Formatting.dll
                foreach (var d in dataObjects)
                {
                    Console.WriteLine("{0}", d.Name);
                }
            }
            else
            {
                Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            */


            //Make any other calls using HttpClient here.

            //Dispose once all HttpClient calls are complete. This is not necessary if the containing object will be disposed of; for example in this case the HttpClient instance will be disposed automatically when the application terminates so the following call is superfluous.
            Client.Dispose();

            
            Console.ReadLine();
        }

        static void SaveCustomObjectToDB()
        {
            // Decode object and save to DB
        }


        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

    }
}

