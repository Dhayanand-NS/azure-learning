using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using System.Net.Http.Headers;

namespace AzureMsEnrta.Client.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenAcquisition _tokenAcquisition;

        public IndexModel(IHttpClientFactory httpClientFactory, ITokenAcquisition tokenAcquisition)
        {
            _httpClientFactory = httpClientFactory;
            _tokenAcquisition = tokenAcquisition;
        }
        public async Task OnGet()
        {
            // Step 1 - Get the access token
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[]
                {
                  "api://304873b4-5ca4-4c33-9b58-905a709f42a9/AllAccess"
                });

            // Step 2 - Create HTTP client
            var httpClient = _httpClientFactory.CreateClient("default");

            // Step 3 - Create request to API
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7076/WeatherForecast");

            // Step 4 - Attach token as proof!
            request.Headers.Authorization = new AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme, token);

            // Step 5 - Send request
            var response = await httpClient.SendAsync(request);
        }
    }
}
