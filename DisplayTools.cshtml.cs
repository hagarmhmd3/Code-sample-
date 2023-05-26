using DentistPortal_Client.DTO;
using DentistPortal_Client.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace DentistPortal_Client.Pages.DoctorPages.Tools
{
    public class DisplayToolsModel : PageModel
    {
        private IHttpClientFactory _httpClient;
        IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json")
              .AddEnvironmentVariables()
              .Build();
        [TempData]
        public string Msg { get; set; } = String.Empty;
        [TempData]
        public string Status { get; set; } = String.Empty;
        public List<Tool> Tools = new();
        public string[] Pictures { get; set; }
        public Tool tool1;
        public DisplayToolsModel(IHttpClientFactory httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task OnGet()
        {
            var client = _httpClient.CreateClient();
            client.BaseAddress = new Uri(config["BaseAddress"]);
            if (HttpContext.Session.GetString("Token") == null)
            {
                Response.Redirect($"https://localhost:7156/Login?url={"DoctorPages/Tools/DisplayTools"}");
                await Task.CompletedTask;
            }
            else
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", HttpContext.Session.GetString("Token"));
                try
                {
                    var request = await client.GetStringAsync("/api/display-tools");
                    if (request is not null)
                    {
                        if (request.Length > 0)
                        {
                            var options = new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
                            };
                            Tools = JsonSerializer.Deserialize<List<Tool>>(request, options);
                        }
                    }
                    else
                    {
                        Msg = request.ToString();
                        Status = "error";
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        LoginModel loginModel = new LoginModel(_httpClient);
                        await loginModel.GetNewToken(HttpContext);
                        await OnGet();
                    }
                }
                catch (Exception e)
                {
                    Msg = e.Message;
                    Status = "error";
                }
            }
        }

        public async Task<IActionResult> OnPost(ToolDto toolDto)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(HttpContext.Session.GetString("Token"));
            toolDto.SellerIdDoctor = Guid.Parse(jwt.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value);
            var client = _httpClient.CreateClient();
            client.BaseAddress = new Uri(config["BaseAddress"]);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", HttpContext.Session.GetString("Token"));
            var multipartContent = new MultipartFormDataContent();
            multipartContent = await MappingContent(multipartContent, toolDto);
            var request = await client.PostAsync("/api/create-tool", multipartContent);
            if (request.IsSuccessStatusCode)
            {
                Msg = "Successfully added";
                Status = "success";
                return RedirectToPage("DisplayTools");
            }
            else
            {
                Msg = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Status = "error";
                return RedirectToPage("DisplayTools");
            }
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            var client = _httpClient.CreateClient();
            client.BaseAddress = new Uri(config["BaseAddress"]);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", HttpContext.Session.GetString("Token"));
            var request = await client.DeleteAsync($"api/delete-Tool/{id}");
            if (request.IsSuccessStatusCode)
            {
                Msg = "Successfully Deleted !";
                Status = "success";
                return RedirectToPage("DisplayTools");
            }
            else
            {
                Msg = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Status = "error";
                return RedirectToPage("DisplayTools");
            }
        }

        public async Task<IActionResult> OnPostEdit(ToolDto toolDto, Guid id)
        {
            var client = _httpClient.CreateClient();
            client.BaseAddress = new Uri(config["BaseAddress"]);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", HttpContext.Session.GetString("Token"));
            var multipartContent = new MultipartFormDataContent();
            multipartContent = await MappingContent(multipartContent, toolDto);
            var request = await client.PutAsync($"api/edit-tool/{id}", multipartContent);
            if (request.IsSuccessStatusCode)
            {
                Msg = "Edited successfully!";
                Status = "success";
                return RedirectToPage("DisplayTools");
            }
            else
            {
                Msg = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Status = "error";
                return RedirectToPage("", new { id });
            }
        }

        private async Task<MultipartFormDataContent> MappingContent(MultipartFormDataContent multipartFormDataContent, ToolDto toolDto)
        {
            multipartFormDataContent.Add(new StringContent(toolDto.ToolName, Encoding.UTF8, MediaTypeNames.Text.Plain), "ToolName");
            multipartFormDataContent.Add(new StringContent(toolDto.Description, Encoding.UTF8, MediaTypeNames.Text.Plain), "Description");
            multipartFormDataContent.Add(new StringContent(toolDto.SellerLocation, Encoding.UTF8, MediaTypeNames.Text.Plain), "SellerLocation");
            multipartFormDataContent.Add(new StringContent(toolDto.ContactNumber, Encoding.UTF8, MediaTypeNames.Text.Plain), "ContactNumber");
            multipartFormDataContent.Add(new StringContent(toolDto.ToolPrice.ToString(), Encoding.UTF8, MediaTypeNames.Text.Plain), "ToolPrice");
            multipartFormDataContent.Add(new StringContent(toolDto.SellerIdDoctor.ToString(), Encoding.UTF8, MediaTypeNames.Text.Plain), "SellerIdDoctor");
            if (!string.IsNullOrEmpty(toolDto.ToolStatus))
                multipartFormDataContent.Add(new StringContent(toolDto.ToolStatus, Encoding.UTF8, MediaTypeNames.Text.Plain), "ToolStatus");
            if (toolDto.Pictures.Count > 0)
            {
                foreach (var file in toolDto.Pictures)
                {
                    var fileContent = new StreamContent(file.OpenReadStream());
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                    multipartFormDataContent.Add(fileContent, "Pictures", file.FileName);
                }
            }
            return multipartFormDataContent;
        }
    }
}
