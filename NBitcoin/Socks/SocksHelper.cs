#if !NOobject
using System;
using System.Collections.Generic;
using System.Linq;


using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Socks
{
	public class SocksHelper
	{
		// https://gitweb.torproject.org/torspec.git/tree/socks-extensions.txt
		// The "NO AUTHENTICATION REQUIRED" (SOCKS5) authentication method[00] is
		// supported; and as of Tor 0.2.3.2-alpha, the "USERNAME/PASSWORD" (SOCKS5)
		// authentication method[02] is supported too, and used as a method to
		// implement stream isolation.As an extension to support some broken clients,
		// we allow clients to pass "USERNAME/PASSWORD" authentication to us even if
		// no authentication was selected.
		static readonly byte[] SelectionMessageNoAuthenticationRequired = new byte[] { 5, 1, 0 };
		static readonly byte[] SelectionMessageUsernamePassword = new byte[] { 5, 1, 2 };

		internal static byte[] CreateConnectMessage(object endpoint)
		{
			return null;
		}
		static Encoding NoBOMUTF8 = new UTF8Encoding(false);
		public static Task Handshake(object s, object endpoint, CancellationToken cancellationToken)
		{
			return null;
		}

	
	}
}
#endif
