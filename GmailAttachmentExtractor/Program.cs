using SQLitePCL;

Batteries.Init();

var gmailExtractor = new GmailAttachmentExtractor();
await gmailExtractor.ExtractAllAttachments(false);


