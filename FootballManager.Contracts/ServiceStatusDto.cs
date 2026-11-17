using System.Runtime.Serialization;

namespace FootballManager.Contracts;

/// <summary>תוצאת בדיקת זמינות שמחזיר השירות כדי שהלקוח יידע אם WCF ומסד Access מוכנים.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class ServiceStatusDto
{
    [DataMember(Order = 1)]
    public string ApplicationName { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public bool DatabaseReady { get; set; }

    [DataMember(Order = 3)]
    public string Message { get; set; } = string.Empty;
}

/// <summary>בקשת הרשמה שמעבירה שם משתמש וסיסמה מהלקוח אל השירות. השרת מאמת ומגבה את הסיסמה לפני שמירת חשבון Player.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class RegisterRequestDto
{
    [DataMember(Order = 1)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Password { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Role { get; set; } = "Player";
}

/// <summary>בקשת אדמין ליצור משתמש מנוהל עם שם משתמש, תפקיד וסיסמה; השרת בודק הרשאה ומגבה את הסיסמה.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class CreateManagedUserRequestDto
{
    [DataMember(Order = 1)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Password { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Role { get; set; } = "Player";
}

/// <summary>תשובה כללית לפעולות שירות כגון יצירה, עדכון ומחיקה: דגל הצלחה והודעה למסך.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class OperationResultDto
{
    [DataMember(Order = 1)]
    public bool Succeeded { get; set; }

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;
}

/// <summary>בקשת כניסה המעבירה שם משתמש וסיסמה ל־WCF לצורך אימות מול גיבוב הסיסמה שבמסד.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class LoginRequestDto
{
    [DataMember(Order = 1)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>תשובת כניסה הכוללת הצלחה או כשל, הודעה, ובכניסה תקינה גם אסימון Session, שם משתמש ו־Role שנקראו מהשרת.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class LoginResponseDto
{
    [DataMember(Order = 1)]
    public bool Succeeded { get; set; }

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string SessionToken { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public string Role { get; set; } = string.Empty;
}

/// <summary>נתונים שאדמין רשאי לראות על חשבון משתמש, כגון מזהה, שם, תפקיד ומצב חסימה; אינו כולל סיסמה.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class ManagedUserDto
{
    [DataMember(Order = 1)]
    public int UserId { get; set; }

    [DataMember(Order = 2)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Role { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public bool IsBlocked { get; set; }

    /// <summary>ממירה את נתוני האובייקט למחרוזת תצוגה עבור המסך או הרשימה. נקראת בקוד מתוך Read Player, Read Team.</summary>
    /// <returns>מחרוזת שמייצגת את תוצאת הפעולה.</returns>
    public override string ToString()
    {
        return $"{Username} | {Role} | {(IsBlocked ? "חסום" : "פעיל")}";
    }
}

/// <summary>מעטפת תשובת השירות לניהול משתמשים, הכוללת הודעת מצב ורשימת ManagedUserDto.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class ManagedUsersResponseDto
{
    [DataMember(Order = 1)]
    public bool Succeeded { get; set; }

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public List<ManagedUserDto> Users { get; set; } = [];
}
