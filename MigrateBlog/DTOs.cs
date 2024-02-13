namespace MigrateBlog;

public record Post(int Id, string Author, DateTime Date, string Title, string Slug, string Content, string[] Categories, string[] Tags);
public record Comment(Guid Id, int PostID, string Author, DateTime Date, string Content, Guid ParentID);
