using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;
using GHOST.Domain.Enums;

namespace GHOST.Presentation;

public partial class StockMovementWindow : Window
{
    private readonly IDay5Service service;
    private readonly ICurrentUserContext user;
    private readonly List<ProductSummary> products;
    private readonly bool isManager;

    public StockMovementWindow(
        IDay5Service service,
        ICurrentUserContext user,
        IReadOnlyList<ProductSummary> products)
    {
        this.service = service;
        this.user = user;
        this.products = products.ToList();
        isManager = user.IsInRole("Manager") || user.IsInRole("Admin");

        InitializeComponent();

        ProductBox.ItemsSource = this.products;
        BuildMovementTypes();

        if (this.products.Count > 0)
            ProductBox.SelectedIndex = 0;

        PermissionNoteText.Text = isManager
            ? "حساب الإدارة يستطيع تسجيل كل أنواع حركات المخزون المسموح بها، بما فيها الهالك والتسوية."
            : "حساب Staff يستطيع تسجيل حركات الشراء والمرتجع والهدية. الهالك والتسوية يحتاجان صلاحية Manager.";
    }

    private void BuildMovementTypes()
    {
        TypeBox.Items.Clear();

        AddType(InventoryTransactionType.Purchase, "شراء / إضافة للمخزون");
        AddType(InventoryTransactionType.Return, "مرتجع / إضافة للمخزون");
        AddType(InventoryTransactionType.Gift, "هدية / صرف من المخزون");

        if (isManager)
        {
            AddType(InventoryTransactionType.Waste, "هالك / صرف من المخزون");
            AddType(InventoryTransactionType.Adjustment, "تسوية / تعديل المخزون");
        }

        TypeBox.SelectedIndex = 0;
    }

    private void AddType(InventoryTransactionType type, string text)
    {
        TypeBox.Items.Add(new ComboBoxItem
        {
            Content = text,
            Tag = type
        });
    }

    private void ProductBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdatePreview();

    private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdatePreview();

    private void QuantityBox_TextChanged(object sender, TextChangedEventArgs e) =>
        UpdatePreview();

    private void UpdatePreview()
    {
        if (ProductBox.SelectedItem is not ProductSummary product)
        {
            CurrentStockText.Text = "—";
            ImpactText.Text = "—";
            AfterStockText.Text = "—";
            return;
        }

        CurrentStockText.Text = product.StockQuantity.ToString("N0");

        if (!TryGetQuantity(out var quantity))
        {
            ImpactText.Text = "—";
            AfterStockText.Text = "—";
            return;
        }

        var type = SelectedType();
        var incoming = type is InventoryTransactionType.Purchase or InventoryTransactionType.Return;
        var delta = incoming ? quantity : -quantity;
        var after = product.StockQuantity + delta;

        ImpactText.Text = delta > 0
            ? $"+{delta:N0}"
            : delta.ToString("N0");

        AfterStockText.Text = after >= 0
            ? after.ToString("N0")
            : "غير صالح";

        AfterStockText.Foreground = after >= 0
            ? FindResource("GhostText")
            : FindResource("Red");
    }

    private bool TryGetQuantity(out int quantity) =>
        int.TryParse(QuantityBox.Text.Trim(), out quantity) && quantity > 0;

    private InventoryTransactionType SelectedType()
    {
        return TypeBox.SelectedItem is ComboBoxItem item &&
               item.Tag is InventoryTransactionType type
            ? type
            : InventoryTransactionType.Purchase;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        if (ProductBox.SelectedItem is not ProductSummary product)
        {
            ErrorText.Text = "اختر المنتج أولًا.";
            return;
        }

        if (TypeBox.SelectedItem is not ComboBoxItem typeItem ||
            typeItem.Tag is not InventoryTransactionType type)
        {
            ErrorText.Text = "اختر نوع الحركة.";
            return;
        }

        if (!TryGetQuantity(out var quantity))
        {
            ErrorText.Text = "أدخل كمية صحيحة أكبر من صفر.";
            QuantityBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(ReasonBox.Text))
        {
            ErrorText.Text = "سبب الحركة مطلوب.";
            ReasonBox.Focus();
            return;
        }

        var delta = type is InventoryTransactionType.Purchase or InventoryTransactionType.Return
            ? quantity
            : -quantity;

        if (product.StockQuantity + delta < 0)
        {
            ErrorText.Text = $"لا يمكن تنفيذ الحركة. المخزون الحالي {product.StockQuantity:N0} فقط.";
            return;
        }

        try
        {
            await service.ChangeStockAsync(
                user.RequireAuthenticated().Id,
                new InventoryRequest(
                    product.Id,
                    type,
                    quantity,
                    ReasonBox.Text.Trim()));

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}