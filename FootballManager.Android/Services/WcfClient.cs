// מטרה: קורא מחלון Android לאותו חוזה SOAP/WCF שמשמש את WPF.
// מי משתמש: MainPage של לקוח Android.
// פעולות: CallAsync מקודד בקשה, בודק HTTP וקורא את ה־DTO שבתשובה.
// תלויות: HttpClient, חוזי FootballManager.Contracts וגישור adb reverse.
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;

namespace FootballManager.AndroidClient.Services;

public sealed class WcfClient(HttpClient http)
{
    // הכתובת המקומית מתאימה ל־AllowedHosts של השרת ומועברת מהמכשיר דרך adb reverse.
    private static readonly Uri Address = new("http://localhost:5152/FootballManagerService");
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Service = "urn:footballmanager:services";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>שולח את הפרמטרים לפי סדרם בחוזה וקורא את תשובת WCF. השרת קובע 401 בלי התחברות ו־403 בלי הרשאה.</summary>
    /// <typeparam name="T">DTO התשובה שמוגדר בחוזה.</typeparam>
    /// <param name="operation">שם הפעולה בחוזה ה־SOAP.</param>
    /// <param name="arguments">שמות פרמטרים וערכים לפי חוזה WCF.</param>
    /// <returns>ה־DTO שחזר מהשירות. דחיית HTTP מועברת למסך כ־WcfRequestException.</returns>
    public async Task<T> CallAsync<T>(string operation, params (string Name, object? Value)[] arguments)
    {
        // השמות ומרחבי השמות של מעטפת SOAP תואמים ל־BasicHttpBinding של השרת.
        var payload = new XElement(Soap + "Envelope",
            new XElement(Soap + "Body", new XElement(Service + operation, arguments.Select(WriteArgument))));
        using var request = new HttpRequestMessage(HttpMethod.Post, Address);
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{Service}/IFootballManagerService/{operation}\"");
        request.Content = new StringContent(payload.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new WcfRequestException(response.StatusCode);
        }

        var envelope = XElement.Parse(await response.Content.ReadAsStringAsync());
        var result = envelope.Element(Soap + "Body")?.Element(Service + (operation + "Response"))
            ?.Element(Service + (operation + "Result"));
        if (result is null) throw new SerializationException("תשובת WCF לא כוללת תוצאה.");

        var serializer = new DataContractSerializer(typeof(T), operation + "Result", Service.NamespaceName);
        using var reader = result.CreateReader();
        return serializer.ReadObject(reader) is T value
            ? value
            : throw new SerializationException("סוג התשובה של WCF אינו תואם.");
    }

    /// <summary>מקודד ערך פשוט או DTO עם שם שמוגדר לפרמטר בחוזה.</summary>
    /// <param name="argument">שם פרמטר WCF והערך שמקודד עבורו.</param>
    private static XElement WriteArgument((string Name, object? Value) argument)
    {
        var name = Service + argument.Name;
        return argument.Value switch
        {
            null => new XElement(name, new XAttribute(Xsi + "nil", true)),
            string or int or bool or DateTime => new XElement(name, argument.Value),
            _ => WriteDto(name, argument.Value)
        };
    }

    /// <summary>שומר על שמות ושדות DataContract בזמן כתיבת אובייקט SOAP.</summary>
    /// <param name="name">שם המאפיין, או הערך שמתקבל מהשדה בהתאם לפעולה.</param>
    /// <param name="value">הערך הראשוני לתצוגה.</param>
    private static XElement WriteDto(XName name, object value)
    {
        var serializer = new DataContractSerializer(value.GetType(), name.LocalName, name.NamespaceName);
        var document = new XDocument();
        using (var writer = document.CreateWriter()) serializer.WriteObject(writer, value);
        return document.Root!;
    }
}

/// <summary>מחזיקה את קוד ה־HTTP שחזר מהשרת, כדי שמסך Android יציג מה קרה לבקשה.</summary>
public sealed class WcfRequestException(HttpStatusCode status) : Exception
{
    public HttpStatusCode Status { get; } = status;
}
