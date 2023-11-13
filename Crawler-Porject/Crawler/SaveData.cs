namespace Crawler
{
	[Serializable]
	public class SaveData
	{
		public static readonly string SAVE_PATH = @"..\..\..\..\Data";
		public static readonly string SAVE_FILENAME = "save.json";

		public HashSet<string> visitedUrls { get; set; } = null!;
		public Queue<string> urlsToVisit { get; set; } = null!;
		public List<Contact> allContacts { get; set; } = null!;
	}
}
