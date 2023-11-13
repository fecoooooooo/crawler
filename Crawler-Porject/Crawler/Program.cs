using Crawler;
using CsvHelper;
using CsvHelper.Configuration;
using HtmlAgilityPack;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using File = System.IO.File;

class Program
{
	static HashSet<string> visitedUrls = new HashSet<string>();
	static Queue<string> urlsToVisit = new Queue<string>();
	static List<Contact> allContacts = new List<Contact>();
	static readonly string[] TAGS = { "pixelated", "stylized" };
	static readonly double MIN_RATING = 4.5;

	static void Main(string[] args)
	{
		try
		{
			LoadIfPossible();

			AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
			urlsToVisit.Enqueue("https://play.google.com/store/apps/details?id=cc.peacedeath.peacedeath&hl=en_US");

			while (urlsToVisit.Count > 0)
			{
				string currentUrl = urlsToVisit.Dequeue();

				AddConsoleUI(currentUrl);

				if (visitedUrls.Contains(currentUrl))
					continue;

				string htmlContent = DownloadHtmlContent(currentUrl);

				HtmlDocument document = new HtmlDocument();
				document.LoadHtml(htmlContent);

				ExtractLinks(document);

				if (ContainsTags(document) && OverMinRating(document, MIN_RATING))
					ExtractContacts(document, currentUrl);

				visitedUrls.Add(currentUrl);
			}

			WriteCSW();
		}
		catch
		{
			Save();
		}
		Console.ReadKey();
	}

	private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
	{
		Save();
	}

	private static bool OverMinRating(HtmlDocument document, double minRating)
	{
		return GetRating(document).HasValue && minRating <= GetRating(document)!.Value;
	}

	private static double? GetRating(HtmlDocument document)
	{
		var ratingNode = document.DocumentNode.SelectSingleNode($"//div[contains(@aria-label,'stars out of five stars')]");
		if (ratingNode != null)
		{
			var ratingString = ratingNode.Attributes["aria-label"].Value;

			Regex regex = new Regex(@"\d+(\.\d+)?");
			Match match = regex.Match(ratingString);
			string value = match.Value;
			double rating = double.Parse(value);

			return rating;
		}

		return null;
	}

	private static void AddConsoleUI(string currentUrl)
	{
		Console.SetCursorPosition(0, 0);
		Console.WriteLine("Scanned:" + visitedUrls.Count);
		Console.WriteLine("Remaining: " + urlsToVisit.Count);
		Console.WriteLine("Contacts found: " + allContacts.Count);
		Console.WriteLine("Current URL: " + currentUrl);
	}

	private static bool ContainsTags(HtmlDocument document)
	{
		foreach(var t in TAGS)
		{
			var tagNode = document.DocumentNode.SelectSingleNode($"//span[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{t}')]");
			if (tagNode != null)
				return true;
		}

		return false;
	}

	static string DownloadHtmlContent(string url)
	{
		using (HttpClient client = new HttpClient())
		{
			HttpResponseMessage response = client.GetAsync(url).Result;
			//response.EnsureSuccessStatusCode();

			string htmlContent = response.Content.ReadAsStringAsync().Result;
			return htmlContent;
		}
	}

	static void ExtractLinks(HtmlDocument document)
	{
		var nodes = document.DocumentNode.SelectNodes("//a[starts-with(@href, '/store/apps/details')]");
		if (nodes == null)
			return;

		foreach (HtmlNode linkNode in nodes)
		{
			string hrefValue = linkNode.Attributes["href"].Value;
			hrefValue = "https://play.google.com" + hrefValue;
			
			urlsToVisit.Enqueue(hrefValue);
		}
	}

	private static void ExtractContacts(HtmlDocument document, string currentUrl)
	{
		Contact contact = new Contact();

		var emailLabelNode = document.DocumentNode.SelectNodes("//div[text()='Email']").FirstOrDefault();
		if(emailLabelNode != null)
			contact.Email = emailLabelNode.ParentNode.ChildNodes[1].InnerHtml;

		var devHref = document.DocumentNode.SelectNodes("//a[starts-with(@href, '/store/apps/dev')]").FirstOrDefault();
		if(devHref != null)
			contact.StudioName = devHref.ChildNodes[0].InnerHtml;

		var gameName = document.DocumentNode.SelectSingleNode("//h1[@itemprop='name']");
		if(gameName != null)
			contact.GameName = gameName.ChildNodes[0].InnerHtml;

		contact.Rating = GetRating(document);
		contact.ReviewCount = GetReviewCount(document);
		contact.Url = currentUrl;

		allContacts.Add(contact);
	}
	private static double? GetReviewCount(HtmlDocument document)
	{
		var ratingNode = document.DocumentNode.SelectSingleNode($"//div[contains(text(),'reviews')]");
		if (ratingNode != null && 2 <= ratingNode.ParentNode.ChildNodes.Count )
		{
			var reviewCountNode = ratingNode.ParentNode.ChildNodes[1];
			var reviewCountText = reviewCountNode.InnerText.Split(' ')[0];

			int multiplier = 1;
			if (reviewCountText[reviewCountText.Length - 1] == 'K')
			{
				multiplier = 1000;
				reviewCountText = reviewCountText.Substring(0, reviewCountText.Length - 1);
			}
			else if (reviewCountText[reviewCountText.Length - 1] == 'M')
			{
				multiplier = 1000000;
				reviewCountText = reviewCountText.Substring(0, reviewCountText.Length - 1);
			}

			double reviewCount = double.Parse(reviewCountText) * multiplier;

			return reviewCount;
		}

		return null;
	}

	private static void WriteCSW()
	{
		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			Delimiter = ","
		};

		using (var writer = new StringWriter())
		using (var csv = new CsvWriter(writer, config))
		{
			csv.WriteRecords(allContacts);
			var csvString = writer.ToString();
			
			string path = @"..\result.txt";
			File.WriteAllText(path, csvString);
		}

		throw new Exception("Egyedi emailcimekre szures, ne menjen el ugynaza az uzenet 2x");
	}

	private static void Save()
	{
		if (!Directory.Exists(SaveData.SAVE_PATH))
			Directory.CreateDirectory(SaveData.SAVE_PATH);

		SaveData saveData = new SaveData()
		{
			visitedUrls = visitedUrls,
			urlsToVisit = urlsToVisit,
			allContacts = allContacts,
		};

		string jsonString = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });

		string saveFilePath = Path.Combine(SaveData.SAVE_PATH, SaveData.SAVE_FILENAME);
		File.WriteAllText(saveFilePath, jsonString);
	}

	private static void LoadIfPossible()
	{
		string saveFilePath = Path.Combine(SaveData.SAVE_PATH, SaveData.SAVE_FILENAME);

		if (File.Exists(saveFilePath))
		{
			var jsonString = File.ReadAllText(saveFilePath);
			object? savedObj = JsonSerializer.Deserialize(jsonString, typeof(SaveData));
			if(savedObj is SaveData)
			{
				SaveData saveData = (SaveData)savedObj;
				visitedUrls = saveData.visitedUrls;
				urlsToVisit = saveData.urlsToVisit;
				allContacts = saveData.allContacts;
			}
		}
	}
}


