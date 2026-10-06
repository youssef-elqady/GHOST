using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GHOST.Application.Day5;
using GHOST.Domain.Enums;

namespace GHOST.Presentation;

public partial class StockHistoryWindow : Window
{
    private readonly IDay5Service service;
    private readonly ObservableCollection<HistoryRow> rows = [];
    private readonly IReadOnlyList<ProductSummary> products;

    public StockHistoryWindow(
        IDay5Service service,
        IReadOnlyList<ProductSummary> products,
        Guid? productId = null)
    {
        this.service = service;
        this.products = products;

        InitializeComponent();

        HistoryGrid.ItemsSource = rows;

        ProductFilterBox.ItemsSource = products;
        ProductFilterBox.SelectedValue = productId;

        BuildTypeFilter();
    }

    private void BuildTypeFilter()
    {
        TypeFilterBox.Items.Clear();
        TypeFilterBox.Items.Add(new ComboBoxItem
        {
            Content = "كل الحركات",
            Tag = null
        });

        foreach (var type in Enum.GetValues<InventoryTransactionType>())
        {
            TypeFilterBox.Items.Add(new ComboBoxItem
            {
                Content = TypeName(type),
                Tag = type
            });
        }

        TypeFilterBox.SelectedIndex = 0;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) =>
        await RefreshAsync();

    private async void Refresh_Click(object sender, RoutedEventArgs e) =>
        await RefreshAsync();

    private async void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            Guid? productId = ProductFilterBox.SelectedValue is Guid id
                ? id
                : null;

            InventoryTransactionType? type = null;

            if (TypeFilterBox.SelectedItem is ComboBoxItem item &&
                item.Tag is InventoryTransactionType selectedType)
            {
                type = selectedType;
            }

            var result = await service.GetInventoryHistoryAsync(
                new InventoryHistoryFilter(
                    ProductId: productId,
                    Type: type,
                    Page: 1,
                    PageSize: 100));

            rows.Clear();

            foreach (var typeItem in result)
            {
                rows.Add(new HistoryRow(
                    typeItem.CreatedAt,
                    typeItem.ProductName,
                    TypeName(typeItem.Type),
                    typeItem.Quantity > 0
                        ? $"+{typeItem.Quantity:N0}"
                        : typeItem.Quantity.ToString("N0"),
                    typeItem.BeforeQuantity,
                    typeItem.AfterQuantity,
                    typeItem.CreatedByName,
                    typeItem.Reason));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "GHOST",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        ProductFilterBox.SelectedIndex = -1;
        TypeFilterBox.SelectedIndex = 0;
    }

    private static string TypeName(InventoryTransactionType type) =>
        type switch
        {
            InventoryTransactionType.Purchase => "شراء",
            InventoryTransactionType.Sale => "بيع",
            InventoryTransactionType.Waste => "هالك",
            InventoryTransactionType.Adjustment => "تسوية",
            InventoryTransactionType.Return => "مرتجع",
            InventoryTransactionType.Gift => "هدية",
            _ => type.ToString()
        };

    private sealed record HistoryRow(
        DateTimeOffset CreatedAt,
        string ProductName,
        string TypeName,
        string QuantityDisplay,
        int BeforeQuantity,
        int AfterQuantity,
        string CreatedByName,
        string Reason);
}