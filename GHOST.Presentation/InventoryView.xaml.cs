using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
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
    private Guid? editingId;

    public InventoryView(IDay5Service service, ICurrentUserContext user)
    {
        this.service = service;
        this.user = user;
        InitializeComponent();
        ProductsGrid.ItemsSource = products;
        CategoryBox.ItemsSource = categories;
        CategoryManageBox.ItemsSource = categories;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e) => await RefreshAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            var ps = await service.GetProductsAsync(true);
            var cs = await service.GetCategoriesAsync(true);

            products.Clear();
            foreach (var p in ps) products.Add(p);

            categories.Clear();
            foreach (var c in cs) categories.Add(c);

            StockProductBox.ItemsSource = ps.Where(x => x.IsActive).ToList();
        }
        catch (Exception ex) { Error(ex); }
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
                await service.UpdateProductAsync(actor, new UpdateProductRequest(id, name, categoryId, selling, cost, minimum, barcode));
                MessageBox.Show("تم تعديل المنتج بنجاح.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                await service.CreateProductAsync(actor, new ProductRequest(name, categoryId, selling, cost, minimum, barcode));
                MessageBox.Show("تمت إضافة المنتج بنجاح.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ClearForm();
            await RefreshAsync();
        }
        catch (Exception ex) { Error(ex); }
    }

    private async void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        var name = CategoryNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("اكتب اسم التصنيف.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await service.CreateCategoryAsync(user.RequireAuthenticated().Id, name);
            CategoryNameBox.Clear();
            await RefreshAsync();
        }
        catch (Exception ex) { Error(ex); }
    }

    private async void DeactivateCategory_Click(object sender, RoutedEventArgs e)
    {
        if (CategoryManageBox.SelectedValue is not Guid id)
        {
            MessageBox.Show("اختر التصنيف.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await service.SetCategoryActiveAsync(user.RequireAuthenticated().Id, id, false);
            await RefreshAsync();
        }
        catch (Exception ex) { Error(ex); }
    }

    private async void ChangeStock_Click(object sender, RoutedEventArgs e)
    {
        if (StockProductBox.SelectedValue is not Guid productId ||
            !int.TryParse(StockQuantityBox.Text.Trim(), out var quantity) ||
            quantity <= 0 ||
            StockTypeBox.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string tag ||
            !Enum.TryParse<InventoryTransactionType>(tag, out var type))
        {
            MessageBox.Show("اختر المنتج ونوع الحركة وأدخل كمية صحيحة.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var reason = StockReasonBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            MessageBox.Show("سبب الحركة مطلوب.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await service.ChangeStockAsync(user.RequireAuthenticated().Id, new InventoryRequest(productId, type, quantity, reason));
            StockQuantityBox.Clear();
            StockReasonBox.Clear();
            await RefreshAsync();
            MessageBox.Show("تم تسجيل حركة المخزون.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { Error(ex); }
    }

    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductSummary p) return;

        editingId = p.Id;
        NameBox.Text = p.Name;
        BarcodeBox.Text = p.Barcode ?? "";
        CategoryBox.SelectedValue = p.CategoryId;
        SellingPriceBox.Text = p.SellingPrice.ToString("0.##");
        CostPriceBox.Text = p.CostPrice.ToString("0.##");
        MinimumStockBox.Text = p.MinimumStockLevel.ToString();
        SaveButton.Content = "حفظ التعديلات";
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
        barcode = string.IsNullOrWhiteSpace(BarcodeBox.Text) ? null : BarcodeBox.Text.Trim();
        categoryId = Guid.Empty;
        selling = cost = 0;
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
            MessageBox.Show("راجع اسم المنتج والتصنيف والأسعار والحد الأدنى.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        categoryId = selected;
        return true;
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
        SaveButton.Content = "حفظ المنتج";
    }

    private static void Error(Exception ex) =>
        MessageBox.Show(ex.Message, "GHOST", MessageBoxButton.OK, MessageBoxImage.Error);
}
