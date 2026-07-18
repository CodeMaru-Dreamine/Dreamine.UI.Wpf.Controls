namespace Dreamine.UI.Wpf.Controls.ViewRegion
{
	/// <summary>
	/// \if KO
	/// <para>비싱글턴 ViewModel 인스턴스의 고유 키를 만들기 위한 형식별 증가 인덱스를 보관합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores per-type incrementing indexes used to create unique keys for nonsingleton view-model instances.</para>
	/// \endif
	/// </summary>
	public static class ViewModelKeyCache
	{
		/// <summary>
		/// \if KO
		/// <para>ViewModel 형식 이름별 마지막 사용 인덱스를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the last-used index mapped by view-model type name.</para>
		/// \endif
		/// </summary>
		/// <remarks>
		/// \if KO
		/// <para>반환된 사전은 변경 가능하며 동시 접근을 자체적으로 동기화하지 않습니다.</para>
		/// \endif
		/// \if EN
		/// <para>The returned dictionary is mutable and does not synchronize concurrent access.</para>
		/// \endif
		/// </remarks>
		public static Dictionary<string, long> IndexMap { get; } = new();

		/// <summary>
		/// \if KO
		/// <para>지정한 형식 이름의 인덱스를 검사된 산술로 1 증가시키고 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Increments the index for the specified type name by one using checked arithmetic and returns it.</para>
		/// \endif
		/// </summary>
		/// <param name="typeName">
		/// \if KO
		/// <para>인덱스를 관리할 ViewModel 형식 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The view-model type name whose index is managed.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>증가된 인덱스입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The incremented index.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="typeName"/>이 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="typeName"/> is null.</para>
		/// \endif
		/// </exception>
		/// <exception cref="OverflowException">
		/// \if KO
		/// <para>기존 인덱스가 <see cref="long.MaxValue"/>이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when the existing index is <see cref="long.MaxValue"/>.</para>
		/// \endif
		/// </exception>
		public static long GetOrIncrement(string typeName)
		{
			IndexMap.TryGetValue(typeName, out long current);
			checked { current++; }
			IndexMap[typeName] = current;
			return current;
		}

		/// <summary>
		/// \if KO
		/// <para>저장된 모든 형식별 인덱스를 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Removes all stored per-type indexes.</para>
		/// \endif
		/// </summary>
		public static void Reset() => IndexMap.Clear();
	}
}
