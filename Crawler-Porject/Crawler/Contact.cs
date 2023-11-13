namespace Crawler
{
	public class Contact
	{
		public string Url { get; set; } = null!;
		public string StudioName { get; set; } = null!;
		public string GameName { get; set; } = null!;
		public string Email { get; set; } = null!;
		public double? Rating { get; set; }
		public double? ReviewCount { get; set; }
		public bool EmailSent { get; set; } = false;
	}

}
