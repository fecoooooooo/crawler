using CsvHelper;
using CsvHelper.Configuration;
using HtmlAgilityPack;
using System.Formats.Asn1;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using File = System.IO.File;

class Program
{
	static HashSet<string> visitedUrls = new HashSet<string>();
	static Queue<string> urlsToVisit = new Queue<string>();
	static List<Contact> allContacts = new List<Contact>();
	static readonly string[] TAGS = { "pixelated", "stylized" };
	static readonly double MIN_RATING = 3.5;

	static readonly string savePath = @"..\..\..\..\Data";
	static readonly string saveFileName = "save.json";

	static void Main(string[] args)
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

			if(ContainsTags(document) && OverMinRating(document, MIN_RATING))
				ExtractContacts(document);

			visitedUrls.Add(currentUrl);
		}

		WriteCSW();
	}

	private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
	{
		Save();
	}

	private static bool OverMinRating(HtmlDocument document, double minRating)
	{
		var ratingNode = document.DocumentNode.SelectSingleNode($"//div[contains(@aria-label,'stars out of five stars')]");
		if(ratingNode != null)
		{
			var ratingString = ratingNode.Attributes["aria-label"].Value;

			Regex regex = new Regex(@"\d+(\.\d+)?");
			Match match = regex.Match(ratingString);
			string value = match.Value;
			double number = double.Parse(value);

			if(minRating <= number)
				return true;
		}

		return false;
	}

	private static void AddConsoleUI(string currentUrl)
	{
		Console.Clear();
		Console.WriteLine("Scanned:" + visitedUrls.Count);
		Console.WriteLine("Remaining: " + urlsToVisit.Count);
		Console.WriteLine("Current: " + currentUrl);
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
			response.EnsureSuccessStatusCode();

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
			//Console.WriteLine(hrefValue);
			
			urlsToVisit.Enqueue(hrefValue);
		}
	}

	private static void ExtractContacts(HtmlDocument document)
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

		allContacts.Add(contact);
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
		if (!Directory.Exists(savePath))
			Directory.CreateDirectory(savePath);

		SaveData saveData = new SaveData()
		{
			visitedUrls = visitedUrls,
			urlsToVisit = urlsToVisit,
			allContacts = allContacts,
		};

		string jsonString = JsonSerializer.Serialize(saveData);

		string saveFilePath = Path.Combine(savePath, saveFileName);
		File.WriteAllText(saveFilePath, jsonString);
	}

	private static void LoadIfPossible()
	{
		string saveFilePath = Path.Combine(savePath, saveFileName);

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

class Contact
{
	public string Url { get; set; } = null!;
	public string StudioName { get; set; } = null!;
	public string GameName { get; set; } = null!;
	public string Email { get; set; } = null!;
}

[Serializable]
class SaveData
{
	public HashSet<string> visitedUrls { get; set; } = null!;
	public Queue<string> urlsToVisit { get; set; } = null!;
	public List<Contact> allContacts { get; set; } = null!;
}