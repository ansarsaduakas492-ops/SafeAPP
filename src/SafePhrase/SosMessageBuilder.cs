using System.Globalization;
using Microsoft.Maui.Devices.Sensors;

namespace SafePhrase;

public static class SosMessageBuilder
{
    public static string Create(SafetySettings settings, Location? location, DateTimeOffset sentAt)
    {
        string locationText = location is null
            ? "Location unavailable. SafePhrase could not access the device location."
            : $"https://maps.google.com/?q={location.Latitude.ToString(CultureInfo.InvariantCulture)},{location.Longitude.ToString(CultureInfo.InvariantCulture)}";
        string timeText = sentAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

        return $"{settings.SosMessage.Trim()}\n\nMy current location:\n{locationText}\n\nTime:\n{timeText}";
    }

    public static string CreateWhatsAppLink(string internationalPhone, string message)
    {
        string digits = new(internationalPhone.Where(char.IsDigit).ToArray());
        return $"https://wa.me/{digits}?text={Uri.EscapeDataString(message)}";
    }
}
