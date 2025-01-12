using SQLite;
using SQLitePCL;

Batteries.Init();

//using (var db = new SQLiteContext())
//{
//    db.Database.EnsureDeleted();
//    db.Database.EnsureCreated();
//}




var gmailExtractor = new GmailAttachmentExtractor();
await gmailExtractor.ExtractAllAttachments(false);


