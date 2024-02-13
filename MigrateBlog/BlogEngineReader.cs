namespace MigrateBlog;

public class BlogEngineReader : SQLiteReaderBase
{
	Dictionary<Guid, int> postIds = new Dictionary<Guid, int>();

    public BlogEngineReader(string filename) : base(filename) { }

	protected override IEnumerable<Post> GetPosts()
	{
		var postCategories = GetPostCategories();
		var postTags = GetPostTags();

		using var command = connection.CreateCommand();
		command.CommandText = "select PostID, Author, DateCreated, Title, Slug, PostContent from be_posts where IsDeleted = '0'";

		var beginNZ = new DateTime(2011, 8, 12);
		var endNZ = new DateTime(2011, 9, 3);
		var beginAU = new DateTime(2011, 9, 2);
		var endAU = new DateTime(2011, 10, 4);
		var beginTH = new DateTime(2011, 10, 4);
		var emptyList = new List<string>();
		var postId = 0;
		var posts = new List<Post>();
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				var guid = reader.GetGuid(0);
				var author = reader.GetString(1);
				var date = reader.GetDateTime(2);
				var title = reader.GetString(3);
				var slug = reader.GetString(4);
				var content = reader.GetString(5);

				postIds[guid] = postId;

				if (!postCategories.TryGetValue(guid, out var categories))
					categories = emptyList;
				if (date < beginNZ)
				{
					categories.Add("Schweden");
					categories.Add("Norwegen");
				}
				else if ((date > beginNZ && date < endNZ))
					categories.Add("Neuseeland");
				else if ((date > beginAU && date < endAU))
					categories.Add("Australien");
				else if ((date > beginTH))
					categories.Add("Thailand");

				if (!postTags.TryGetValue(guid, out var tags))
					tags = emptyList;
				tags.Remove("Schweden");
				tags.Remove("Norwegen");
				tags.Remove("Neuseeland");
				tags.Remove("Australien");
				tags.Remove("Thailand");

				posts.Add(new Post(postId, author, date, title, slug, content, categories.ToArray(), tags.ToArray()));

				postId++;
			}
		}

		return posts;
	}

	private Dictionary<Guid, List<string>> GetPostCategories()
	{
		using var metaCmd = connection.CreateCommand();
		metaCmd.CommandText = "select pc.PostID, c.CategoryName from be_postcategory pc, be_categories c where pc.CategoryID = c.CategoryID";

		Dictionary<Guid, List<string>> categories = new();
		using (var reader = metaCmd.ExecuteReader())
		{
			while (reader.Read())
			{
				var postId = reader.GetGuid(0);
				var category = reader.GetString(1);

				if (category == "Weltreise 2011")
					category = "NZ/AU/TH-2011";

				if (categories.TryGetValue(postId, out var postCategories))
					postCategories.Add(category);
				else
					categories.Add(postId, new List<string>([category]));
			}
		}

		return categories;
	}

	private Dictionary<Guid, List<string>> GetPostTags()
	{
		using var metaCmd = connection.CreateCommand();
		metaCmd.CommandText = "select PostID, Tag from be_posttag";

		Dictionary<Guid, List<string>> tags = new();
		using (var reader = metaCmd.ExecuteReader())
		{
			while (reader.Read())
			{
				var postId = reader.GetGuid(0);
				var tag = reader.GetString(1);

				if (tags.TryGetValue(postId, out var postTags))
					postTags.Add(tag);
				else
					tags.Add(postId, new List<string>([tag]));
			}
		}

		return tags;
	}

	protected override IEnumerable<Comment> GetComments()
	{
		using var command = connection.CreateCommand();
		command.CommandText = "select PostCommentID, PostID, Author, CommentDate, Comment, ParentCommentID from be_postcomment where IsSpam = '0' and PostID in (select PostID from be_posts where IsDeleted = '0')";

		var comments = new List<Comment>();
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				var guid = reader.GetGuid(0);
				var postGuid = reader.GetGuid(1);
				var author = reader.GetString(2);
				var date = reader.GetDateTime(3);
				var content = reader.GetString(4);
				var parentGuid = reader.GetGuid(5);

				comments.Add(new Comment(guid, postIds[postGuid], author, date, content, parentGuid));
			}
		}

		return comments;
	}
}
