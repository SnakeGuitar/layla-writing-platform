namespace client_web.Helpers;

public static class FormatData
{
    public static string FormatDateToString(DateTime date)
    {
        var months = new[] { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic" };
        return $"{date.Day} {months[date.Month - 1]}. {date.Year}";
    }

    public static string EnumToMethodName(this Enum method)
    {
        return method.ToString();
    }
}