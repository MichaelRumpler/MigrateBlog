using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrateBlog
{
	internal class MinimalMistakesWriter
	{
		protected readonly string targetBasePath = @"D:\MichaelRumpler.github.io";


		internal void WritePost(Post post)
		{
			var targetFilename = Path.Combine(
				targetBasePath,
				"_posts",
				$"{post.Date:yyyy-MM-dd}-{post.Slug}.md");

			StringBuilder sb = new StringBuilder();

			var title = post.Title
				.Replace("&hellip;", "...")
				;
			sb.AppendLine("---");
			sb.AppendLine($"title: {title}");
			sb.AppendLine($"author: {post.Author}");
			sb.AppendLine($"date: {post.Date:yyyy-MM-dd}");
			sb.AppendLine($"comments_locked: true");
			sb.AppendLine("classes: wide");

			sb.AppendLine("categories:");
			sb.AppendLine("  - Reiseblog");
			if (post.Categories != null)
			{
				foreach (var category in post.Categories)
					if(category != "2015")		// This breaks jekyll. It doesn't know what to do with an integer at this point.
						sb.AppendLine($"  - {category}");
			}

			if (post.Tags != null && post.Tags.Length > 0)
			{
				sb.AppendLine("tags:");
				foreach (var tag in post.Tags)
					sb.AppendLine($"  - {tag}");
			}

			sb.AppendLine("---");
			sb.AppendLine();
			sb.AppendLine(GetContent(post));

			File.WriteAllText(targetFilename, sb.ToString());
		}

		internal void WriteComment(Comment comment, Post post)
		{
			var folderPath = Path.Combine(
				targetBasePath,
				"_data",
				"comments",
				post.Slug);

			if(!Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);

			var targetFilename = Path.Combine(folderPath, $"comment-{comment.Date.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds}.yml");

			StringBuilder sb = new StringBuilder();

			var parentId = comment.ParentID == Guid.Empty ? comment.Id : comment.ParentID;
			var message = comment.Content
				.Replace("\r\n", "\\r\\n")
				.Replace("\n", "\\r\\n")
				.Replace("\"", "\\\"");
			message = ReplaceUmlauts(message);

			sb.AppendLine($"_id: {comment.Id}");
			sb.AppendLine($"_parent: {parentId}");
			sb.AppendLine($"message: \"{message}\"");
			sb.AppendLine($"name: {comment.Author}");
			sb.AppendLine($"hidden: ''");
			sb.AppendLine($"date: '{comment.Date.ToUniversalTime():O}'");

			File.WriteAllText(targetFilename, sb.ToString());
		}


		/// <summary>
		/// Finds all referenced images, copy the image files to the target folder and change the image reference.
		/// </summary>
		/// <param name="post"></param>
		/// <returns>The post content with the new image references.</returns>
		string GetContent(Post post)
		{
			var content = ReplaceImages(post);
			content = ReplaceVideos(content);
			return ReplaceUmlauts(content);
		}

		string ReplaceImages(Post post)
		{
			var content = post.Content;

			var monthPath = $"{post.Date:yyyy\\/MM}";

			var sourceImagePath = post.Date.Year == 2011
				? "D:\\reiseblog.mrumpler.at\\images\\2011"             // all images are in that single folder
				: $"D:\\reiseblog.mrumpler.at\\images";                 // images are in subfolders for each month
																		// But somehow the images for a blog post from 31.10. ended up in the 11 folder. So I need to copy from the folder from the old image ref.

			var targetImagePath = Path.Combine(targetBasePath, $"assets/images/{post.Date:yyyy\\/MM}");
			if (!Directory.Exists(targetImagePath))
				Directory.CreateDirectory(targetImagePath);

			foreach (var imageRef in GetImageReferences(post.Content))
			{
				var (imageName, thumbName, alt, width, height) = ParseImageRef(imageRef);

				File.Copy(Path.Combine(sourceImagePath, imageName), Path.Combine(targetImagePath, Path.GetFileName(imageName)), true);
				File.Copy(Path.Combine(sourceImagePath, thumbName), Path.Combine(targetImagePath, Path.GetFileName(thumbName)), true);

				imageName = Path.GetFileName(imageName);
				thumbName = Path.GetFileName(thumbName);

				var newImage = $"<a href=\"/assets/images/{monthPath}/{imageName}\"><img src=\"/assets/images/{monthPath}/{thumbName}\" width=\"{width}\" height=\"{height}\" alt=\"{alt}\" border=\"0\" /></a>";

				content = content.Replace(imageRef, newImage);
			}

			return content;
		}

		string[] imagePrefixes = {
				"http://www.mrumpler.at/Reiseblog/image.axd?picture=",		// BlogEngine.NET
				"http://content.mrumpler.at/images/"						// WordPress
			};

		IEnumerable<string> GetImageReferences(string content)
		{
			var end = "</a>";
			foreach (var prefix in imagePrefixes)
			{
				var start = $"<a href=\"{prefix}";
				int i = content.IndexOf(start);

				while (i >= 0)
				{
					int j = content.IndexOf(end, i);
					if (j >= 0)
						yield return content.Substring(i, j + 4 - i);
					else
						Console.WriteLine("No closing tag for image link found!");

					i = content.IndexOf(start, j);
				}
			}
		}

		(string imageName, string thumbName, string alt, string width, string height) ParseImageRef(string imageRef)
		{
			var imageName = GetAttribute(imageRef, "href")
				.Replace(imagePrefixes[0], string.Empty)
				.Replace(imagePrefixes[1], string.Empty);

			var thumbName = GetAttribute(imageRef, "src")
				.Replace(imagePrefixes[0], string.Empty)
				.Replace(imagePrefixes[1], string.Empty);

			var alt = GetAttribute(imageRef, "alt");
			var width = GetAttribute(imageRef, "width");
			var height = GetAttribute(imageRef, "height");
		
			return (imageName, thumbName, alt, width, height);
		}

		string GetAttribute(string htmlSnippet, string attribute)
		{
			var start = $" {attribute}=\"";
			var quote = "\"";
			var s = htmlSnippet.IndexOf(start);
			if (s == -1)
			{
				start = $" {attribute}='";
				quote = "'";
				s = htmlSnippet.IndexOf(start);
			}
			if (s == -1)
					return string.Empty;

			s += start.Length;
			var e = htmlSnippet.IndexOf(quote, s);
			return htmlSnippet.Substring(s, e - s);
		}

		string ReplaceVideos(string content)
		{
			/*
			 * Videos in BlogEngine.NET (we didn't use any in WordPress) look like:
			 * 
<div id="scid:5737277B-5D6D-4f48-ABFC-DD9C333F4C5D:64e96401-691b-4924-9af5-8320a511ff23" class="wlWriterEditableSmartContent" style="margin: 0px; display: inline; float: none; padding: 0px;">
<div><object width="448" height="277"><param name="movie" value="http://www.youtube.com/v/lyaVEl-PiLo?hl=en&amp;hd=1" /></object></div>
</div>
			 * 
<div style="padding: 0px; margin: 0px; display: inline; float: none" id="scid:5737277B-5D6D-4f48-ABFC-DD9C333F4C5D:afc42c9f-4228-45fe-9cdb-daebfa2a7c34" class="wlWriterEditableSmartContent"><div><object width="448" height="252"><param name="movie" value="http://www.youtube.com/v/JeOWc1PXPu8?hl=en&amp;hd=1"></param><embed src="http://www.youtube.com/v/JeOWc1PXPu8?hl=en&amp;hd=1" type="application/x-shockwave-flash" width="448" height="252"></embed></object></div></div>

			 */

			int i = 0;
			while((i = content.IndexOf("id=\"scid:5737277B-5D6D-4f48-ABFC-DD9C333F4C5D:", i)) > -1)
			{
				var s = content.LastIndexOf("<div ", i);
				i += "id=\"scid:5737277B-5D6D-4f48-ABFC-DD9C333F4C5D:".Length;
				if (s == -1) continue;

				var j = content.IndexOf("<div><object ", i);
				if (j == -1) continue;

				j = content.IndexOf("www.youtube.com/v/", j);
				if (j == -1) continue;

				j += "www.youtube.com/v/".Length;
				var e = content.IndexOf("?", j);
				if (e == -1) continue;

				var videoId = content.Substring(j, e-j);

				e = content.IndexOf("</div>", e);
				if (e == -1) continue;
				e += "</div>".Length;
				e = content.IndexOf("</div>", e);
				if (e == -1) continue;
				e += "</div>".Length;


				var oldVideoRef = content.Substring(s, e-s);
				var newVideoRef = $"{{% include video id=\"{videoId}\" provider=\"youtube\" %}}";

				content = content.Replace(oldVideoRef, newVideoRef);
			}

			return content;
		}

		string ReplaceUmlauts(string content)
			=> content
				.Replace("Ã¤", "ä")
				.Replace("Ã¶", "ö")
				.Replace("Ã¼", "ü")
				.Replace("Ã–", "Ö")     // Ä missing
				.Replace("Ãœ", "Ü")
				.Replace("ÃŸ", "ß")
				.Replace("Â²", "²")
				.Replace("â€™", "´")
				.Replace("â€¦", "&hellip;")
				.Replace("â€“", "-")
				.Replace("â€œ", "&raquo;")
				.Replace("â€\u009d", "&laquo;");
	}
}
