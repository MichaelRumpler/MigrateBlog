using System.Collections.Immutable;
using Microsoft.Data.Sqlite;


namespace MigrateBlog
{
	public abstract class SQLiteReaderBase : IDisposable
	{
		protected SqliteConnection connection;

		public ImmutableArray<Post> Posts { get; protected set; }
		public ImmutableArray<Comment> Comments { get; protected set; }


        public SQLiteReaderBase(string filename)
        {
			connection = new SqliteConnection($"Data Source={filename}");
			connection.Open();
		}

		public void Read()
		{
			Posts = GetPosts().ToImmutableArray();
			Comments = GetComments().ToImmutableArray();
		}

		protected abstract IEnumerable<Post> GetPosts();

		protected abstract IEnumerable<Comment> GetComments();

		public void Dispose()
		{
			connection?.Dispose();
			connection = null;
		}
	}
}
