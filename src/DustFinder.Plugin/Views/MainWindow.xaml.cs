using System.Windows;
using DustFinder.Plugin.ViewModels;

namespace DustFinder.Plugin.Views;

public partial class MainWindow : Window
{
	public MainWindow(MainViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;
		Loaded += async (_, _) => await viewModel.RefreshAsync();
	}
}

