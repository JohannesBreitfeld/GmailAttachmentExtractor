using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;

public class GmailAttachmentExtractor
{
   public async Task ExtractAttachment()
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

        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });

        var request = service.Users.Threads.List("me");

        var threads = request.Execute().Threads;

        foreach (var thread in threads)
        {

            var threadRequest = service.Users.Threads.Get("me", thread.Id);
            var threadData = threadRequest.Execute();

            foreach (var message in threadData.Messages)
            {
                foreach (var part in message.Payload.Parts)
                {
                    if (!string.IsNullOrEmpty(part.Filename))
                    {
                        var attachId = part.Body.AttachmentId;
                        var attachRequest = service.Users.Messages.Attachments.Get("me", message.Id, attachId);
                        var attachData = attachRequest.Execute();
                        byte[] data = Convert.FromBase64String(attachData.Data.Replace('-', '+').Replace('_', '/'));

                        var fileName = part.Filename;
                        var folderPath = Path.Combine(Environment.CurrentDirectory, "Attachments");
                        Directory.CreateDirectory(folderPath);
                        var filePath = Path.Combine(folderPath, fileName);

                        File.WriteAllBytes(filePath, data);

                        //Console.WriteLine(fileName.ToString());
                    }
                }
            }
        }
    }
}
