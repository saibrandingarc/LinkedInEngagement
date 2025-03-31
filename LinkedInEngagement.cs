using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LinkedInEngagement.Models;
using LinkedInEngagement.Context;
using System.Web;
using System.Security.Policy;
using System.Reflection.Metadata;
////using Org.BouncyCastle.Asn1.Ocsp;
//using Google.Apis.Auth.OAuth2;
//using Google.Ads.GoogleAds.Config;
//using Google.Ads.GoogleAds.Lib;
//using Google.Api.Gax;
//using Azure.Core;
//using Google.Ads.Gax.Config;
//using Google.Ads.GoogleAds;
//using Google.Ads.GoogleAds.V18.Errors;
//using Google.Ads.GoogleAds.V18.Services;
//using Google.Api;

namespace LinkedInEngagement
{
    public class LinkedInEngagement
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public LinkedInEngagement(ILoggerFactory loggerFactory, HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context)
        {
            _logger = loggerFactory.CreateLogger<LinkedInEngagement>();
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
            _configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        }

        [Function("RunLinkedinOrg")]
        public async Task RunLinkedinOrgAsync([TimerTrigger("0 */24 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# RunLinkedinOrg function executed at: {DateTime.Now}");
            //var client_id = _configuration["social:client_id"];
            //var redirect_uri = _configuration["social:redirect_uri"];
            //var scope = _configuration["social:scope"];
            //var state = _configuration["social:state"];
            //var connection = _configuration["social:connection"];

            //var url = $"https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id={client_id}&redirect_uri={redirect_uri}&scope={scope}&state={state}&connection={connection}";
            //await GetLinkedinOrganizations();
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next RunLinkedinOrg timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        [Function("RunLinkedinPosts")]
        public async Task RunLinkedinPosts([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# RunLinkedinPosts function executed at: {DateTime.Now}");
            await GetLinkedinOrganizationPostEngagements();
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next RunLinkedinPosts timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
        public string GetLinkedinAuthCode()
        {
            var client_id = _configuration["social:client_id"];
            var redirect_uri = _configuration["social:redirect_uri"];
            var scope = _configuration["social:scope"];
            var state = _configuration["social:state"];
            var connection = _configuration["social:connection"];

            var url = $"https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id={client_id}&redirect_uri={redirect_uri}&scope={scope}&state={state}&connection={connection}";
            return url;
        }

        // Method to Get Access Token
        public async Task<string> GetLinkedinAccessToken(string AuthCodeUrl)
        {
            var client_id = _configuration["social:client_id"];
            var client_secret = _configuration["social:client_secret"];
            var redirect_uri = _configuration["social:redirect_uri"];
            var scope = _configuration["social:scope"];
            var state = _configuration["social:state"];
            var connection = _configuration["social:connection"];
            var grant_type = _configuration["social:grant_type"];
            var AccessTokenUrl = _configuration["social:AccessTokenUrl"];

            // Extract the query string part of the URL
            var query = new Uri(AuthCodeUrl).Query;

            // Parse the query string into a NameValueCollection
            var queryParams = HttpUtility.ParseQueryString(query);

            // Get the value of the 'code' parameter
            string code = queryParams["code"];
            Console.WriteLine($"Code: {code}");

            var client = new HttpClient();
            var parameters = new Dictionary<string, string>
        {
            { "grant_type", grant_type },
            { "code", code },
            { "client_id", client_id },
            { "client_secret", client_secret },
            { "redirect_uri", redirect_uri }
        };
            var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

            // Construct the full URL
            var fullUrl = $"{AccessTokenUrl}?{queryString}";
            try
            {
                var response = await client.GetAsync(fullUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(json);
                    var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (responseData.ContainsKey("error"))
                    {
                        Console.WriteLine("Error: Missing required key");
                        return "";
                    }
                    else
                    {
                        Console.WriteLine("no errors");
                        var accessToken = await AddSettingsAsync(responseData, "linkedin");
                        //return accessToken;
                        return System.Text.Json.JsonSerializer.Serialize(accessToken);
                    }
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(json);
                    return System.Text.Json.JsonSerializer.Serialize(json);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
                return ex.Message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return ex.Message;
            }
        }

        // Method to Get Access Token
        public async Task<string> GetLinkedinOrganizations()
        {
            var accessToken = await GetValidLinkedInTokenAsync();
            Console.WriteLine(accessToken);
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/organizationalEntityAcls?q=roleAssignee&projection=(elements*(organizationalTarget~))");
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            try
            {
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    JObject jsonResponse = JObject.Parse(json);
                    foreach (JObject element in jsonResponse["elements"])
                    {
                        if (element is JObject elementObj)
                        {
                            string orgId = elementObj["organizationalTarget"]?.ToString();
                            string orgName = elementObj["organizationalTarget~"]?["localizedName"]?.ToString();
                            string localizedWebsite = elementObj["organizationalTarget~"]?["localizedWebsite"]?.ToString();
                            string id = elementObj["organizationalTarget~"]?["id"]?.ToString();
                            if (localizedWebsite != null)
                            {
                                string publicurl = getDomainName(localizedWebsite);
                                _logger.LogInformation($"Domain : {orgName} - {localizedWebsite}");
                                Console.WriteLine(orgName + "-" + localizedWebsite);
                                var clientData = await _context.ClientDetails
                                    .Where(c => c.PublicURL.Contains(publicurl))
                                    .FirstOrDefaultAsync();
                                if (clientData != null)
                                {
                                    Console.WriteLine("exist :" + publicurl);
                                    clientData.LinkedinId = id;
                                    await _context.SaveChangesAsync();
                                }
                                else
                                {
                                    Console.WriteLine("Not exist :" + publicurl);
                                }
                            }
                        }
                    }
                    return System.Text.Json.JsonSerializer.Serialize(json);
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(json);
                    return System.Text.Json.JsonSerializer.Serialize(json);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
                return ex.Message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return ex.Message;
            }
        }

        // Method to Get Access Token
        public async Task<string> GetLinkedinOrganizationPostEngagements()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var clientData = await _context.ClientDetails
                .Where(c => !string.IsNullOrEmpty(c.LinkedinId) && (c.LinkedInRunDate == null || c.LinkedInRunDate < today))
                .OrderBy(c => c.LinkedInRunDate)
                .FirstOrDefaultAsync();
            if (clientData != null)
            {
                var PostsData = await _context.LinkedInPosts
                .Where(c => c.OrganizationId == clientData.LinkedinId)
                .Select(c => c.PostId)
                .ToHashSetAsync();
                string jsonResult = JsonConvert.SerializeObject(PostsData, Formatting.Indented);
                _logger.LogWarning("Client: {clientid}", jsonResult);
                _logger.LogWarning("Client: {clientid} {@FirstRecord}", clientData.LinkedinId, clientData.Name);
                var accessToken = await GetValidLinkedInTokenAsync();
                ////foreach (var clientData in clients)
                ////{
                string baseUrl = "https://api.linkedin.com/v2/shares";
                int count = 20; // Number of records per request
                int start = 0;  // Start from 0
                int totalRecords = int.MaxValue;

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                while (start < totalRecords)
                {
                    string requestUrl = $"{baseUrl}?q=owners&owners=urn:li:organization:{clientData.LinkedinId}&start={start}&count={count}";
                    Console.WriteLine("requesturl :" + requestUrl);
                    var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                    try
                    {
                        var response = await client.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            JObject jsonResponse = JObject.Parse(json);

                            if (start == 0)
                            {
                                totalRecords = jsonResponse["paging"]?["total"]?.Value<int>() ?? 0;
                                _logger.LogWarning("Total: {totalRecords}", totalRecords);
                            }

                            JArray elements = jsonResponse["elements"] as JArray;
                            if (elements != null)
                            {
                                foreach (var element in elements)
                                {
                                    var postId = element["id"]?.ToString();
                                    bool postExists = PostsData.Contains(postId);
                                    _logger.LogWarning($"Post : {postId}., status: {postExists}");
                                    int ExistingPostId = 0;
                                    string activityUrn = element["activity"]?.ToString();
                                    var socialActions = await FetchSocialActions(client, accessToken, activityUrn);
                                    if (postExists)
                                    {
                                        var existingPostData = await _context.LinkedInPosts
                                            .Where(p => p.PostId == postId)
                                            .OrderByDescending(e => e.CreatedOn)
                                            .FirstOrDefaultAsync();
                                        if (existingPostData != null)
                                        {
                                            ExistingPostId = existingPostData.Id; // Converts int to string safely
                                            _logger.LogWarning("Existing Post ID: {ExistingPostId}", ExistingPostId);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("No post found with ID: {PostId}", postId);
                                        }
                                    }
                                    else
                                    {
                                        JArray contentEntities = element["content"]?["contentEntities"] as JArray;
                                        var resolvedURL = "";
                                        if (contentEntities != null && contentEntities.Count > 0)
                                        {
                                            var firstEntity = contentEntities[0];
                                            _logger.LogWarning("First Entity: {firstEntity}", firstEntity);
                                            // Check if thumbnails exist and are empty
                                            JArray thumbnails = firstEntity["thumbnails"] as JArray;
                                            _logger.LogWarning("thumbnails count: {thumbnails}", thumbnails != null ? thumbnails.Count : 0);
                                            if (thumbnails == null || thumbnails.Count == 0)
                                            {
                                                _logger.LogWarning("if thumbnails count: {thumbnails}", thumbnails != null ? thumbnails.Count : 0);
                                                resolvedURL = firstEntity["entityLocation"]?.ToString();
                                            }
                                            else
                                            {
                                                _logger.LogWarning("else thumbnails count: {thumbnails}", thumbnails.Count);
                                                Console.WriteLine("Thumbnails are available.");
                                                resolvedURL = firstEntity["thumbnails"]?[0]?["resolvedUrl"]?.ToString();
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("No content entities found.");
                                        }
                                        LinkedInPost post = new LinkedInPost();
                                        post.OrganizationId = clientData.LinkedinId;
                                        post.PostId = element["id"]?.ToString();
                                        post.PostContent = element["text"]?["text"]?.ToString();
                                        post.ResolvedURL = resolvedURL;
                                        post.CreatedOn = DateTimeOffset.FromUnixTimeMilliseconds(element["created"]?["time"]?.Value<long>() ?? 0).UtcDateTime;
                                        post.PostsCount = totalRecords;
                                        await _context.LinkedInPosts.AddAsync(post);
                                        await _context.SaveChangesAsync();
                                        ExistingPostId = post.Id;
                                        _logger.LogWarning("New Post ID: {ExistingPostId}", ExistingPostId);
                                    }
                                    var postObjCheck = await _context.LinkedInPostsEngagements
                                        .Where(c => c.OrganizationId == clientData.LinkedinId && c.LinkedInPostId == ExistingPostId && c.DateOfRun <= today)
                                        .FirstOrDefaultAsync();
                                    if (postObjCheck == null)
                                    {
                                        LinkedInPostsEngagement postObj = new LinkedInPostsEngagement
                                        {
                                            OrganizationId = clientData.LinkedinId,
                                            LinkedInPostId = ExistingPostId,
                                            TotalLikes = socialActions["likes"],
                                            Impressions = 0,
                                            TotalComments = socialActions["comments"],
                                            TotalShares = socialActions["shares"],
                                            DateOfRun = today
                                        };
                                        _logger.LogWarning($"{clientData.LinkedinId}------- {ExistingPostId} ---- {today}");
                                        await _context.LinkedInPostsEngagements.AddAsync(postObj);
                                        await _context.SaveChangesAsync();
                                        _logger.LogWarning("Post Data inserted");
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Post Data Exist");
                                    }
                                    //Console.WriteLine(new string('-', 50));
                                }
                            }
                            else
                            {
                                _logger.LogWarning("elements is not getting");
                            }
                            start += count;
                        }
                        else
                        {
                            Console.WriteLine($"API request failed with status: {response.StatusCode}");
                            break;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"Request failed: {ex.Message}");
                        break;
                    }
                }
                clientData.LinkedInRunDate = today;
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation("No matching record found.");
            }
            return System.Text.Json.JsonSerializer.Serialize(clientData);
        }

        //    // Method to Get Access Token
        //    public async Task<string> GetLinkedinOrgPostEngagements()
        //    {
        //        var posts = await _context.LinkedInPosts.Select(p => new
        //        {
        //            p.Id,
        //            p.OrganizationId,
        //            p.PostId
        //        }).ToListAsync();
        //        var accessToken = await GetValidLinkedInTokenAsync();
        //        Console.WriteLine(accessToken);
        //        foreach (var post in posts)
        //        {
        //            Console.WriteLine($"Name: {post.PostId}");
        //            string baseUrl = "https://api.linkedin.com/v2/shares";

        //            using HttpClient client = new HttpClient();
        //            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
        //            try
        //            {
        //                LinkedInPostsEngagement postObj = new LinkedInPostsEngagement
        //                {
        //                    OrganizationId = post.OrganizationId,
        //                    PostId = post.Id,
        //                };
        //                string activityUrn = "urn:li:activity:" + post.PostId;

        //                // Fetch social actions for this post
        //                var socialActions = await FetchSocialActions(client, accessToken, activityUrn);
        //                postObj.TotalLikes = socialActions["likes"];
        //                postObj.Impressions = 0; // You may need a different API for impressions
        //                postObj.TotalComments = socialActions["comments"];
        //                postObj.TotalShares = socialActions["shares"];
        //                await _context.LinkedInPostsEngagements.AddAsync(postObj);
        //                await _context.SaveChangesAsync();
        //                Console.WriteLine(new string('-', 50));
        //            }
        //            catch (HttpRequestException ex)
        //            {
        //                Console.WriteLine($"Request failed: {ex.Message}");
        //                break;
        //            }
        //        }
        //        return "success";
        //    }

        public async Task<Dictionary<string, int>> FetchSocialActions(HttpClient client, string accessToken, string activityUrn)
        {
            Dictionary<string, int> socialActions = new Dictionary<string, int>
            {
                { "likes", 0 },
                { "comments", 0 },
                { "shares", 0 }
            };

            if (string.IsNullOrEmpty(activityUrn)) return socialActions;

            string socialActionsUrl = $"https://api.linkedin.com/v2/socialActions/{activityUrn}";
            var request = new HttpRequestMessage(HttpMethod.Get, socialActionsUrl);
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            try
            {
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    JObject socialResponse = JObject.Parse(json);

                    // Extract social engagement details
                    socialActions["likes"] = socialResponse["likesSummary"]?["totalLikes"]?.Value<int>() ?? 0;
                    socialActions["comments"] = socialResponse["commentsSummary"]?["totalComments"]?.Value<int>() ?? 0;
                    socialActions["shares"] = socialResponse["shares"]?.Value<int>() ?? 0;
                }
                else
                {
                    Console.WriteLine($"Failed to fetch social actions for {activityUrn}. Status: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Social actions request failed: {ex.Message}");
            }

            return socialActions;
        }

        public async Task<string> GetValidLinkedInTokenAsync()
        {
            var settings = await _context.Settings
                        .Where(p => p.tokenFrom == "linkedin")
                        .OrderByDescending(p => p.DateAdded)
                        .FirstOrDefaultAsync();

            if (settings == null)
            {
                return "Invalid Token";
            }

            // Calculate expiration time
            var expirationTime = settings.DateAdded.AddSeconds(settings.ExpiresIn);
            Console.WriteLine(expirationTime);
            Console.WriteLine(DateTime.UtcNow);
            if (DateTime.UtcNow >= expirationTime)
            {
                // Token has expired, regenerate using refresh token
                var newToken = await RegenerateLinkedInTokenAsync(settings.RefreshToken);

                // Update the token and date in the settings table
                settings.AccessToken = newToken;
                settings.DateAdded = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return newToken;
            }

            // Token is still valid
            return settings.AccessToken;
        }

        // Method to regenerate the token using the refresh token
        private async Task<string> RegenerateLinkedInTokenAsync(string refreshToken)
        {
            // Replace with actual API call to Zoho to regenerate the token
            using (var httpClient = new HttpClient())
            {
                var client_id = _configuration["social:client_id"];
                var client_secret = _configuration["social:client_secret"];
                var AccessTokenUrl = _configuration["social:AccessTokenUrl"];

                var parameters = new Dictionary<string, string>
                {
                    { "refresh_token", refreshToken },
                    { "client_id", client_id },
                    { "client_secret", client_secret },
                    { "grant_type", "refresh_token" }
                };

                try
                {
                    var response = await httpClient.PostAsync(AccessTokenUrl, new FormUrlEncodedContent(parameters));

                    if (response.IsSuccessStatusCode)
                    {
                        var settings = await _context.Settings
                            .Where(p => p.tokenFrom == "linkedin")
                            .OrderByDescending(p => p.DateAdded)
                            .FirstOrDefaultAsync();

                        var json = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                        // Return the new access token
                        return responseData["access_token"];
                    }
                    else
                    {
                        throw new Exception("Failed to regenerate token.");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Request failed: {ex.Message}");
                    return ex.Message;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return ex.Message;
                }
            }
        }

        public async Task<string?> AddSettingsAsync(Dictionary<string, string>? responseData, string tokenFrom)
        {
            var accessToken = responseData["access_token"];
            var existData = await _context.Settings
                .Where(p => p.tokenFrom == tokenFrom)
                .OrderByDescending(p => p.DateAdded)
                .FirstOrDefaultAsync();

            if (existData != null)
            {
                existData.DateAdded = DateTime.UtcNow;
                existData.AccessToken = accessToken;

                // Save changes
                await _context.SaveChangesAsync();
            }
            else
            {
                var newSetting = new Settings
                {
                    AccessToken = responseData["access_token"],
                    RefreshToken = responseData["refresh_token"],
                    Scope = responseData["scope"],
                    ExpiresIn = int.Parse(responseData["expires_in"]),
                    DateAdded = DateTime.UtcNow,
                    tokenFrom = tokenFrom
                };

                _context.Settings.Add(newSetting);
            }

            await _context.SaveChangesAsync();
            return accessToken;
        }

        //    public async Task<string> GetGoogleServiceAccountAsync()
        //    {
        //        string relativePath = _configuration["GoogleAds:ServiceAccountJsonPath"];
        //        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

        //        if (!File.Exists(fullPath))
        //        {
        //            throw new FileNotFoundException($"Service account file not found at {fullPath}");
        //        }

        //        string scope = "https://www.googleapis.com/auth/adwords";

        //        GoogleCredential credential;
        //        using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
        //        {
        //            credential = GoogleCredential.FromStream(stream)
        //                .CreateScoped(new[] { scope });
        //        }

        //        var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

        //        return token.ToString();
        //    }

        //    //public async Task<string> GetGetGoogleAdsListAsync()
        //    //{
        //    //    string token = await GetGoogleServiceAccountAsync();
        //    //    List<Ad> adsList = new List<Ad>();

        //    //    try
        //    //    {
        //    //        GoogleAdsServiceClient googleAdsService = client.GetService(Services.V14.GoogleAdsService);

        //    //        // Define Query to Fetch Ads
        //    //        string query = @"
        //    //        SELECT ad_group_ad.ad.id, 
        //    //               ad_group_ad.ad.type, 
        //    //               ad_group_ad.ad.name, 
        //    //               ad_group_ad.ad.final_urls
        //    //        FROM ad_group_ad 
        //    //        WHERE ad_group_ad.status = 'ENABLED'";

        //    //        // Execute Search Query
        //    //        PagedEnumerable<SearchGoogleAdsResponse, GoogleAdsRow> response =
        //    //            googleAdsService.Search(customerId, query);

        //    //        // Iterate and Store Ads
        //    //        foreach (GoogleAdsRow row in response)
        //    //        {
        //    //            adsList.Add(row.AdGroupAd.Ad);
        //    //            Console.WriteLine($"Ad ID: {row.AdGroupAd.Ad.Id}, Name: {row.AdGroupAd.Ad.Name}, Type: {row.AdGroupAd.Ad.Type}");
        //    //        }
        //    //    }
        //    //    catch (GoogleAdsException e)
        //    //    {
        //    //        Console.WriteLine($"Google Ads API error: {e.Message}");
        //    //    }

        //    //    return adsList;
        //    //}

        //    private GoogleAdsClient CreateGoogleAdsClient()
        //    {
        //        var developerToken = _configuration["GoogleAds:DeveloperToken"];
        //        var managerCustomerId = _configuration["GoogleAds:ManagerCustomerId"];
        //        var serviceAccountJsonPath = _configuration["GoogleAds:ServiceAccountJsonPath"];
        //        var impersonatedEmail = _configuration["GoogleAds:ImpersonatedEmail"];

        //        if (string.IsNullOrEmpty(developerToken) ||
        //            string.IsNullOrEmpty(managerCustomerId) ||
        //            string.IsNullOrEmpty(serviceAccountJsonPath) ||
        //            string.IsNullOrEmpty(impersonatedEmail))
        //        {
        //            throw new Exception("Google Ads API credentials are missing in appsettings.json.");
        //        }

        //        GoogleCredential credential;
        //        using (var stream = new FileStream(serviceAccountJsonPath, FileMode.Open, FileAccess.Read))
        //        {
        //            credential = GoogleCredential.FromStream(stream)
        //                .CreateScoped(new[] { "https://www.googleapis.com/auth/adwords" })
        //                .CreateWithUser(impersonatedEmail);
        //        }

        //        var token = credential.UnderlyingCredential.GetAccessTokenForRequestAsync().ToString();

        //        GoogleAdsConfig config = new GoogleAdsConfig()
        //        {
        //            DeveloperToken = developerToken,
        //            LoginCustomerId = managerCustomerId,
        //            OAuth2Mode = OAuth2Flow.SERVICE_ACCOUNT,
        //            OAuth2SecretsJsonPath = serviceAccountJsonPath,
        //            OAuth2PrnEmail = impersonatedEmail
        //        };
        //        return new GoogleAdsClient(config);
        //    }

        //    public async Task<string> GetMccAccountsAsync()
        //    {
        //        //GoogleAdsClient client = CreateGoogleAdsClient();
        //        //Console.WriteLine(client);

        //        var developerToken = _configuration["GoogleAds:DeveloperToken"];
        //        var managerCustomerId = _configuration["GoogleAds:ManagerCustomerId"];
        //        var serviceAccountJsonPath = _configuration["GoogleAds:ServiceAccountJsonPath"];
        //        var impersonatedEmail = _configuration["GoogleAds:ImpersonatedEmail"];

        //        var customerId = "8437902303";

        //        GoogleAdsConfig config = new GoogleAdsConfig()
        //        {
        //            DeveloperToken = developerToken,
        //            OAuth2Mode = OAuth2Flow.SERVICE_ACCOUNT,
        //            OAuth2SecretsJsonPath = serviceAccountJsonPath,
        //            LoginCustomerId = managerCustomerId
        //        };
        //        GoogleAdsClient client = new GoogleAdsClient(config);

        //        // Get the GoogleAdsService.
        //        GoogleAdsServiceClient googleAdsService = client.GetService(
        //            Google.Ads.GoogleAds.Services.V18.GoogleAdsService);

        //        // Create a query that will retrieve all campaigns.
        //        string query = @"SELECT
        //            campaign.id,
        //            campaign.name,
        //            campaign.network_settings.target_content_network
        //        FROM campaign
        //        ORDER BY campaign.id";

        //        try
        //        {
        //            // Issue a search request.
        //            googleAdsService.SearchStream(customerId.ToString(), query,
        //                delegate (SearchGoogleAdsStreamResponse resp)
        //                {
        //                    foreach (GoogleAdsRow googleAdsRow in resp.Results)
        //                    {
        //                        Console.WriteLine("Campaign with ID {0} and name '{1}' was found.",
        //                            googleAdsRow.Campaign.Id, googleAdsRow.Campaign.Name);
        //                    }
        //                }
        //            );

        //            return impersonatedEmail;
        //        }
        //        catch (GoogleAdsException e)
        //        {
        //            Console.WriteLine("Failure:");
        //            Console.WriteLine($"Message: {e.Message}");
        //            Console.WriteLine($"Failure: {e.Failure}");
        //            Console.WriteLine($"Request ID: {e.RequestId}");
        //            return e.Message;
        //        }
        //    }

        public string getDomainName(string website) {
            if (!website.StartsWith("http")) {
                website = "https://" + website;
            }

            Uri uri = new Uri(website);
            string host = uri.Host;

            if (host.StartsWith("www.")) {
                host = host.Substring(4);
            }

            Console.WriteLine($"Domain Name: {host}");
            return host;
        }

    }
}