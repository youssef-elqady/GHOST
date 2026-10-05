using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;
using GHOST.Domain.Enums;

namespace GHOST.Presentation;

public partial class InventoryView : UserControl
{
    private readonly IDay5Service service;
    private readonly ICurrentUserContext user;
    private readonly ObservableCollection<ProductSummary> products = [];
    private readonly ObservableCollection<CategorySummary> categories = [];
    private readonly DispatcherTimer filterTimer;
    private Guid? editingId;
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

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

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
            foreach (var c in cs)
                categories.Add(c);

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
            foreach (var p in result.Items)
                products.Add(p);

            TotalProductsText.Text = result.TotalCount.ToString("N0");
            ActiveProductsText.Text = result.ActiveCount.ToString("N0");
            LowStockText.Text = result.LowStockCount.ToString("N0");
            StockValueText.Text = $"{result.RetailStockValue:N0} ج.م";
            CatalogCountText.Text = $"عرض {result.Items.Count:N0} من {result.TotalCount:N0}";

            if (editingId is Guid currentId &&
                result.Items.All(x => x.Id != currentId))
            {
                ClearForm();
            }
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }

    private void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        EditorPanel.Visibility = Visibility.Visible;
        NameBox.Focus();
    }

    private async void SaveProduct_Click(object sender, RoutedEventArgs e)
    {
        if (!ReadForm(out var name, out var categoryId, out var selling, out var cost, out var minimum, out var barcode))
            return;

        try
        {
            var actor = user.RequireAuthenticated().Id;

            if (editingId is Guid id)
            {
                await service.UpdateProductAsync(
                    actor,
                    new UpdateProductRequest(id, name, categoryId, selling, cost, minimum, barcode));
            }
            else
            {
                await service.CreateProductAsync(
                    actor,
                    new ProductRequest(name, categoryId, selling, cost, minimum, barcode));
            }

            ClearForm();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }

    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductSummary p)
            return;

        editingId = p.Id;
        NameBox.Text = p.Name;
        BarcodeBox.Text = p.Barcode ?? string.Empty;
        CategoryBox.SelectedValue = p.CategoryId;
        SellingPriceBox.Text = p.SellingPrice.ToString("0.##");
        CostPriceBox.Text = p.CostPrice.ToString("0.##");
        MinimumStockBox.Text = p.MinimumStockLevel.ToString();
        SaveButton.Content = "حفظ التعديلات";
        EditorPanel.Visibility = Visibility.Visible;
    }

    private bool ReadForm(
        out string name,
        out Guid categoryId,
        out decimal selling,
        out decimal cost,
        out int minimum,
        out string? barcode)
    {
        name = NameBox.Text.Trim();
        barcode = string.IsNullOrWhiteSpace(BarcodeBox.Text)
            ? null
            : BarcodeBox.Text.Trim();
        categoryId = Guid.Empty;
        selling = 0;
        cost = 0;
        minimum = 0;

        if (string.IsNullOrWhiteSpace(name) ||
            CategoryBox.SelectedValue is not Guid selected ||
            !decimal.TryParse(SellingPriceBox.Text.Trim(), out selling) ||
            selling < 0 ||
            !decimal.TryParse(CostPriceBox.Text.Trim(), out cost) ||
            cost < 0 ||
            !int.TryParse(MinimumStockBox.Text.Trim(), out minimum) ||
            minimum < 0)
        {
            MessageBox.Show(
                "راجع اسم المنتج والتصنيف والأسعار والحد الأدنى.",
                "GHOST",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        categoryId = selected;
        return true;
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        FilterCategoryBox.SelectedIndex = 0;
        FilterStatusBox.SelectedIndex = 0;
    }

    private void ClearForm_Click(object sender, RoutedEventArgs e) => ClearForm();

    private void ClearForm()
    {
        editingId = null;
        ProductsGrid.SelectedItem = null;
        NameBox.Clear();
        BarcodeBox.Clear();
        SellingPriceBox.Clear();
        CostPriceBox.Clear();
        MinimumStockBox.Clear();
        CategoryBox.SelectedIndex = -1;
        SaveButton.Content = "حفظ";
        EditorPanel.Visibility = Visibility.Collapsed;
    }

    private static void Error(Exception ex) =>
        MessageBox.Show(
            ex.Message,
            "GHOST",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}