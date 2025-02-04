﻿#if !NOobject
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Generic;

using System.Linq;

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.Protocol.Connectors;

namespace NBitcoin.Protocol
{
	public class NodeConnectionParameters
	{

		public NodeConnectionParameters()
		{
			TemplateBehaviors.Add(new PingPongBehavior());
			Version = null;
			IsRelay = true;
			Services = NodeServices.Nothing;
			ConnectCancellation = default(CancellationToken);
			// Use max supported by MAC OSX Yosemite/Mavericks/Sierra (https://fasterdata.es.net/host-tuning/osx/)
			this.objectSettings.ReceiveBufferSize = 1048576;
			this.objectSettings.SendBufferSize = 1048576;
			////////////////////////
			UserAgent = VersionPayload.GetNBitcoinUserAgent();
			PreferredTransactionOptions = TransactionOptions.All;
		}

		public NodeConnectionParameters(NodeConnectionParameters other)
		{
			Version = other.Version;
			IsRelay = other.IsRelay;
			Services = other.Services;
			ConnectCancellation = other.ConnectCancellation;
			UserAgent = other.UserAgent;
			AddressFrom = other.AddressFrom;
			Nonce = other.Nonce;
			Advertize = other.Advertize;
			PreferredTransactionOptions = other.PreferredTransactionOptions;
			EndpointConnector = other.EndpointConnector.Clone();
			objectSettings = other.objectSettings.Clone();
			foreach (var behavior in other.TemplateBehaviors)
			{
				TemplateBehaviors.Add(behavior.Clone());
			}
		}

		/// <summary>
		/// Send addr unsolicited message of the AddressFrom peer when passing to Handshaked state
		/// </summary>
		public bool Advertize
		{
			get;
			set;
		}

		public uint? Version
		{
			get;
			set;
		}

		/// <summary>
		/// If true, the node will receive all incoming transactions if no bloomfilter are set
		/// </summary>
		public bool IsRelay
		{
			get;
			set;
		}

		public NodeServices Services
		{
			get;
			set;
		}

		public TransactionOptions PreferredTransactionOptions
		{
			get;
			set;
		}

		public string UserAgent
		{
			get;
			set;
		}
		[Obsolete("Use objectSettings.ReceiveBufferSize instead")]
		public int ReceiveBufferSize
		{
			get
			{
				return objectSettings.ReceiveBufferSize is int v ? v : 1048576;
			}
			set
			{
				objectSettings.ReceiveBufferSize = value;
			}
		}

		[Obsolete("Use objectSettings.SendBufferSize instead")]
		public int SendBufferSize
		{
			get
			{
				return objectSettings.SendBufferSize is int v ? v : 1048576;
			}
			set
			{
				objectSettings.SendBufferSize = value;
			}
		}

		public objectSettings objectSettings { get; set; } = new objectSettings();

		public IEnpointConnector EndpointConnector { get; set; } = new DefaultEndpointConnector();

		/// <summary>
		/// Whether we reuse a 1MB buffer for deserializing messages, for limiting GC activity (Default : true)
		/// </summary>
		[Obsolete("Ignored, all arrays are allocated through ArrayPool")]
		public bool ReuseBuffer
		{
			get;
			set;
		}
		public CancellationToken ConnectCancellation
		{
			get;
			set;
		}

		private readonly NodeBehaviorsCollection _TemplateBehaviors = new NodeBehaviorsCollection(null);
		public NodeBehaviorsCollection TemplateBehaviors
		{
			get
			{
				return _TemplateBehaviors;
			}
		}

		public NodeConnectionParameters Clone()
		{
			return new NodeConnectionParameters(this);
		}

		public object AddressFrom
		{
			get;
			set;
		}

		public ulong? Nonce
		{
			get;
			set;
		}

		public VersionPayload CreateVersion(object peer, Network network)
		{
			return null;
		}
	}
}
#endif
