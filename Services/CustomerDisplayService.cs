namespace Healthcare_Industry.Services;

public static class CustomerDisplayService
{
    public static string GetInitial(string? fullName, string? email)
    {
        var source = !string.IsNullOrWhiteSpace(fullName) ? fullName : email;
        return string.IsNullOrWhiteSpace(source)
            ? "U"
            : source.Trim()[0].ToString().ToUpperInvariant();
    }
}
