using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using SQLite;
using static Google.Apis.Requests.BatchRequest;

public class GmailAttachmentExtractor
{
    public GmailService? Service { get; private set; }
    public DateTime LastRetrievedMessageDate { get; set; }
    
    public GmailAttachmentExtractor()
    {
        LastRetrievedMessageDate = GetLastExtractionDate();
    }

    public async Task StartService()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<GmailAttachmentExtractor>().Build();

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync
            (
                new ClientSecrets
                {
                    ClientId = config["installed:client_id"],
                    ClientSecret = config["installed:client_secret"]
                },
                new[] { GmailService.Scope.GmailReadonly },
                "user",
                CancellationToken.None
            );

        Service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });
    }

   public async Task ExtractAllAttachments(bool onlyGetNew = true)
   {
        await StartService();

        if(Service == null)
        {
            return;
        }

        using var db = new SQLiteContext();

        string? dateFilter = null;

        if (onlyGetNew)
        {
            dateFilter = LastRetrievedMessageDate.ToString("yyyy/MM/dd");
        }

        var allThreads = new List<Google.Apis.Gmail.v1.Data.Thread>();
        string? nextPageToken = null;

        do
        {
            var request = Service.Users.Threads.List("me");
            if (dateFilter is not null)
            {
                request.Q = $"after:{dateFilter}";
            }
            request.MaxResults = 500;
            request.PageToken = nextPageToken;

            var response = request.Execute();
            allThreads.AddRange(response.Threads);
            nextPageToken = response.NextPageToken;

        } while (nextPageToken != null);

        foreach (var thread in allThreads)
        {

            var threadRequest = Service.Users.Threads.Get("me", thread.Id);
            var threadData = threadRequest.Execute();

            foreach (var message in threadData.Messages)
            {
                foreach (var part in message.Payload.Parts)
                {
                    if (string.IsNullOrEmpty(part.Filename))
                    {
                        continue;
                    }
                    
                    DateTime messageDate = UnixTimeToDateTime(message.InternalDate!.Value);

                    if (messageDate > LastRetrievedMessageDate)
                    {
                        LastRetrievedMessageDate = messageDate;
                    }

                    var attachId = part.Body.AttachmentId;
                    var attachRequest = Service.Users.Messages.Attachments.Get("me", message.Id, attachId);
                    var attachData = attachRequest.Execute();
                    byte[] data = Convert.FromBase64String(attachData.Data.Replace('-', '+').Replace('_', '/'));

                    var fileName = part.PartId;
                    var folderPath = Path.Combine(Environment.CurrentDirectory, "Attachments");
                    Directory.CreateDirectory(folderPath);
                    var filePath = Path.Combine(folderPath, fileName!);

                    File.WriteAllBytes(filePath, data);

                    await db.ImageAttachments.AddAsync(new ImageAttachment() { Id = fileName, Timestamp = messageDate });

                    Console.WriteLine(fileName);
                }
            }

            await db.ExtractionDates.AddAsync(new ExtractionDate() { Date= LastRetrievedMessageDate });

            await db.SaveChangesAsync();

            Service.Dispose();
        }
    }

    public DateTime UnixTimeToDateTime(long unixTime)
    {
        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime;
        return dateTime.ToLocalTime();
    }


    public DateTime GetLastExtractionDate()
    {
        using var context = new SQLiteContext();

        var latestExtraction = context.ExtractionDates
            .OrderByDescending(d => d.Date)
            .FirstOrDefault();

        return latestExtraction?.Date ?? DateTime.MinValue;
    }
}

