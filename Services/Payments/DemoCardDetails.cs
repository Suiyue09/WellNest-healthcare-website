namespace Healthcare_Industry.Services;

public sealed class DemoCardDetails
{
    public string CardholderName { get; init; } = "";
    public string LastFour { get; init; } = "";
    public string Expiry { get; init; } = "";
    public string MaskedNumber => string.IsNullOrWhiteSpace(LastFour) ? "" : $"**** **** **** {LastFour}";

    public static DemoCardDetails FromInput(string? cardholderName, string? cardNumber, string? expiry)
    {
        var digits = new string((cardNumber ?? "").Where(char.IsDigit).ToArray());
        var lastFour = digits.Length >= 4 ? digits[^4..] : digits;

        return new DemoCardDetails
        {
            CardholderName = cardholderName?.Trim() ?? "",
            LastFour = lastFour,
            Expiry = expiry?.Trim() ?? ""
        };
    }
}
