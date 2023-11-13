using Crawler;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Web;

class Program
{
	const int EMAIL_PER_RUN = 100;
	public static readonly string CONTACTS_PATH = @"..\..\..\..\Data";
	public static readonly string CONTACTS_FILENAME = "contacts.json";

	static List<Contact> allContacts = null!;

	static void Main()
	{
		LoadAllContacts();

		var contactsToSendMailTo = GetContactsToSendMailTo();

		SendMail(contactsToSendMailTo);

		Save(allContacts);
	}

	private static void LoadAllContacts()
	{
		string contactsFilePath = Path.Combine(CONTACTS_PATH, CONTACTS_FILENAME);

		string json = File.ReadAllText(contactsFilePath);

		var allContactsTemp = JsonSerializer.Deserialize<List<Contact>>(json);
		if (allContactsTemp == null)
			throw new Exception("No data could be read.");

		allContacts = allContactsTemp;
	}

	private static List<Contact> GetContactsToSendMailTo()
	{
		var contactsToSendMailTo = allContacts
		.Where(x => x.EmailSent == false)
		.OrderByDescending(x => x.Rating).ThenByDescending(x => x.ReviewCount)
		.Take(EMAIL_PER_RUN)
		.ToList();

		return contactsToSendMailTo;
	}

	private static void Save(List<Contact> allContacts)
	{
		string jsonString = JsonSerializer.Serialize(allContacts, new JsonSerializerOptions { WriteIndented = true });
		string decodedJson = HttpUtility.UrlDecode(jsonString);

		string contactsFilePath = Path.Combine(CONTACTS_PATH, CONTACTS_FILENAME);
		File.WriteAllText(contactsFilePath, decodedJson);
	}

	static void SendMail(List<Contact> contacts)
	{
		foreach(var c in contacts)
		{
			string fromEmail = "dajka1989@gmail.com";
			string password = "amks exoi khhf pifi";

			MailMessage mail = new MailMessage();
			mail.From = new MailAddress(fromEmail);
			
			mail.To.Add(c.Email);

			mail.Subject = "Collaboration";
			mail.Body = GetMailBodyWithContactInfo(c);

			SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
			smtpClient.Port = 587;
			smtpClient.Credentials = new NetworkCredential(fromEmail, password);
			smtpClient.EnableSsl = true;

			try
			{
				//smtpClient.Send(mail);
				allContacts.Where(x => x.StudioName == c.StudioName).ToList().ForEach(x => x.EmailSent = true);
				Console.WriteLine(mail.Body);
                Console.ReadKey();
				Console.Clear();
            }
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
		}

		
	}

	const string NAME_TOKEN= "<name>";
	const string GAME_TOKEN = "<game>";

	private static string GetMailBodyWithContactInfo(Contact c)
	{
		string msg = File.ReadAllText("template.txt");
		msg = msg.Replace(NAME_TOKEN, c.StudioName);
		msg = msg.Replace(GAME_TOKEN, c.GameName);

		return msg;
	}
}