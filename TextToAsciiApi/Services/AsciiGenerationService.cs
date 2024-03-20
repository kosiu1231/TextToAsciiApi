using Newtonsoft.Json;
using System.Drawing;
using System.Runtime.Versioning;
using TextToAsciiApi.Models;
using static System.Net.Mime.MediaTypeNames;

namespace TextToAsciiApi.Services
{
    public class AsciiGenerationService
    {
        private readonly IConfiguration _configuration;

        public AsciiGenerationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<PexelsImage> GenerateArt(GenerationRequest request)
        {
            try
            {
                PexelsImage imageObj = await FetchData(request.Value);

                byte[]? imageData = null;

                using (var client = new HttpClient())
                {
                    imageData = await client.GetByteArrayAsync(imageObj.ImageSrcUrl);
                }

                using (var stream = new System.IO.MemoryStream(imageData))
                using (var image = new Bitmap(stream))
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            Color pixelColor = image.GetPixel(x, y);

                            int brightness = (int)(0.21 * pixelColor.R + 0.72 * pixelColor.G + 0.07 * pixelColor.B);
                            //int brightness = (int)(0.299 * pixelColor.R + 0.587 * pixelColor.G + 0.114 * pixelColor.B);

                            imageObj.Art += GetAsciiCharacter(brightness);
                        }

                        if(y != image.Height-1)
                            imageObj.Art += "<br>";
                    }
                }

                return imageObj;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not generate ASCII: " + ex.Message);
            }
        }

        private async Task<PexelsImage> FetchData(string input)
        {
            string baseUrl = "https://api.pexels.com/v1/search";
            var apiKey = _configuration.GetSection("Pexels:ApiKey").Value;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", apiKey);

                string requestUrl = $"{baseUrl}?locale=en-US&per_page=1&query={input}";
                HttpResponseMessage response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(responseBody)!;
                    var image = new PexelsImage
                    {
                        ImageUrl = data.photos[0].url,
                        ImageSrcUrl = data.photos[0].src.tiny,
                        Photographer = data.photos[0].photographer
                    };

                    return image;
                }
                else
                {
                    throw new HttpRequestException($"HTTP request failed: {response.StatusCode}, {response.ReasonPhrase}");
                }
            }
        }

        private const string Black = "@";
        private const string Charcoal = "#";
        private const string Darkgray = "8";
        private const string Mediumgray = "&";
        private const string Medium = "o";
        private const string Gray = ":";
        private const string Slategray = "*";
        private const string Lightgray = ".";
        private const string White = " ";

        static string GetAsciiCharacter(int brightness)
        {
            // Map brightness to ASCII characters
            if (brightness >= 230)
                return White;
            else if (brightness >= 200)
                return Lightgray;
            else if (brightness >= 180)
                return Slategray;
            else if (brightness >= 150)
                return Gray;
            else if (brightness >= 120)
                return Medium;
            else if (brightness >= 100)
                return Mediumgray;
            else if (brightness >= 80)
                return Darkgray;
            else if (brightness >= 50)
                return Charcoal;
            else
                return Black;
        }
    }
}
