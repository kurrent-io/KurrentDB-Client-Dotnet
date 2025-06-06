﻿using System.Text.Json;
using KurrentDB.Client;

namespace KurrentDB.Client;

public static class ReadOnlyMemoryExtensions {
	public static Position ParsePosition(this ReadOnlyMemory<byte> json) {
		using var doc = JsonDocument.Parse(json);

		var checkPoint = doc.RootElement.GetString();
		if (checkPoint is null)
			throw new("Unable to parse Position, data is missing!");

		if (Position.TryParse(checkPoint, out var position) && position.HasValue)
			return position.Value;

		throw new("Unable to parse Position, invalid data!");
	}

	public static StreamPosition ParseStreamPosition(this ReadOnlyMemory<byte> json) {
		using var doc = JsonDocument.Parse(json);

		var checkPoint = doc.RootElement.GetString();
		if (checkPoint is null)
			throw new("Unable to parse Position, data is missing!");

		return StreamPosition.FromInt64(int.Parse(checkPoint));
	}
}
