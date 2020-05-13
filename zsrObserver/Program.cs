using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace zsrObserver
{
    class Program
    {
        static string data = "{\"jsonrpc\":\"2.0\",\"method\":\"GetTrainDelaySimple\",\"params\":[],\"id\":1}";
        static string contentType = "application/json";

        static async Task Main(string[] args)
        {            
            string response = await PostAsync("http://mapa.zsr.sk/json.rpc", data, contentType);            
            JObject json = JObject.Parse(response);   

            JArray items = (JArray)json["result"];

            Dictionary<string, int> trainAndDelay = new Dictionary<string, int>();            
            Dictionary<Dictionary<string, string>, int> stationAndDelay = new Dictionary<Dictionary<string, string>, int>();

            Dictionary<Tuple<TestTuple>, int> lookup = new Dictionary<Tuple<TestTuple>, int>();
            string nazov;
            string stanica;

            for (int i = 0; i < items.Count; i++)
            {
                nazov = (string)json["result"][i]["Nazov"];
                stanica = (string)json["result"][i]["InfoZoStanice"];
                Console.WriteLine(json["result"][i]["Nazov"]);
                Console.WriteLine(json["result"][i]["Meska"]);
                Console.WriteLine(json["result"][i]["InfoZoStanice"]);
                Console.WriteLine("\n");

                trainAndDelay.Add((string)json["result"][i]["Nazov"], (int)json["result"][i]["Meska"]);

                Dictionary<string, string> trainAndStation = new Dictionary<string, string> { { (string)json["result"][i]["Nazov"], (string)json["result"][i]["InfoZoStanice"] } };               
                stationAndDelay.Add(trainAndStation, (int)json["result"][i]["Meska"]);
                //stationAndDelay.Add(new Dictionary<string, string> { { (string)json["result"][i]["Nazov"], (string)json["result"][i]["InfoZoStanice"] } }, (int)json["result"][i]["Meska"]);

                //lookup.Add(new Tuple<string, string> ((string)json["result"][i]["Nazov"], (string)json["result"][i]["InfoZoStanice"]), (int)json["result"][i]["Meska"]);
                lookup.Add(new Tuple<TestTuple>(new TestTuple(nazov, stanica)), (int)json["result"][i]["Meska"]);
            }

            var delayedTrains = await GetDelay(trainAndDelay, 10);
            Console.WriteLine("Trains with delayied");
            foreach (var train in delayedTrains)
            {
                Console.WriteLine(train);
            }

            Console.WriteLine("\n");

            List<Dictionary<string, string>> stationDelay = await GetDelayInStation(stationAndDelay, 10);
            Console.WriteLine("Trains in station with delay");
            foreach (Dictionary<string, string> trainsStations in stationDelay)
            {
                foreach (KeyValuePair<string, string> kvp in trainsStations)
                {
                    Console.WriteLine(kvp.Key + " " + kvp.Value);                                    
                }                
            }

            Console.WriteLine("\n");

            List<Tuple<TestTuple>> stationDelayTuple = await GetDelayInStationAndTrainTuple(lookup, 10);
            Console.WriteLine("Trains in station with delay");
            foreach (Tuple<TestTuple> trainsStations in stationDelayTuple)
            {
                Console.WriteLine(trainsStations.Item1.Nazov + " " + trainsStations.Item1.InfoZoStanice);              
            }


        }

        public static async Task<List<Tuple<TestTuple>>> GetDelayInStationAndTrainTuple(Dictionary<Tuple<TestTuple>, int> DelayInStationAndTrainTuple, int delay)
        {
            var evenFrenchNumbers = DelayInStationAndTrainTuple.Where(p => p.Value > delay).Select(p => p.Key);

            return await Task.FromResult(evenFrenchNumbers.ToList());
        }


        public static async Task<List<string>> GetDelay(Dictionary<string, int> trainAndDelay, int delay)
        {
            //var evenFrenchNumbers = from entry in trainAndDelay where (entry.Value > 10) select entry.Key;
            var evenFrenchNumbers = trainAndDelay.Where(p => p.Value > delay).Select(p => p.Key);

            return await Task.FromResult(evenFrenchNumbers.ToList());           
        }

        public static async Task<List<Dictionary<string, string>>> GetDelayInStation(Dictionary<Dictionary<string, string>, int> stationAndDelay, int delay)
        {           
            var evenFrenchNumbers = stationAndDelay.Where(p => p.Value > delay).Select(p => p.Key);

            return await Task.FromResult(evenFrenchNumbers.ToList());
        }
        

        public static async Task<string> PostAsync(string uri, string data, string contentType, string method = "POST")
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = contentType;
            request.Method = method;

            using (Stream requestBody = request.GetRequestStream())
            {
                await requestBody.WriteAsync(dataBytes, 0, dataBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {             
                return await reader.ReadToEndAsync();
            }
        }
    }
    public class TestTuple
    {
        public string Nazov { get; set; }
        public string InfoZoStanice { get; set; }

        public TestTuple(string nazov, string infoZoStanice)
        {
            Nazov = nazov;
            InfoZoStanice = infoZoStanice;
        }       
    }
}
