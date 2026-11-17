# FootballManager — העברות וסטטיסטיקות, סבב 4

הפתרון כולל את לקוח WPF, שירות SOAP של CoreWCF, האתר הקיים ופרויקט FootballManager.Contracts המשותף. מסד ההדגמה נמצא ב־FootballManager.WCF/Data/FootballManager.accdb. הוא מכיל גם את נתוני C1–C3, וכן העברות וסטטיסטיקות משחק לסבב 4.

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

## התנסות בהעברות ובסטטיסטיקות

1. נכנסים ל־WPF עם DemoManager ופותחים **העברות וסטטיסטיקות** בדשבורד. בשתי הכרטיסיות יש רשימה, טופס ופקודות לשליפה, הוספה, עדכון ומחיקה. לאחר שמירה הרשימה נטענת מחדש מהשירות.
2. אחרי שמירת העברה, מתנתקים ונכנסים עם DemoPlayer. כפתור **בדיקת הרשאה להעברה** בדשבורד שולח את אותה בקשת שמירה דרך WCF ומציג `403 Forbidden`. נתוני ההעברה אינם משתנים. בקשה ללא התחברות מקבלת `401 Unauthorized`. Admin ממשיך לנהל משתמשים ואינו מורשה ל־C4.
3. במסכי WPF הקיימים אפשר לפתוח משחקי C3, שחקנים, קבוצות וסגלים של C2 ולבדוק את כניסת C1.

## סיסמאות להדגמה מקומית

המסד מכיל גיבובי סיסמאות ב־Users.PasswordHash. הסיסמאות נמסרו בנפרד בחבילת C2. כדי לקבל סיסמאות הדגמה חדשות, עוצרים את השירות ומריצים על עותק עבודה מקומי את הפקודה הבאה. היא מקצה סיסמאות לשלושת חשבונות ההדגמה ומציגה אותן פעם אחת. לאחר מכן מפעילים שוב את השירות.

```powershell
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --prepare-demo-accounts
```
