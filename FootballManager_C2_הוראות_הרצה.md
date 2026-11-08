# FootballManager — הוראות הרצה והגשה

## מה כלול

- `FootballManager.WPF.slnx` ו־`FootballManager.WPF.sln` — קובצי הפתרון.
- `FootballManager.WPF` — לקוח WPF, כולל Views, ViewModels, Models, Services, Images ו־Resources.
- `FootballManager.WCF` — שירות CoreWCF, לוגיקה עסקית, אתר מקומי וגישה למסד Access.
- `FootballManager.Contracts` — חוזים ו־DTOs משותפים.
- `FootballManager.WCF/Data/FootballManager.accdb` — מסד Access עם שמונה טבלאות DSD ונתוני הדגמה.

## דרישות והרצה

יש להתקין .NET 10 SDK ואת Microsoft Access Database Engine (ACE OLE DB) בגרסת 64-bit.

פתח PowerShell בתיקיית הפתרון והריץ:

```powershell
dotnet build .\FootballManager.WPF.slnx
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj
```

השאר את חלון השירות פתוח. האתר יעלה ב־`http://localhost:5152/`, ושירות SOAP של WCF ב־`http://localhost:5152/FootballManagerService`. להפעלת יישום WPF, פתח חלון PowerShell נוסף באותה תיקייה והריץ:

```powershell
dotnet run --project .\FootballManager.WPF\FootballManager.WPF.csproj
```

האתר ולקוח WPF משתמשים בשירות. רק שירות WCF ניגש למסד Access. השירות מאזין ל־localhost בלבד.

## חשבונות ונתוני הדגמה

קובץ Access שסופק מכיל חשבון Admin אחד, `DemoManager` ו־`DemoPlayer`. נשמרו במסד hashes של הסיסמאות בלבד. הסיסמאות נמצאות בקובץ פרטי נפרד ואינן חלק מקובץ ה־ZIP.

נתוני C2 כוללים שלוש קבוצות, שנים־עשר שחקנים ושנים־עשר שיוכי סגל. טבלאות `Matches`, `PlayerStatistics` ו־`Transfers` קיימות ונשארות ריקות, בהתאם להיקף המחזור.

במסד חדש אפשר ליצור Admin, משתמשי הדגמה ונתוני תוכן באמצעות:

```powershell
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --create-admin
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --seed-demo-users
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --seed-demo-content
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --prepare-demo-accounts
```

הפקודה האחרונה מגדירה סיסמאות אקראיות ומציגה אותן פעם אחת. יש לשמור אותן בקובץ פרטי ולא לצרף ל־ZIP.

## תכולת ההגשה

הגש את מסמך הדרישות, ZIP קוד המקור, קובץ Access וסרטון ההדגמה. אין לכלול תיקיות `bin`, `obj`, `.vs`, קובצי משתמש של Visual Studio או סיסמאות גלויות ב־ZIP.
