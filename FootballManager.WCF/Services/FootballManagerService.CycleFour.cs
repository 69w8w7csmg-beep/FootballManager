// פעולות סבב 4: העברות וסטטיסטיקות משחק עם אכיפת הרשאת מנהל בשרת.
using System.Data.OleDb;
using System.Net;
using CoreWCF;
using CoreWCF.Channels;
using FootballManager.Contracts;

namespace FootballManager.WCF.Services;

public sealed partial class FootballManagerService
{
    public TransfersResponseDto GetTransfers(string sessionToken)
    {
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return new TransfersResponseDto { Message = denial };

        try
        {
            return new TransfersResponseDto
            {
                Succeeded = true,
                Message = "רשימת ההעברות נטענה.",
                Transfers = _transfers.GetTransfers()
            };
        }
        catch (OleDbException)
        {
            return new TransfersResponseDto { Message = "לא ניתן לטעון את ההעברות ממסד Access." };
        }
    }

    public OperationResultDto SaveTransfer(string sessionToken, TransferDto transfer)
    {
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return Failure(denial);
        if (transfer is null || transfer.TransferId < 0 || transfer.PlayerId <= 0 ||
            transfer.FromTeamId <= 0 || transfer.ToTeamId <= 0 ||
            transfer.FromTeamId == transfer.ToTeamId)
            return Failure("יש לבחור שחקן ושתי קבוצות שונות שקיימות במסד.");
        if (transfer.TransferDate.Year < 100 || transfer.Price < 0m ||
            transfer.Price > 922337203685477.5807m ||
            decimal.Round(transfer.Price, 4) != transfer.Price)
            return Failure("יש להזין תאריך תקין ומחיר שאינו שלילי, עם עד ארבע ספרות אחרי הנקודה.");

        try
        {
            return _transfers.Save(transfer)
                ? Success(transfer.TransferId == 0 ? "ההעברה נוספה ונשמרה." : "פרטי ההעברה עודכנו ונשמרו.")
                : Failure("ההעברה לא נמצאה.");
        }
        catch (OleDbException)
        {
            return Failure("ההעברה לא נשמרה. יש לוודא שהשחקן והקבוצות קיימים.");
        }
    }

    public OperationResultDto DeleteTransfer(string sessionToken, int transferId)
    {
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return Failure(denial);
        if (transferId <= 0) return Failure("יש לבחור העברה קיימת.");

        try
        {
            return _transfers.Delete(transferId)
                ? Success("ההעברה נמחקה.")
                : Failure("ההעברה לא נמצאה.");
        }
        catch (OleDbException)
        {
            return Failure("לא ניתן למחוק את ההעברה ממסד Access.");
        }
    }

    public PlayerStatisticsResponseDto GetPlayerStatistics(string sessionToken)
    {
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return new PlayerStatisticsResponseDto { Message = denial };

        try
        {
            return new PlayerStatisticsResponseDto
            {
                Succeeded = true,
                Message = "סטטיסטיקות המשחק נטענו.",
                Statistics = _statistics.GetStatistics()
            };
        }
        catch (OleDbException)
        {
            return new PlayerStatisticsResponseDto { Message = "לא ניתן לטעון את הסטטיסטיקות ממסד Access." };
        }
    }

    public OperationResultDto SavePlayerStatistic(string sessionToken, PlayerStatisticDto statistic)
    {
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return Failure(denial);
        if (statistic is null || statistic.StatisticId < 0 || statistic.PlayerId <= 0 || statistic.MatchId <= 0)
            return Failure("יש לבחור שחקן ומשחק קיימים.");
        if (statistic.Goals is < 0 or > short.MaxValue ||
            statistic.Assists is < 0 or > short.MaxValue ||
            statistic.MinutesPlayed is < 0 or > short.MaxValue ||
            !float.IsFinite(statistic.Rating) || statistic.Rating is < 0 or > 100)
            return Failure("יש להזין מספרים בין 0 ל־32767 ודירוג בין 0 ל־100.");

        try
        {
            return _statistics.Save(statistic)
                ? Success(statistic.StatisticId == 0 ? "הסטטיסטיקה נוספה ונשמרה." : "הסטטיסטיקה עודכנה ונשמרה.")
                : Failure("הסטטיסטיקה לא נמצאה.");
        }
        catch (OleDbException)
        {
            return Failure("הסטטיסטיקה לא נשמרה. יש לוודא שהשחקן והמשחק קיימים.");
        }
    }

    public OperationResultDto DeletePlayerStatistic(string sessionToken, int statisticId)
    {
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return Failure(denial);
        if (statisticId <= 0) return Failure("יש לבחור סטטיסטיקה קיימת.");

        try
        {
            return _statistics.Delete(statisticId)
                ? Success("הסטטיסטיקה נמחקה.")
                : Failure("הסטטיסטיקה לא נמצאה.");
        }
        catch (OleDbException)
        {
            return Failure("לא ניתן למחוק את הסטטיסטיקה ממסד Access.");
        }
    }

    private string? CycleFourAccessError(string token)
    {
        HttpStatusCode status;
        string explanation;
        if (!TryGetAuthenticatedUser(token, out var user))
        {
            status = HttpStatusCode.Unauthorized;
            explanation = "נדרשת התחברות פעילה.";
        }
        else if (user.Role == "Manager")
        {
            return null;
        }
        else
        {
            status = HttpStatusCode.Forbidden;
            explanation = "רק מנהל קבוצות רשאי לנהל העברות וסטטיסטיקות.";
        }

        OperationContext.Current.OutgoingMessageProperties[HttpResponseMessageProperty.Name] =
            new HttpResponseMessageProperty { StatusCode = status };
        return $"{(int)status} {status} · {explanation}";
    }
}
