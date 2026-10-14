namespace Microsoft.Maui.Devices.Sensors;

// Only the console checks use this model; the application uses the device API.
public sealed class Location(double latitude, double longitude)
{
    public double Latitude { get; } = latitude;
    public double Longitude { get; } = longitude;
}
