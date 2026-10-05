using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;

namespace GHOST.Presentation;

public partial class InventoryView : UserControl
{
    private readonly IDay5Service service;
    private readonly ICurrentUserContext user;
    private readonly ObservableCollection<ProductSummary> products = [];
    private readonly ObservableCollection<CategorySummary> categories = [];
    private readonly DispatcherTimer filterTimer;
    private bool initialized;

    public InventoryView(IDay5Service service, ICurrentUserContext user)
    {
        this.service = service;
        this.user = user;

        filterTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(280)
        };
        filterTimer.Tick += FilterTimer_Tick;

        InitializeComponent();

        ProductsGrid.ItemsSource = products;
        CategoryBox.ItemsSource = categories;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (initialized)
            return;

        initialized = true;

        FilterCategoryBox.ItemsSource = categories;
        FilterCategoryBox.SelectedIndex = 0;
        FilterStatusBox.SelectedIndex = 0;

        await RefreshAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) =>
        await RefreshAsync();

    private void FilterChanged(object sender, RoutedEventArgs e)
    {
        if (!initialized)
            return;

        filterTimer.Stop();
        filterTimer.Start();
    }

    private async void FilterTimer_Tick(object? sender, EventArgs e)
    {
        filterTimer.Stop();
        await ApplyCatalogFilterAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var cs = await service.GetCategoriesAsync(true);

            categories.Clear();
            foreach (var category in cs)
                categories.Add(category);

            if (FilterCategoryBox.SelectedValue is Guid selectedCategory &&
                categories.All(x => x.Id != selectedCategory))
            {
                FilterCategoryBox.SelectedIndex = 0;
            }

            await ApplyCatalogFilterAsync();
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }

    private async Task ApplyCatalogFilterAsync()
    {
        try
        {
            var status = ProductCatalogStatus.Active;

            if (FilterStatusBox.SelectedItem is ComboBoxItem statusItem &&
                statusItem.Tag is string statusTag &&
                Enum.TryParse<ProductCatalogStatus>(statusTag, out var parsedStatus))
            {
                status = parsedStatus;
            }

            Guid? categoryId = FilterCategoryBox.SelectedValue is Guid selectedCategory
                ? selectedCategory
                : null;

            var result = await service.GetProductCatalogAsync(
                new ProductCatalogFilterRequest(
                    SearchText: SearchBox.Text,
                    CategoryId: categoryId,
                    Status: status,
                    Page: 1,
                    PageSize: 100));

            products.Clear();
            foreach (var product in result.Items)
                products.Add(product);

            TotalProductsText.Text = result.TotalCount.ToString("N0");
            ActiveProductsText.Text = result.ActiveCount.ToString("N0");
            LowStockText.Text = result.LowStockCount.ToString("N0");
            StockValueText.Text = $"{result.RetailStockValue:N0} ج.م";
            ProductsCountText.Text =
                $"عرض {result.Items.Count:N0} من {result.TotalCount:N0}";
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }

    private async void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        var window = new ProductEditorWindow(
            service,
            user,
            categories);

        window.Owner = Window.GetWindow(this);

        if (window.ShowDialog() == true)
            await RefreshAsync();
    }

    private async void ProductsGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductSummary product)
            return;

        ProductsGrid.SelectedItem = null;

        var window = new ProductEditorWindow(
            service,
            user,
            categories,
            product);

        window.Owner = Window.GetWindow(this);

        if (window.ShowDialog() == true)
            await RefreshAsync();
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        FilterCategoryBox.SelectedIndex = 0;
        FilterStatusBox.SelectedIndex = 0;
    }

    private static void Error(Exception ex) =>
        MessageBox.Show(
            ex.Message,
            "GHOST",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}