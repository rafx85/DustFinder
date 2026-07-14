using System;
using System.Reflection;

namespace DustFinder.Plugin;

internal static class PluginVersionInfo
{
	public static Version Current { get; } =
		Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0);

	public static string Display { get; } = $"v{Current.Major}.{Current.Minor}.{Math.Max(Current.Build, 0)}";
}
