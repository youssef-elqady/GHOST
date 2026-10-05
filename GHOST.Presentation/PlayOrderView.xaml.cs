using System.Windows;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;

namespace GHOST.Presentation;

public partial class PlayOrderView : Window
{
    private readonly PlayOrderViewModel viewModel;

    public PlayOrderView(
        IDay5Service day5Service,
        ICurrentUserContext currentUser,
        Guid sessionId)
    {
        InitializeComponent();

        viewModel = new PlayOrderViewModel(
            day5Service,
            currentUser);

        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            try
            {
                await viewModel.LoadAsync(sessionId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "GHOST",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
            }
        };
    }

    private void Close_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        viewModel.Dispose();
        base.OnClosed(e);
    }
}
