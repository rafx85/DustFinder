using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DustFinder.Plugin.Integration;
using DustFinder.Plugin.ViewModels;
using DustFinder.Plugin.Views;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Plugins;

namespace DustFinder.Plugin;

public sealed class DustFinderPlugin : IPlugin
{
	private readonly MenuItem _menuItem;
	private MainWindow? _window;
	private MainViewModel? _viewModel;
	private bool _enabled;

	public DustFinderPlugin()
	{
		_menuItem = new MenuItem { Header = "DustFinder" };
		_menuItem.Click += (_, _) => ShowWindow();
	}

	public string Name => "DustFinder";
	public string Description => "Recommendation-only Hearthstone collection and dust planner.";
	public string ButtonText => "Open DustFinder";
	public string Author => "Rafx";
	public Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0);
	public MenuItem MenuItem => _menuItem;

	public void OnLoad()
	{
		_enabled = true;
		CollectionHelpers.Hearthstone.OnCollectionChanged += OnCollectionChanged;
	}

	public void OnUnload()
	{
		_enabled = false;
		CollectionHelpers.Hearthstone.OnCollectionChanged -= OnCollectionChanged;
		Application.Current?.Dispatcher.Invoke(() => _window?.Close());
		_window = null;
		_viewModel = null;
	}

	public void OnButtonPress() => ShowWindow();
	public void OnUpdate() { }

	private void ShowWindow()
	{
		if(!_enabled)
			return;
		Application.Current.Dispatcher.Invoke(() =>
		{
			if(_window != null)
			{
				if(_window.WindowState == WindowState.Minimized)
					_window.WindowState = WindowState.Normal;
				_window.Activate();
				return;
			}

			var dataDirectory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"HearthstoneDeckTracker",
				"DustFinder");
			_viewModel = new MainViewModel(new HdtCollectionSource(), dataDirectory);
			_window = new MainWindow(_viewModel);
			_window.Closed += (_, _) =>
			{
				_window = null;
				_viewModel = null;
			};
			_window.Show();
		});
	}

	private void OnCollectionChanged()
	{
		if(!_enabled || _viewModel == null || Application.Current == null)
			return;
		Application.Current.Dispatcher.BeginInvoke(new Action(async () => await _viewModel.RefreshAsync()));
	}
}
