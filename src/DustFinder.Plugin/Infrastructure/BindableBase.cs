using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DustFinder.Plugin.Infrastructure;

public abstract class BindableBase : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if(EqualityComparer<T>.Default.Equals(field, value))
			return false;
		field = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		return true;
	}

	protected void Raise([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

