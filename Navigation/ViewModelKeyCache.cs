namespace Dreamine.UI.Wpf.Controls.ViewRegion
{
	/// <summary>
	/// Provides an index cache used to generate unique keys for non-singleton ViewModel instances.
	/// - Maintains an incrementing index per ViewModel type name.
	/// - Used only when <c>useSingletonView</c> is set to <c>false</c>.
	/// </summary>
	public static class ViewModelKeyCache
	{
		/// <summary>
		/// A dictionary mapping ViewModel type names to their current Instance index.
		/// This is used to avoid duplicate registrations or Instance collisions in non-singleton scenarios.
		///
		/// <para>Key: Fully qualified ViewModel type name (e.g., "MyApp.ViewModels.MyPageViewModel")</para>
		/// <para>Value: Last used index for that type (incremented on each new Instance)</para>
		///
		/// Example:
		/// <code>
		/// var typeName = "MyApp.ViewModels.MyPageViewModel";
		/// var index = ViewModelKeyCache.IndexMap.GetOrIncrement(typeName);
		/// var uniqueKey = $"{typeName}_{index:D2}";
		/// </code>
		/// </summary>
		public static Dictionary<string, long> IndexMap { get; } = new();

		/// <summary>
		/// Increments and returns the index for the given type name, using <c>long</c> to prevent overflow.
		/// </summary>
		public static long GetOrIncrement(string typeName)
		{
			IndexMap.TryGetValue(typeName, out long current);
			checked { current++; }
			IndexMap[typeName] = current;
			return current;
		}

		/// <summary>
		/// Resets the index map. Useful for testing or app re-initialization.
		/// </summary>
		public static void Reset() => IndexMap.Clear();
	}
}
