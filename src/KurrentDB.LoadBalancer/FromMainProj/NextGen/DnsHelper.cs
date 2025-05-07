using System.Net;
using System.Net.Sockets;

namespace KurrentDb.Client;

public static class DnsHelper {
	/// <summary>
	/// Resolves hostnames to a list of IPEndPoints.
	/// </summary>
	/// <param name="uri">The Uri object representing the connection details.</param>
	/// <param name="defaultPortOverride">Optional override for the default port (defaults to 2113).</param>
	/// <returns>A list of resolved IPEndPoints.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown if the Uri.Authority is empty or whitespace.</exception>
	/// <exception cref="FormatException">
	/// Thrown if the host segment format within the Uri.Authority is invalid
	/// (e.g. invalid port number, empty segment between commas).
	/// </exception>
	/// <exception cref="SocketException">
	/// Thrown if a hostname parsed from the Uri.Authority cannot be resolved via DNS.
	/// </exception>
	/// <remarks>
	/// Note: The standard System.Uri class may not correctly parse authorities
	/// containing comma-separated hosts. This method works by extracting the
	/// Uri.Authority string and manually parsing it. The caller is responsible
	/// for ensuring the input Uri object could be successfully created, although
	/// basic checks are performed. The Uri.Port property is NOT reliably used
	/// here due to the potential multi-host format; port resolution happens per segment.
	/// </remarks>
	public static List<IPEndPoint> ResolveEndpoints(Uri uri, int defaultPortOverride) {
		// Extract the full authority part (e.g., "admin:changeit@host1,host2:port" or "host1,host2")
		// We use uri.Authority not because we want the user info, but because it's the only property
		// that reliably gives us the complete, raw string containing the potentially comma-separated
		// list of hosts, which we then need to parse manually due to its non-standard nature.
		// Using uri.Host would lead to data loss (missing hosts after the first comma).
		var authority = uri.Authority;

		if (string.IsNullOrWhiteSpace(authority))
			// An empty authority means no host information was present.
			throw new ArgumentException("The provided Uri has an empty or whitespace Authority component.", nameof(uri));

		var resolvedEndpoints = new List<IPEndPoint>();

		// Find the start of the host section (after user info, if present)
		var userInfoEndIndex = authority.LastIndexOf('@');
		var hostSection = userInfoEndIndex >= 0
			? authority.Substring(userInfoEndIndex + 1)
			: authority;

		// If after removing user info the host section is empty, it's an error.
		if (string.IsNullOrWhiteSpace(hostSection))
			throw new FormatException($"Invalid Authority format in Uri '{uri.OriginalString}': Host section is empty after removing user info.");

		// Split the host section by commas for multiple nodes
		var hostSegments = hostSection.Split([','], StringSplitOptions.RemoveEmptyEntries);

		if (hostSegments.Length == 0)
			// This case might happen if the authority was just "user@," or ","
			throw new FormatException($"Invalid Authority format in Uri '{uri.OriginalString}': No valid host segments found after splitting.");

		foreach (var segment in hostSegments) {
			string currentHost;
			var    currentPort    = defaultPortOverride; // Start with the default/override
			var    trimmedSegment = segment.Trim();

			if (string.IsNullOrEmpty(trimmedSegment))
				throw new FormatException($"Invalid Authority format in Uri '{uri.OriginalString}': Empty segment found within host section '{hostSection}'.");

			// Check for explicit port within the segment
			// Note: We IGNORE uri.Port because it's unreliable with comma-separated hosts.
			var lastColonIndex      = trimmedSegment.LastIndexOf(':');
			var closingBracketIndex = trimmedSegment.LastIndexOf(']'); // For IPv6

			if (lastColonIndex > 0 && lastColonIndex > closingBracketIndex) {
				var portString = trimmedSegment.Substring(lastColonIndex + 1);
				currentHost = trimmedSegment.Substring(0, lastColonIndex).Trim();

				if (!int.TryParse(portString, out var parsedPort) || parsedPort < IPEndPoint.MinPort || parsedPort > IPEndPoint.MaxPort)
					throw new FormatException(
						$"Invalid port number '{portString}' in segment '{trimmedSegment}' within Uri '{uri.OriginalString}'. Port must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}."
					);

				currentPort = parsedPort;
			}
			else {
				currentHost = trimmedSegment; // No explicit port, will use defaultPortOverride
			}

			// Clean up potential IPv6 brackets
			if (currentHost.StartsWith("[") && currentHost.EndsWith("]")) {
				if (currentHost.Length > 2)
					currentHost = currentHost.Substring(1, currentHost.Length - 2);
				else
					throw new FormatException($"Invalid IPv6 address format '[]' in segment '{trimmedSegment}' within Uri '{uri.OriginalString}'.");
			}

			if (string.IsNullOrWhiteSpace(currentHost))
				throw new FormatException($"Invalid Authority format: Host part became empty after processing segment '{trimmedSegment}' within Uri '{uri.OriginalString}'.");

			// --- Resolve the host ---
			try {
				var addresses = Dns.GetHostAddresses(currentHost);

				if (addresses == null || addresses.Length == 0)
					// Should be caught by SocketException below, but handle defensively.
					throw new SocketException((int)SocketError.HostNotFound);

				foreach (var address in addresses)
					resolvedEndpoints.Add(new IPEndPoint(address, currentPort));
			}
			catch (SocketException ex) {
				throw new SocketException(
					ex.ErrorCode,
					$"Failed to resolve host '{currentHost}' from segment '{trimmedSegment}' within Uri '{uri.OriginalString}'. DNS error: {ex.Message}"
				);
			}
			catch (ArgumentException ex) // From GetHostAddresses (e.g., invalid chars in hostname)
			{
				throw new FormatException($"Invalid host format '{currentHost}' in segment '{trimmedSegment}' within Uri '{uri.OriginalString}'. Error: {ex.Message}", ex);
			}
		}

		return resolvedEndpoints.Distinct().ToList();
	}
}
