using System.Windows;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace GHOST.Presentation;

public static class ThemeService
{
    public static bool IsDark { get; private set; } = true;

    public static void Toggle() => Apply(!IsDark);

    public static void Apply(bool dark)
    {
        IsDark = dark;
        var resources = WpfApplication.Current.Resources;

        Set(resources, "AppBg", dark ? "#0B0D10" : "#F4F6F8");
        Set(resources, "SidebarBg", dark ? "#0E1014" : "#FFFFFF");
        Set(resources, "Surface", dark ? "#13161B" : "#FFFFFF");
        Set(resources, "Surface2", dark ? "#171B21" : "#F0F2F5");
        Set(resources, "Surface3", dark ? "#1C2128" : "#E8EBEF");
        Set(resources, "Line", dark ? "#252B33" : "#D8DDE4");
        Set(resources, "LineSoft", dark ? "#1D2229" : "#E7EAF0");
        Set(resources, "White", dark ? "#F4F6F8" : "#171A1F");
        Set(resources, "Text", dark ? "#DDE2E8" : "#2B3038");
        Set(resources, "Muted", dark ? "#7D8795" : "#687180");
        Set(resources, "Dim", dark ? "#596270" : "#89919D");
        Set(resources, "Gold", dark ? "#B9A36D" : "#8D7135");
        Set(resources, "GoldSoft", dark ? "#29251D" : "#F2ECDE");
        Set(resources, "Green", dark ? "#65B88F" : "#39805F");
        Set(resources, "GreenSoft", dark ? "#182A23" : "#E4F1EB");
        Set(resources, "Amber", dark ? "#D2A45C" : "#9A6B20");
        Set(resources, "AmberSoft", dark ? "#2C261C" : "#F6EDDB");
        Set(resources, "Blue", dark ? "#789BC8" : "#4D6F9E");
        Set(resources, "BlueSoft", dark ? "#1D2838" : "#E7EDF6");
        Set(resources, "Red", dark ? "#D47A7A" : "#A94E4E");
        Set(resources, "RedSoft", dark ? "#2D2022" : "#F5E5E5");
        Set(resources, "GhostAccentHover", dark ? "#C7B77F" : "#A48645");
        Set(resources, "GhostControlHover", dark ? "#22272F" : "#E5E8ED");
        Set(resources, "GhostControlPressed", dark ? "#20252D" : "#DCE1E8");
        Set(resources, "GhostActiveNav", dark ? "#242019" : "#F1EBDD");
        Set(resources, "GhostSidebarUser", dark ? "#15191F" : "#F5F6F8");
        Set(resources, "GhostBg", dark ? "#0B0D10" : "#F4F6F8");
        Set(resources, "GhostSurface", dark ? "#13161B" : "#FFFFFF");
        Set(resources, "GhostRaised", dark ? "#1C2128" : "#E8EBEF");
        Set(resources, "GhostHover", dark ? "#171B21" : "#F0F2F5");
        Set(resources, "GhostBorder", dark ? "#252B33" : "#D8DDE4");
        Set(resources, "GhostAccent", dark ? "#B9A36D" : "#8D7135");
        Set(resources, "GhostText", dark ? "#DDE2E8" : "#2B3038");
        Set(resources, "GhostMuted", dark ? "#7D8795" : "#687180");
        Set(resources, "GhostSuccess", dark ? "#65B88F" : "#39805F");
        Set(resources, "GhostWarning", dark ? "#D2A45C" : "#9A6B20");
        Set(resources, "GhostDanger", dark ? "#D47A7A" : "#A94E4E");
    }

    private static void Set(ResourceDictionary resources, string key, string hex)
    {
        if (resources[key] is SolidColorBrush brush)
            brush.Color = (Color)ColorConverter.ConvertFromString(hex)!;
    }
}
