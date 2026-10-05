using System.Windows;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;

namespace GHOST.Presentation;

public partial class ProductEditorWindow : Window
{
    private readonly IDay5Service service;
    private readonly ICurrentUserContext user;
    private readonly Guid? editingId;

    public ProductEditorWindow(
        IDay5Service service,
        ICurrentUserContext user,
        IReadOnlyList<CategorySummary> categories,
        ProductSummary? product = null)
    {
        this.service = service;
        this.user = user;
        editingId = product?.Id;

        InitializeComponent();

        CategoryBox.ItemsSource = categories
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToList();

        if (product is null)
        {
            TitleText.Text = "إضافة منتج";
            SaveButton.Content = "حفظ المنتج";
        }
        else
        {
            TitleText.Text = "تعديل المنتج";
            SaveButton.Content = "حفظ التعديلات";

            NameBox.Text = product.Name;
            BarcodeBox.Text = product.Barcode ?? string.Empty;
            CategoryBox.SelectedValue = product.CategoryId;
            SellingPriceBox.Text = product.SellingPrice.ToString("0.##");
            CostPriceBox.Text = product.CostPrice.ToString("0.##");
            MinimumStockBox.Text = product.MinimumStockLevel.ToString();
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadForm(
                out var name,
                out var categoryId,
                out var sellingPrice,
                out var costPrice,
                out var minimumStock,
                out var barcode))
        {
            return;
        }

        try
        {
            var actor = user.RequireAuthenticated().Id;

            if (editingId is Guid id)
            {
                await service.UpdateProductAsync(
                    actor,
                    new UpdateProductRequest(
                        id,
                        name,
                        categoryId,
                        sellingPrice,
                        costPrice,
                        minimumStock,
                        barcode));
            }
            else
            {
                await service.CreateProductAsync(
                    actor,
                    new ProductRequest(
                        name,
                        categoryId,
                        sellingPrice,
                        costPrice,
                        minimumStock,
                        barcode));
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "GHOST",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private bool TryReadForm(
        out string name,
        out Guid categoryId,
        out decimal sellingPrice,
        out decimal costPrice,
        out int minimumStock,
        out string? barcode)
    {
        name = NameBox.Text.Trim();
        categoryId = Guid.Empty;
        sellingPrice = 0;
        costPrice = 0;
        minimumStock = 0;
        barcode = string.IsNullOrWhiteSpace(BarcodeBox.Text)
            ? null
            : BarcodeBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            CategoryBox.SelectedValue is not Guid selectedCategory ||
            !decimal.TryParse(SellingPriceBox.Text.Trim(), out sellingPrice) ||
            sellingPrice < 0 ||
            !decimal.TryParse(CostPriceBox.Text.Trim(), out costPrice) ||
            costPrice < 0 ||
            !int.TryParse(MinimumStockBox.Text.Trim(), out minimumStock) ||
            minimumStock < 0)
        {
            MessageBox.Show(
                "راجع اسم المنتج والتصنيف والأسعار والحد الأدنى للمخزون.",
                "GHOST",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
        }

        categoryId = selectedCategory;
        return true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}