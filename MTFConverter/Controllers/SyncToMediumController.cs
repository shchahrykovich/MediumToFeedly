using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MTFConverter.Controllers
{
    public class SyncToMediumController
    {
        private const string JsonPrefixMedium = "])}while(1);</x>";
        private const int Myself = 1;

        public void Sync(String userId, String feedCategory, String feedlyToken)
        {
            using (HttpClient httpClient = new HttpClient())
            { 
                var rssLinks = GetLinksFromMedium(userId, httpClient).GetAwaiter().GetResult();
                var feedlyRss = GetFeedlyRss(httpClient, feedlyToken).GetAwaiter().GetResult();

                foreach (var rssLink in rssLinks)
                {
                    if (!feedlyRss.Contains(rssLink.Link))
                    {
                        var newFeed = new
                        {
                            id = $"feed/{rssLink.Link}",
                            title = $"Medium - {rssLink.UserName}",
                            categories = new Object[] {
                                new {
                                    id = feedCategory,
                                    label ="Medium"
                                }
                                }
                        };
                        var content = new StringContent(JsonConvert.SerializeObject(newFeed));
                        using (var httpResponse = httpClient
                            .PostAsync("https://cloud.feedly.com/v3/subscriptions", content).GetAwaiter().GetResult())
                        {
                            if (!httpResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine(httpResponse.ReasonPhrase);
                                Console.WriteLine(httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                            }
                        }
                    }
                }
            }
        }

        private static async Task<string> GetFeedlyRss(HttpClient httpClient, String feedlyToken)
        {
            //https://developer.feedly.com/v3/developer/#how-do-i-generate-a-developer-access-token
            httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {feedlyToken}'");
            using (var httpResponse = await httpClient.GetAsync("https://cloud.feedly.com/v3/subscriptions"))
            {
                return await httpResponse.Content.ReadAsStringAsync();
            }
        }

        private static async Task<IEnumerable<(String Link, String UserName)>> GetLinksFromMedium(String userId, HttpClient mediumClient)
        {
            var mediumProfileUrl = $"https://medium.com/_/api/users/{userId}/profile/stream?limit=100&source=following";
            using (var httpResponse = await mediumClient.GetAsync(mediumProfileUrl))
            {
                var rawResponse = await httpResponse.Content.ReadAsStringAsync();

                var stringResponse = rawResponse.Substring(JsonPrefixMedium.Length);
                var response = JObject.Parse(stringResponse);

                var rawUsersFollowedCount = response.SelectToken($"payload.references.SocialStats.{userId}.usersFollowedCount");
                var usersFollowedCount = rawUsersFollowedCount.Value<int>();

                var users = response.SelectTokens("payload.references.User.*").ToArray();
                if (users.Length != (usersFollowedCount + Myself))
                {
                    throw new Exception("Can't enumerate all users");
                }

                List<(String, String)> result = new List<(String, String)>();
                foreach (var user in users.Take(usersFollowedCount))
                {
                    var userName = user["username"].Value<String>();
                    result.Add(( $"https://medium.com/feed/@{userName}", userName ));
                }
                return result;
            }
        }
    }
}
