# FootballManager — חבילת קוד ומסד נתונים

## מה כלול

- `FootballManager.WPF.slnx` — קובץ ה־Solution.
- `FootballManager.WPF` — ממשק WPF, Views, ViewModels, Models, Services, Images ו־Resources.
- `FootballManager.WCF` — שירות CoreWCF, לוגיקה עסקית, גישה ל־Access וכלי יצירת חשבונות מקומיים.
- `FootballManager.Contracts` — DTOs וחוזים משותפים ללקוח ולשירות.
- `FootballManager.WCF/Data/FootballManager.accdb` — מסד Access עם שמונת מבני הטבלאות, קשרים ושלושת חשבונות ההדגמה.

## הפעלה

יש להתקין Microsoft Access Database Engine בגרסת 64-bit, ולוודא שהיא תואמת לגרסת Office המותקנת במחשב.

פתח PowerShell בתיקיית הפתרון והריץ:

```powershell
dotnet build .\FootballManager.WPF.slnx
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj
```

השאר את חלון השירות פתוח. בחלון PowerShell נוסף, מאותה תיקייה, הפעל את WPF:

```powershell
dotnet run --project .\FootballManager.WPF\FootballManager.WPF.csproj
```

השירות מאזין ב־localhost בלבד בכתובת `http://localhost:5152/FootballManagerService`. הוא אינו נגיש ממחשבים אחרים.

## חשבונות הדגמה

המסד שסופק כבר מכיל חשבון אחד מכל Role: `Player`, `Manager` ו־`Admin`. סיסמאות Player ו־Manager נוצרו באקראי, הוצגו פעם אחת בעת יצירתן ונמסרו בנפרד לבעל הפרויקט. במסד נשמרו hashes בלבד.

אין צורך להריץ שוב את כלי יצירת חשבונות ההדגמה. אם יוצרים מסד חדש, יש ליצור Admin מקומי ואז ליצור את חשבונות ההדגמה:

```powershell
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --create-admin
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --seed-demo-users
```

## הגשה

הגש את קובץ ה־ZIP יחד עם הסרטון בנפרד. תיקיות `bin`, `obj`, `.vs`, קובצי `*.csproj.user` ופרויקט ה־API הישן אינם חלק מהחבילה.
