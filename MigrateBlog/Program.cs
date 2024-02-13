namespace MigrateBlog
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var writer = new MinimalMistakesWriter();

			MigrateBlog(new BlogEngineReader(@"D:\reiseblog.mrumpler.at\blogengine.sqlite"), writer);
			MigrateBlog(new WordPressReader(@"D:\reiseblog.mrumpler.at\WordPressDB.db"), writer);
		}

		static void MigrateBlog(SQLiteReaderBase db, MinimalMistakesWriter writer)
		{
			db.Read();

			foreach (var post in db.Posts)
			{
				writer.WritePost(post);

				foreach (var comment in db.Comments.Where(c => c.PostID == post.Id))
					writer.WriteComment(comment, post);
			}
		}
	}
}
