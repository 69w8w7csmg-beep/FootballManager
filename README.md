# FootballManager C2

פתרון מחזור 2 של FootballManager: לקוח WPF, שירות SOAP באמצעות CoreWCF, אתר הדגמה מקומי ונתוני Microsoft Access.

## מבנה

- `FootballManager.WPF` — לקוח שולחני, Views, ViewModels, Models, Services, Images ו־Resources.
- `FootballManager.WCF` — שירות CoreWCF, פעולות HTTP של האתר, לוגיקת הרשאות וגישה למסד Access.
- `FootballManager.Contracts` — חוזים ו־DTOs משותפים.
- `FootballManager_C2.accdb` — מסד ההגשה; עותק העבודה נמצא גם ב־`FootballManager.WCF/Data`.
- `FootballManager_C2_דרישות_סופיות.docx` — מסמך הדרישות המעודכן.
- `FootballManager_C2_הדגמה.webm` — סרטון הדגמה.

## הפעלה

דרושים .NET 10 SDK ו־Microsoft Access Database Engine (ACE OLE DB) 64-bit.

```powershell
dotnet build .\FootballManager.WPF.slnx
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj
```

השירות והאתר זמינים מקומית ב־`http://localhost:5152/`. להפעלת WPF, בחלון נוסף:

```powershell
dotnet run --project .\FootballManager.WPF\FootballManager.WPF.csproj
```

WPF מתקשר עם WCF באמצעות SOAP. רק WCF ניגש למסד.

פרטי כניסה להדגמה נשמרים מחוץ למאגר; במסד יש hashes בלבד. אין להעלות סיסמאות גלויות.
