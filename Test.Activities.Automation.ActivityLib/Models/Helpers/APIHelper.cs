using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Test.Activities.Automation.ActivityLib.Models.Helpers
{
    public static class APIHelper
    {
        public static IEnumerable<T> GetApiCollection<T>(string url, HttpClient client) where T : class
        {
            var response = client.GetAsync(url).Result;
            var stream = response.Content.ReadAsStreamAsync().Result;

            var res = new List<T>();

            var reader = new StreamReader(stream);
            stream.Position = 0;

            var ch = reader.Read();
            if (ch == '[')
            {
                var multiple = JsonDeserialize<T[]>(stream);
                res.AddRange(multiple);

                return res;
            }

            var single = JsonDeserialize<T>(stream);
            res.Add(single);

            return res;
        }

        public static T JsonDeserialize<T>(Stream stream) where T : class
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            stream.Position = 0;
            var res = serializer.ReadObject(stream) as T;
            return res;
        }
    }
}
