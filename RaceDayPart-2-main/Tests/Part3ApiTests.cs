using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RaceDay.Api.Tests;

public class Part3ApiTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;
    private static readonly byte[] Gif = "GIF89a"u8.ToArray();

    public Part3ApiTests(RaceDayApiFactory factory) => _factory = factory;

    [Fact]
    public async Task EventRejectsUnsupportedType()
    {
        using var organiser = await OrganiserAsync();
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody(eventType: "Swim"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EventPersistsDistanceKm()
    {
        using var organiser = await OrganiserAsync();
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody(distanceKm: 12.35m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(12.35m, json.RootElement.GetProperty("distanceKm").GetDecimal());
        var legacyResponse = await organiser.PostAsJsonAsync("/api/events", EventBody(legacyDistance: 8m));
        using var legacyJson = JsonDocument.Parse(await legacyResponse.Content.ReadAsStringAsync());
        Assert.Equal(8m, legacyJson.RootElement.GetProperty("distanceKm").GetDecimal());
    }

    [Fact]
    public async Task EventBrowseFiltersBySearchTypeAndUpcoming()
    {
        using var organiser = await OrganiserAsync();
        await organiser.PostAsJsonAsync("/api/events", EventBody(name: "Sunrise Run", location: "Cape Town", eventType: "Run"));
        await organiser.PostAsJsonAsync("/api/events", EventBody(name: "Harbor Walk", location: "Durban", eventType: "Walk"));
        var response = await organiser.GetAsync("/api/events?search=Sunrise&type=Run&location=Cape&upcomingOnly=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Single(json.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task OrganiserMineReturnsOnlyOwnedEvents()
    {
        using var organiser = await OrganiserAsync();
        await organiser.PostAsJsonAsync("/api/events", EventBody());
        var response = await organiser.GetAsync("/api/events/mine");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(json.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task CategoryReturnsPriceCapacityAndEnrolmentCount()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, evt, price: 18.75m, capacity: 4);
        var response = await organiser.GetAsync($"/api/categories/{category}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(18.75m, json.RootElement.GetProperty("price").GetDecimal());
        Assert.Equal(4, json.RootElement.GetProperty("capacity").GetInt32());
        Assert.Equal(0, json.RootElement.GetProperty("enrolledCount").GetInt32());
    }

    [Fact]
    public async Task CategoryRejectsInvalidAgeRange()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var response = await organiser.PostAsJsonAsync("/api/categories", CategoryBody(evt, minimumAge: 50, maximumAge: 20));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await organiser.PostAsJsonAsync("/api/categories", CategoryBody(evt, price: 1.234m))).StatusCode);
    }

    [Fact]
    public async Task CategoryCapacityRejectsAdditionalEnrolment()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, evt, capacity: 1);
        using var first = await ParticipantAsync();
        using var second = await ParticipantAsync();
        Assert.Equal(HttpStatusCode.Created, (await first.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await second.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category))).StatusCode);
    }

    [Fact]
    public async Task CancelledEnrolmentDoesNotConsumeCapacity()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, evt, capacity: 1);
        using var first = await ParticipantAsync();
        using var second = await ParticipantAsync();
        var created = await first.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category));
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("registrationId").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await first.DeleteAsync($"/api/enrolments/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await second.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category))).StatusCode);
    }

    [Fact]
    public async Task PastEventCannotBeJoined()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser, DateTime.Today.AddDays(-1));
        var category = await CreateCategoryAsync(organiser, evt);
        using var participant = await ParticipantAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await participant.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category))).StatusCode);
    }

    [Fact]
    public async Task ParticipantCancellationSetsCancelledStatus()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, evt);
        using var participant = await ParticipantAsync();
        var created = await participant.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category));
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("registrationId").GetInt32();
        await participant.DeleteAsync($"/api/enrolments/{id}");
        var read = await participant.GetAsync($"/api/registrations/{id}");
        using var result = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        Assert.Equal("Cancelled", result.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ParticipantCannotCancelRegistrationWithResult()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, evt);
        using var participant = await ParticipantAsync();
        var created = await participant.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category));
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var registrationId = json.RootElement.GetProperty("registrationId").GetInt32();
        await organiser.PostAsJsonAsync("/api/results", new { registrationId, finishTime = "00:25:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Conflict, (await participant.DeleteAsync($"/api/enrolments/{registrationId}")).StatusCode);
    }

    [Fact]
    public async Task OrganiserCanUpdateEnrolmentStatus()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, evt);
        using var participant = await ParticipantAsync();
        var created = await participant.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category));
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("registrationId").GetInt32();
        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/registrations/me");
        Assert.Single(mine.EnumerateArray());
        Assert.Equal("Part Three Race", mine[0].GetProperty("eventName").GetString());
        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{id}/status", new { status = "confirmed" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResultsRejectUnsupportedStatus()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, evt);
        using var participant = await ParticipantAsync();
        var registration = await participant.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category));
        using var json = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("registrationId").GetInt32();
        Assert.Equal(HttpStatusCode.BadRequest, (await organiser.PostAsJsonAsync("/api/results", new { registrationId = id, finishTime = "00:25:00", position = 1, status = "DNS" })).StatusCode);
    }

    [Fact]
    public async Task ResultSupportsDnfAndParticipantMeRoute()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, evt);
        using var participant = await ParticipantAsync();
        var registration = await participant.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category));
        using var json = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("registrationId").GetInt32();
        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync("/api/results", new { registrationId = id, finishTime = "00:00:00", position = 1, status = "DNF" })).StatusCode);
        var me = await participant.GetAsync("/api/results/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var results = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal("DNF", results.RootElement[0].GetProperty("positionOfNFinishers").GetString());
        using var otherParticipant = await ParticipantAsync();
        var otherRegistration = await otherParticipant.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category));
        using var otherJson = JsonDocument.Parse(await otherRegistration.Content.ReadAsStringAsync());
        var otherRegistrationId = otherJson.RootElement.GetProperty("registrationId").GetInt32();
        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync("/api/results", new { registrationId = otherRegistrationId, finishTime = "00:24:00", position = 1, status = "Finished" })).StatusCode);
        var otherResults = await otherParticipant.GetFromJsonAsync<JsonElement>("/api/results/me");
        Assert.Equal("1 of 1 finishers", otherResults[0].GetProperty("positionOfNFinishers").GetString());
    }

    [Fact]
    public async Task RegistrationRejectsUnsupportedRole()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { firstName = "Test", lastName = "User", email = Email(), password = "SecurePass123!", role = "Admin" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateEmailIsRejectedWithoutCaseSensitivity()
    {
        using var client = _factory.CreateClient();
        var email = Email();
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(client, email)).StatusCode);
        var response = await RegisterAsync(client, email.ToUpperInvariant());
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ProfilePictureUploadPersistsProfileUrl()
    {
        using var participant = await ParticipantAsync();
        using var form = ImageForm("picture.gif", Gif);
        var response = await participant.PostAsync("/api/users/me/profile-picture", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await participant.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.StartsWith("https://blob.test/profile-pictures/", profile.GetProperty("profilePictureUrl").GetString());
        Assert.NotEmpty(_factory.Services.GetRequiredService<FakeBlobStorageService>().Files);
    }

    [Fact]
    public async Task PictureUploadRejectsUnsupportedExtension()
    {
        using var participant = await ParticipantAsync();
        using var form = ImageForm("malware.exe", Gif);
        Assert.Equal(HttpStatusCode.BadRequest, (await participant.PostAsync("/api/users/me/profile-picture", form)).StatusCode);
        var oversized = new byte[5 * 1024 * 1024 + 1];
        using var oversizedForm = ImageForm("large.gif", oversized);
        Assert.Equal(HttpStatusCode.BadRequest, (await participant.PostAsync("/api/users/me/profile-picture", oversizedForm)).StatusCode);
    }

    [Fact]
    public async Task EventBannerUploadSavesPublicUrl()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        using var form = ImageForm("banner.gif", Gif);
        var response = await organiser.PostAsync($"/api/events/{evt}/banner", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var read = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{evt}");
        Assert.StartsWith("https://blob.test/event-banners/", read.GetProperty("bannerImageUrl").GetString());
    }

    [Fact]
    public async Task DifferentOrganiserCannotUploadEventBanner()
    {
        using var owner = await OrganiserAsync();
        var evt = await CreateEventAsync(owner);
        using var other = await OrganiserAsync();
        using var form = ImageForm("banner.gif", Gif);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsync($"/api/events/{evt}/banner", form)).StatusCode);
    }

    [Fact]
    public async Task CategoryWithoutCapacityHasNoEnrolmentLimit()
    {
        using var organiser = await OrganiserAsync();
        var evt = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, evt);
        using var first = await ParticipantAsync();
        using var second = await ParticipantAsync();
        Assert.Equal(HttpStatusCode.Created, (await first.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await second.PostAsJsonAsync("/api/enrolments", Enrolment(evt, category))).StatusCode);
    }

    private async Task<HttpClient> OrganiserAsync()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = Email();
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(client, email, "Organiser")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SecurePass123!" })).StatusCode);
        return client;
    }

    private async Task<HttpClient> ParticipantAsync()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = Email();
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(client, email)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SecurePass123!" })).StatusCode);
        return client;
    }

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string role = "Participant") =>
        client.PostAsJsonAsync("/api/auth/register", new { firstName = "Part", lastName = "Three", email, password = "SecurePass123!", role, dateOfBirth = "1990-05-12" });

    private async Task<int> CreateEventAsync(HttpClient organiser, DateTime? date = null)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody(date: date));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("eventId").GetInt32();
    }

    private static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, decimal price = 0, int? capacity = null)
    {
        var response = await organiser.PostAsJsonAsync("/api/categories", CategoryBody(eventId, price: price, capacity: capacity));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("categoryId").GetInt32();
    }

    private static object EventBody(string name = "Part Three Race", string location = "Pretoria", string eventType = "Run", decimal distanceKm = 5, decimal? legacyDistance = null, DateTime? date = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["description"] = "API test event",
            ["date"] = (date ?? DateTime.Today.AddDays(20)).ToString("yyyy-MM-dd"),
            ["location"] = location,
            ["eventType"] = eventType
        };
        if (legacyDistance.HasValue) body["distance"] = legacyDistance.Value;
        else body["distanceKm"] = distanceKm;
        return body;
    }

    private static object CategoryBody(int eventId, int minimumAge = 0, int maximumAge = 120, decimal price = 0, int? capacity = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = "Open Category",
            ["minimumAge"] = minimumAge,
            ["maximumAge"] = maximumAge,
            ["distance"] = 5m,
            ["price"] = price,
            ["eventId"] = eventId
        };
        if (capacity.HasValue) body["capacity"] = capacity.Value;
        return body;
    }

    private static object Enrolment(int eventId, int categoryId) => new { eventId, categoryId };
    private static string Email() => $"test-{Guid.NewGuid():N}@example.com";
    private static MultipartFormDataContent ImageForm(string fileName, byte[] bytes)
    {
        var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(bytes);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/gif");
        form.Add(image, "file", fileName);
        return form;
    }
}
