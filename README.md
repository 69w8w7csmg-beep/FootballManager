# FootballManager — משחקים ותוצאות, סבב 3

הפתרון כולל את לקוח WPF, שירות SOAP של CoreWCF, אתר C2 ואת פרויקט FootballManager.Contracts המשותף. מסד ההדגמה נמצא ב־FootballManager.WCF/Data/FootballManager.accdb. הוא כבר מכיל את החשבונות, הקבוצות, השחקנים והמשחקים הדרושים להדגמה.

## הפעלה מקומית ב־Windows

נדרשים .NET 10 SDK ו־Microsoft ACE OLE DB 16.0 בגרסת 64 סיביות. מחלצים את קוד המקור לתיקייה מקומית שיש בה הרשאת כתיבה. מתיקיית הפתרון מפעילים ב־PowerShell:

```powershell
dotnet build .\FootballManager.WPF.sln
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj
```

משאירים את השירות פועל. מחלון PowerShell שני, באותה תיקייה, מפעילים את הלקוח:

```powershell
dotnet run --project .\FootballManager.WPF\FootballManager.WPF.csproj
```

כתובת השירות היא http://localhost:5152/FootballManagerService והאתר של C2 זמין ב־http://localhost:5152/. רק השרת פותח את קובץ Access.

## התנסות במשחקים

1. נכנסים ל־WPF עם DemoManager ובוחרים במשחקים ותוצאות. בוחרים משחק, משנים תוצאה ושומרים, או יוצרים משחק בין שתי קבוצות קיימות. טעינה חוזרת מציגה את הערכים מהשירות.
2. מתחברים עם DemoPlayer. ניתן לחפש לפי קבוצה ותאריך ולצפות בתוצאות. ניסיון שמירה בחשבון זה מגיע לשרת ומציג 403. בקשת שירות ללא התחברות מקבלת 401. חשבון Admin ממשיך לנהל משתמשים בדשבורד.
3. אפשר לפתוח את רשימת השחקנים או את סגל הקבוצה כדי לראות גם את תוכן C2.

## סיסמאות להדגמה מקומית

המסד מכיל גיבובי סיסמאות. כדי לקבל סיסמאות הדגמה משלכם, עוצרים את השירות ומריצים על עותק העבודה המקומי את הפקודה הבאה. היא מקצה סיסמאות חדשות לשלושת חשבונות ההדגמה ומציגה אותן פעם אחת. לאחר מכן מפעילים שוב את השירות.

```powershell
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --prepare-demo-accounts
```
