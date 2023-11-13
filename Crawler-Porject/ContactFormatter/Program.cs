using Crawler;
using System.Text.Json;
using System.Web;

class Program
{
	public static readonly string CONTACTS_PATH = @"..\..\..\..\Data";
	public static readonly string CONTACTS_FILENAME = "contacts.json";

	static void Main()
	{
		DistinctSudios();
		//UrlDecode();
	}

	private static void DistinctSudios()
	{
		string contactsFilePath = Path.Combine(CONTACTS_PATH, CONTACTS_FILENAME);
		var json = File.ReadAllText(contactsFilePath);

		var allContacts = JsonSerializer.Deserialize<List<Contact>>(json);
		if(allContacts == null)
			throw new Exception("No data could be read.");

		var distinctStudiosContacts = allContacts.GroupBy(x => x.StudioName).Select(g => g.First()).ToList();
		File.WriteAllText(contactsFilePath, JsonSerializer.Serialize(distinctStudiosContacts, new JsonSerializerOptions { WriteIndented = true }));
	}

	//private static void DistinctSudios()
	//{
	//	string contactsFilePath = Path.Combine(CONTACTS_PATH, CONTACTS_FILENAME);
	//	var json = File.ReadAllText(contactsFilePath);
	//
	//	var allContacts = JsonSerializer.Deserialize<List<Contact>>(json);
	//	if (allContacts == null)
	//		throw new Exception("No data could be read.");
	//
	//	List<string> studios = new List<string>();
	//
	//	for (int i = 0; i < allContacts.Count; ++i)
	//	{
	//		var contact = allContacts[i];
	//		if (studios.Contains(contact.StudioName))
	//			allContacts.RemoveAt(i);
	//		else
	//			studios.Add(contact.StudioName);
	//	}
	//
	//	File.WriteAllText(contactsFilePath, JsonSerializer.Serialize(allContacts, new JsonSerializerOptions { WriteIndented = true }));
	//}

	static void UrlDecode()
	{
		string contactsFilePath = Path.Combine(CONTACTS_PATH, CONTACTS_FILENAME);

		var encodedUrl = File.ReadAllText(contactsFilePath);
		string decodedUrl = HttpUtility.UrlDecode(encodedUrl);

		File.WriteAllText(contactsFilePath, decodedUrl);
	}
}

