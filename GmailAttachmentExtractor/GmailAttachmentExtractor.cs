using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using static Google.Apis.Requests.BatchRequest;

public class GmailAttachmentExtractor
{
    public GmailService? Service { get; private set; }
    public DateTime? lastDownloadedMessageDate { get; set; }

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

        string? dateFilter = null;

        if (onlyGetNew && lastDownloadedMessageDate is not null)
        {
            dateFilter = lastDownloadedMessageDate?.ToString("yyyy/MM/dd");
        }

        var allThreads = new List<Google.Apis.Gmail.v1.Data.Thread>();
        string? nextPageToken = null;

        do
        {
            var request = Service.Users.Threads.List("me");
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
                    DateTime messageDate = UnixTimeToDateTime(message.InternalDate.Value);

                    if (!lastDownloadedMessageDate.HasValue || messageDate > lastDownloadedMessageDate.Value)
                    {
                        lastDownloadedMessageDate = messageDate;
                    }

                    //var attachId = part.Body.AttachmentId;
                    //var attachRequest = Service.Users.Messages.Attachments.Get("me", message.Id, attachId);
                    //var attachData = attachRequest.Execute();
                    //byte[] data = Convert.FromBase64String(attachData.Data.Replace('-', '+').Replace('_', '/'));

                    var fileName = messageDate.ToString();
                    //var folderPath = Path.Combine(Environment.CurrentDirectory, "Attachments");
                    //Directory.CreateDirectory(folderPath);
                    //var filePath = Path.Combine(folderPath, fileName);

                    //File.WriteAllBytes(filePath, data);

                    Console.WriteLine(fileName);
                }
            }
        }
    }

    public DateTime UnixTimeToDateTime(long unixTime)
    {
        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime;
        return dateTime.ToLocalTime();
    }
}

