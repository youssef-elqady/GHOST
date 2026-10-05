using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;

namespace GHOST.Presentation;

public partial class CategoryManagementWindow : Window
{
    private readonly IDay5Service service;
    private readonly ICurrentUserContext user;
    private readonly ObservableCollection<CategorySummary> categories = [];

    public CategoryManagementWindow(
        IDay5Service service,
        ICurrentUserContext user)
    {
        this.service = service;
        this.user = user;

        InitializeComponent();
        CategoriesGrid.ItemsSource = categories;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var rows = await service.GetCategoriesAsync(true);

            categories.Clear();
            foreach (var row in rows)
                categories.Add(row);

            UpdateActions();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        await AddCategoryAsync();
    }

    private async void NameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        await AddCategoryAsync();
    }

    private async Task AddCategoryAsync()
    {
        var name = NameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("اكتب اسم التصنيف أولًا.");
            NameBox.Focus();
            return;
        }

        try
        {
            await service.CreateCategoryAsync(
                user.RequireAuthenticated().Id,
                name);

            NameBox.Clear();
            await RefreshAsync();
            NameBox.Focus();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void CategoriesGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        UpdateActions();
    }

    private async void Enable_Click(object sender, RoutedEventArgs e)
    {
        if (CategoriesGrid.SelectedItem is not CategorySummary category)
            return;

        await SetActiveAsync(category, true);
    }

    private async void Disable_Click(object sender, RoutedEventArgs e)
    {
        if (CategoriesGrid.SelectedItem is not CategorySummary category)
            return;

        await SetActiveAsync(category, false);
    }

    private async Task SetActiveAsync(
        CategorySummary category,
        bool active)
    {
        try
        {
            await service.SetCategoryActiveAsync(
                user.RequireAuthenticated().Id,
                category.Id,
                active);

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void UpdateActions()
    {
        if (CategoriesGrid.SelectedItem is not CategorySummary category)
        {
            EnableButton.IsEnabled = false;
            DisableButton.IsEnabled = false;
            return;
        }

        EnableButton.IsEnabled = !category.IsActive;
        DisableButton.IsEnabled = category.IsActive;
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(
            message,
            "GHOST",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}