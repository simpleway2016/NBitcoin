#if !NOobject
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;

namespace NBitcoin.Protocol.Behaviors
{
	public class SocksSettingsBehavior : NodeBehavior
	{
		public SocksSettingsBehavior()
		{

		}
		public SocksSettingsBehavior(object socksEndpoint)
		{
			SocksEndpoint = socksEndpoint;
		}
		public SocksSettingsBehavior(object socksEndpoint, bool onlyForOnionHosts)
		{
			SocksEndpoint = socksEndpoint;
			OnlyForOnionHosts = onlyForOnionHosts;
		}
		public SocksSettingsBehavior(object socksEndpoint, bool onlyForOnionHosts, object networkCredential, bool streamIsolation)
		{
			SocksEndpoint = socksEndpoint;
			OnlyForOnionHosts = onlyForOnionHosts;
			StreamIsolation = streamIsolation;
			NetworkCredential = networkCredential;
		}
		/// <summary>
		/// If the socks endpoint to connect to
		/// </summary>
		public object SocksEndpoint { get; set; }
		/// <summary>
		/// If the socks proxy is only used for Tor traffic (default: true)
		/// </summary>
		public bool OnlyForOnionHosts { get; set; } = true;


		/// <summary>
		/// Credentials to connect to the SOCKS proxy (Use StreamIsolation instead of you want Tor isolation)
		/// </summary>
		public object NetworkCredential { get; set; }

		/// <summary>
		/// Randomize the NetworkCredentials to the Socks proxy
		/// </summary>
		public bool StreamIsolation { get; set; }

		internal object GetCredentials()
		{
			return NetworkCredential ??
				(StreamIsolation ? GenerateCredentials() : null);
		}

		private object GenerateCredentials()
		{
			return null;
		}

		public override object Clone()
		{
			return new SocksSettingsBehavior(SocksEndpoint, OnlyForOnionHosts, NetworkCredential, StreamIsolation);
		}

		protected override void AttachCore()
		{

		}

		protected override void DetachCore()
		{

		}
	}
}
#endif
