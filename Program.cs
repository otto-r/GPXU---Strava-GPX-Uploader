using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace StravaGPXU
{
    public class Program
    {
        static readonly HttpClient client = new HttpClient();


        static async Task Main()
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("config.json");

            var config = builder.Build();
            var uploadCounter = 0;
            try
            {
                string path = Directory.GetCurrentDirectory() + @"\gpx";

                string[] files = Directory.GetFiles(path);
                var counter = 0;
                foreach (var file in files)
                {

                    var requestUri = "https://www.strava.com/api/v3/uploads";
                    var formContent = new MultipartFormDataContent();

                    string text = File.ReadAllText(file);

                    string patternstart = Regex.Escape("<name><![CDATA[");
                    string patternend = Regex.Escape(" ");
                    string regexexpr = patternstart + @"(.*?)" + patternend;

                    MatchCollection matches = Regex.Matches(text, @regexexpr);
                    var match = matches.Any() ? matches.FirstOrDefault().ToString() : "";

                    var nameOfActivity = !string.IsNullOrEmpty(match) ? match.Split(new string[] { "<name><![CDATA[" }, StringSplitOptions.None)[1].Split(' ')[0].Trim() : "";
                    var nameOfFile = file.Split(@"\").Last();

                    if (!file.EndsWith(".gpx", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.Write("Invalid ext: ");
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine($" Activity: {nameOfActivity}, File: {nameOfFile}");
                        continue;
                    }

                    counter++;

                    if (counter == 600)
                    {
                        Console.WriteLine("Reached Strava 15 min upload limit (of 600). Sleeping for 900 sec (15 minutes)");
                        for (int seconds = 900; seconds >= 0; seconds--)
                        {
                            Console.CursorLeft = 1;
                            Console.Write("{0} ", seconds);
                            Thread.Sleep(1000);
                        }
                        counter = 0;
                    }
                    var stream = new FileStream(file, FileMode.Open);


                    formContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");

                    formContent.Add(new StreamContent(stream), "file", "file");
                    formContent.Add(new StringContent(nameOfActivity + " " + config["appendedString"]), "name");
                    formContent.Add(new StringContent("gpx"), "data_type");
                    formContent.Add(new StringContent(""), "description");

                    var accessToken = config["accessToken"];

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    HttpResponseMessage response = await client.PostAsync(requestUri, formContent);
                    response.EnsureSuccessStatusCode();


                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.Write("Uploaded:    ");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine($" Activity: {nameOfActivity}, File: {nameOfFile}");
                    uploadCounter++;
                }
                Console.WriteLine($"Number of files uploaded: {uploadCounter}");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }
    }
}