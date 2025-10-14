using AIFactory.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AIFactory.Util
{
    public class MESManager
    {
        private string _postAddress = "https://jsonplaceholder.typicode.com/posts";

        public string PostAddress
        {
            get { return _postAddress; }
            set { _postAddress = value; }
        }


        private readonly HttpClient _httpClient = new HttpClient();

        public async Task SendPostLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var json = JsonSerializer.Serialize(new { timestamp = DateTime.Now.ToString("o") });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = await _httpClient.PostAsync(PostAddress, content, token);
                    var responseText = await response.Content.ReadAsStringAsync();

                    //Dispatcher.Invoke(() =>
                    //{
                    //    ResponseBlock.Text = $"Last response:\n{responseText.Substring(0, Math.Min(300, responseText.Length))}...";
                    //});
                }
                catch (OperationCanceledException)
                {
                    // Graceful exit
                    break;
                }
                catch (Exception ex)
                {
                    //Dispatcher.Invoke(() =>
                    //{
                    //    ResponseBlock.Text = $"Error: {ex.Message}";
                    //});
                    break;
                }

                await Task.Delay(1000, token); // Wait 1 second
            }
        }


        public async Task<DataRealTime> GetDataAsync()
        {
            DataRealTime lstData = new DataRealTime();

            var json = JsonSerializer.Serialize(new { timestamp = DateTime.Now.ToString("o") });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(PostAddress, content);
                var responseText = await response.Content.ReadAsStringAsync();

                //Dispatcher.Invoke(() =>
                //{
                //    ResponseBlock.Text = $"Last response:\n{responseText.Substring(0, Math.Min(300, responseText.Length))}...";
                //});
                lstData = new DataRealTime();
            }
            catch (OperationCanceledException)
            {
                // Graceful exit
            }
            catch (Exception ex)
            {
                //Dispatcher.Invoke(() =>
                //{
                //    ResponseBlock.Text = $"Error: {ex.Message}";
                //});
            }
            return lstData;

        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }

    public class MESClient
    {
        
        private string Url = "https://testapi.jasonwatmore.com/products/1";
        public MESClient(string url)
        {
            Url = url;
        }
        private static readonly HttpClient client = new HttpClient();

        public async Task<string> FetchDataAsync()
        {
            HttpResponseMessage response = await client.GetAsync(Url);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return json;
        }
    }
}
