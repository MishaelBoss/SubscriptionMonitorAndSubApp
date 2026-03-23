using Microsoft.Maui.Devices;

namespace SubApp.Scripts;

public static class AppConfig
{
    public static string BaseUrl => DeviceInfo.Platform == DevicePlatform.Android 
        ? "http://10.0.2.2:8000" 
        : "http://127.0.0.1:8000";
}