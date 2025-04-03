
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace brj
{
    public class SignInTrigger
    {
        private readonly ILogger _logger;

        public SignInTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SignInTrigger>();
        }

        [Function("SignInTrigger")]
        public async Task Run([TimerTrigger("0 31 19 * * 3")] TimerInfo myTimer)
        { 
            string loginUrl = "https://www.aarhuskanokajak.dk/account/loginajax";

            var loginPostBody = new
            {
                username = Environment.GetEnvironmentVariable("AKKKUSERNAME"),
                password = Environment.GetEnvironmentVariable("PASSWORD"),
                remember = "true"
            };

            string loginPostBodyJson = JsonSerializer.Serialize(loginPostBody);

            using (var httpClient = new HttpClient())
            {
                var loginContent = new StringContent(loginPostBodyJson, Encoding.UTF8, "application/json");
                var loginResponse = await httpClient.PostAsync(loginUrl, loginContent);

                if (loginResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation("Login successful!");
                }
                else
                {
                    _logger.LogError($"Login failed! Status code: {(int)loginResponse.StatusCode}");
                    throw new Exception($"Login failed! Status code: {(int)loginResponse.StatusCode}");
                }

                // Get events
                string tomorrow = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                string eightDaysFromNow = DateTime.Now.AddDays(8).ToString("yyyy-MM-dd");

                string eventListUrl = $"https://www.aarhuskanokajak.dk/api/activity/event/days?eventsToShow=300&fromOffset={tomorrow}T23:00:00.000Z&toTime={eightDaysFromNow}T21:59:59.999Z";

                var eventsResponse = await httpClient.GetAsync(eventListUrl);

                if (eventsResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation("Event list retrieved successfully!");
                }
                else
                {
                    _logger.LogError($"Failed to retrieve event list! Status code: {(int)eventsResponse.StatusCode}");
                    throw new Exception($"Failed to retrieve event list! Status code: {(int)eventsResponse.StatusCode}");
                }

                string eventsJson = await eventsResponse.Content.ReadAsStringAsync();
                JsonNode events = JsonNode.Parse(eventsJson);

                JsonNode activity = null;

                foreach (JsonNode eventItem in events.AsArray())
                {
                    foreach (JsonNode slot in eventItem["slots"].AsArray())
                    {
                        if (slot["name"]?.ToString() == "Klubaften, friroede 2025")
                        {
                            activity = slot;
                            break;
                        }
                    }
                    if (activity != null) break;
                }

                if (activity == null)
                {
                    _logger.LogError("Could not find the specified activity");
                    throw new Exception("Could not find the specified activity");
                }

                // Make booking
                string bookPostBody = @$"{{""isValidationRequest"":false,""memberAttendeeCount"":1,""bookingText"":"""",""attendees"":[{{""memberId"":""{Environment.GetEnvironmentVariable("AKKKMEMBERID")}"",""attendeeCount"":1,""doCancel"":false,""doUpdate"":false,""bookingText"":""""}}],""bookingActionOnPayment"":""bookForFree"",""sendMails"":true}}";

                string bookingUrl = $"https://www.aarhuskanokajak.dk/api/activity/{activity["activityId"]}/event/{activity["eventId"]}/book";

                var bookingContent = new StringContent(bookPostBody, Encoding.UTF8, "application/json");
                var bookingResponse = await httpClient.PostAsync(bookingUrl, bookingContent);

                if (bookingResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation("Booking successful!");
                }
                else
                {
                    _logger.LogError($"Booking failed! Status code: {(int)bookingResponse.StatusCode}");
                    throw new Exception($"Booking failed! Status code: {(int)bookingResponse.StatusCode}");
                }
            }
        }
    }
}
