#if !NOobject
using NBitcoin.Protocol.Behaviors;
using NBitcoin.Socks;
using System;
using System.Collections.Generic;
using System.Linq;


using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Connectors
{
	public class DefaultEndpointConnector : IEnpointConnector
	{
		/// <summary>
		/// Connect to only hidden service nodes over Tor.
		/// Prevents connecting to clearnet nodes over Tor.
		/// </summary>
		public bool AllowOnlyTorEndpoints { get; set; } = false;

		public DefaultEndpointConnector()
		{
		}

		public DefaultEndpointConnector(bool allowOnlyTorEndpoints)
		{
			AllowOnlyTorEndpoints = allowOnlyTorEndpoints;
		}

		public IEnpointConnector Clone()
		{
			return new DefaultEndpointConnector(AllowOnlyTorEndpoints);
		}

		
	}
}
#endif
