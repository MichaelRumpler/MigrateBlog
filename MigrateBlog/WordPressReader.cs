namespace MigrateBlog;

public class WordPressReader : SQLiteReaderBase
{
    public WordPressReader(string filename) : base(filename) { }

	protected override IEnumerable<Post> GetPosts()
	{
		var postCategories = GetPostCategories();

		using var command = connection.CreateCommand();
		command.CommandText = "select p.ID, u.user_login, post_date, post_title, post_name, post_content from wp_posts p, wp_users u where post_type = 'post' and post_status = 'publish' and p.post_author = u.ID";

		var emptyList = new List<string>();
		var posts = new List<Post>();
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				var id = reader.GetInt32(0);
				var author = reader.GetString(1);
				var date = reader.GetDateTime(2);
				var title = reader.GetString(3);
				var slug = reader.GetString(4);
				var content = reader.GetString(5);

				if (!postCategories.TryGetValue(id, out var categories))
					categories = emptyList;

				posts.Add(new Post(id, author, date, title, slug, content, categories.ToArray(), emptyList.ToArray()));
			}
		}

		return posts;
	}

	private Dictionary<int, List<string>> GetPostCategories()
	{
		using var metaCmd = connection.CreateCommand();
		metaCmd.CommandText = "select object_id, name from wp_term_relationships r, wp_terms t where object_id in (select ID from wp_posts where post_type = 'post' and post_status = 'publish') and term_taxonomy_id = t.term_id";

		Dictionary<int, List<string>> categories = new();
		using (var reader = metaCmd.ExecuteReader())
		{
			while (reader.Read())
			{
				var postId = reader.GetInt32(0);
				var category = reader.GetString(1);

				if (categories.TryGetValue(postId, out var postCategories))
					postCategories.Add(category);
				else
					categories.Add(postId, new List<string>([category]));
			}
		}

		return categories;
	}

	protected override IEnumerable<Comment> GetComments()
	{
		using var command = connection.CreateCommand();
		command.CommandText = "select comment_ID, comment_post_ID, comment_author, comment_date, comment_content, comment_parent from wp_comments";

		var commentGuids = new Dictionary<int, Guid>();
		var comments = new List<Comment>();
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				var id = reader.GetInt32(0);
				var postId = reader.GetInt32(1);
				var author = reader.GetString(2);
				var date = reader.GetDateTime(3);
				var content = reader.GetString(4);
				var parentId = reader.GetInt32(5);

				if(!commentGuids.TryGetValue(id, out var guid))
				{
					guid = new Guid();
					commentGuids.Add(id, guid);
				}

				if (!commentGuids.TryGetValue(parentId, out var parentGuid))
				{
					parentGuid = new Guid();
					commentGuids.Add(parentId, parentGuid);
				}

				comments.Add(new Comment(guid, postId, author, date, content, parentGuid));
			}
		}

		return comments;
	}
}
