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

        Set(resources, "AppBg", dark ? "#0A0D12" : "#F5F7FA");
        Set(resources, "SidebarBg", dark ? "#0C1016" : "#FFFFFF");
        Set(resources, "Surface", dark ? "#10151C" : "#FFFFFF");
        Set(resources, "Surface2", dark ? "#151B23" : "#F0F3F7");
        Set(resources, "Surface3", dark ? "#1B222C" : "#E7ECF2");
        Set(resources, "Line", dark ? "#26303B" : "#D7DEE7");
        Set(resources, "LineSoft", dark ? "#1B232D" : "#E5E9EF");
        Set(resources, "White", dark ? "#F7F9FB" : "#1D2630");
        Set(resources, "Text", dark ? "#E2E7ED" : "#2D3743");
        Set(resources, "Muted", dark ? "#8A96A5" : "#687687");
        Set(resources, "Dim", dark ? "#626D7B" : "#8A95A3");

        Set(resources, "Gold", dark ? "#5E7FA8" : "#456887");
        Set(resources, "GoldSoft", dark ? "#1A2635" : "#E8F0F7");
        Set(resources, "Green", dark ? "#55A884" : "#397A5F");
        Set(resources, "GreenSoft", dark ? "#152A22" : "#E4F1EB");
        Set(resources, "Amber", dark ? "#C49A58" : "#956C2B");
        Set(resources, "AmberSoft", dark ? "#2A241A" : "#F5EDDE");
        Set(resources, "Blue", dark ? "#6E9BD0" : "#4E739F");
        Set(resources, "BlueSoft", dark ? "#182638" : "#E7EEF7");
        Set(resources, "Red", dark ? "#C96F73" : "#A94E4E");
        Set(resources, "RedSoft", dark ? "#2B1D21" : "#F6E6E8");

        Set(resources, "GhostAccentHover", dark ? "#7292B9" : "#587B9D");
        Set(resources, "GhostControlHover", dark ? "#202934" : "#E7EBF0");
        Set(resources, "GhostControlPressed", dark ? "#1B232D" : "#DDE4EC");
        Set(resources, "GhostActiveNav", dark ? "#182333" : "#E8F0F7");
        Set(resources, "GhostSidebarUser", dark ? "#131A22" : "#F5F7FA");

        Set(resources, "GhostBg", dark ? "#0A0D12" : "#F5F7FA");
        Set(resources, "GhostSurface", dark ? "#10151C" : "#FFFFFF");
        Set(resources, "GhostRaised", dark ? "#1B222C" : "#E7ECF2");
        Set(resources, "GhostHover", dark ? "#151B23" : "#F0F3F7");
        Set(resources, "GhostBorder", dark ? "#26303B" : "#D7DEE7");
        Set(resources, "GhostAccent", dark ? "#5E7FA8" : "#456887");
        Set(resources, "GhostText", dark ? "#E2E7ED" : "#2D3743");
        Set(resources, "GhostMuted", dark ? "#8A96A5" : "#687687");
        Set(resources, "GhostSuccess", dark ? "#55A884" : "#397A5F");
        Set(resources, "GhostWarning", dark ? "#C49A58" : "#956C2B");
        Set(resources, "GhostDanger", dark ? "#C96F73" : "#A94E4E");
    }

    private static void Set(ResourceDictionary resources, string key, string hex)
    {
        if (resources[key] is SolidColorBrush brush)
            brush.Color = (Color)ColorConverter.ConvertFromString(hex)!;
    }
}