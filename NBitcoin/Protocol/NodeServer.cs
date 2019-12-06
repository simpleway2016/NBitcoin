#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;


using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Logging;

namespace NBitcoin.Protocol
{
	public delegate void NodeServerNodeEventHandler(NodeServer sender, Node node);
	public delegate void NodeServerMessageEventHandler(NodeServer sender, IncomingMessage message);
	public class NodeServer : IDisposable
	{
		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}

		uint _Version;
		public uint Version
		{
			get
			{
				return _Version;
			}
		}

		/// <summary>
		/// The parameters that will be cloned and applied for each node connecting to the NodeServer
		/// </summary>
		public NodeConnectionParameters InboundNodeConnectionParameters
		{
			get;
			set;
		}

		public NodeServer(Network network, uint? version = null,
			int internalPort = -1)
		{
			
		}


		public event NodeServerNodeEventHandler NodeRemoved;
		public event NodeServerNodeEventHandler NodeAdded;
		public event NodeServerMessageEventHandler MessageReceived;

		void _Nodes_NodeRemoved(object sender, NodeEventArgs node)
		{
			var removed = NodeRemoved;
			if (removed != null)
				removed(this, node.Node);
		}

		void _Nodes_NodeAdded(object sender, NodeEventArgs node)
		{
			var added = NodeAdded;
			if (added != null)
				added(this, node.Node);
		}

		public bool AllowLocalPeers
		{
			get;
			set;
		}

		public int MaxConnections
		{
			get;
			set;
		}


		private object _LocalEndpoint;
		public object LocalEndpoint
		{
			get
			{
				return _LocalEndpoint;
			}
			set
			{
				_LocalEndpoint = Utils.EnsureIPv6(value);
			}
		}

		object socket;

		public bool IsListening
		{
			get
			{
				return socket != null;
			}
		}

		public void Listen(int maxIncoming = 8)
		{
			
		}

		private void BeginAccept()
		{
			
		}

	

		internal readonly MessageProducer<IncomingMessage> _MessageProducer = new MessageProducer<IncomingMessage>();
		internal readonly MessageProducer<object> _InternalMessageProducer = new MessageProducer<object>();

		MessageProducer<IncomingMessage> _AllMessages = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> AllMessages
		{
			get
			{
				return _AllMessages;
			}
		}

		volatile object _ExternalEndpoint;
		public object ExternalEndpoint
		{
			get
			{
				return _ExternalEndpoint;
			}
			set
			{
				_ExternalEndpoint = Utils.EnsureIPv6(value);
			}
		}


		internal void ExternalAddressDetected(object iPAddress)
		{
			
		}

		void ProcessMessage(IncomingMessage message)
		{
			AllMessages.PushMessage(message);

			using (Logs.NodeServer.BeginScope("Processing inbound message {message}", message.Message))
			{
				ProcessMessageCore(message);
			}
		}

		private void ProcessMessageCore(IncomingMessage message)
		{
			
		}

		void node_StateChanged(Node node, NodeState oldState)
		{
			if (node.State == NodeState.Disconnecting ||
				node.State == NodeState.Failed ||
				node.State == NodeState.Offline)
				ConnectedNodes.Remove(node);
		}

		private readonly NodesCollection _ConnectedNodes = new NodesCollection();
		public NodesCollection ConnectedNodes
		{
			get
			{
				return _ConnectedNodes;
			}
		}


		List<IDisposable> _Resources = new List<IDisposable>();
		IDisposable OwnResource(IDisposable resource)
		{
			if (_Cancel.IsCancellationRequested)
			{
				resource.Dispose();
				return Scope.Nothing;
			}
			return new Scope(() =>
			{
				lock (_Resources)
				{
					_Resources.Add(resource);
				}
			}, () =>
			{
				lock (_Resources)
				{
					_Resources.Remove(resource);
				}
			});
		}
		#region IDisposable Members

		CancellationTokenSource _Cancel = new CancellationTokenSource();
		public void Dispose()
		{
			if (!_Cancel.IsCancellationRequested)
			{
				_Cancel.Cancel();
				Logs.NodeServer.LogInformation("Stopping node server...");
				lock (_Resources)
				{
					foreach (var resource in _Resources)
						resource.Dispose();
				}
				try
				{
					_ConnectedNodes.DisconnectAll();
				}
				finally
				{
					if (socket != null)
					{
						Utils.SafeCloseSocket(socket);
						socket = null;
					}
				}
			}
		}

		#endregion

		internal NodeConnectionParameters CreateNodeConnectionParameters()
		{
			var myExternal = Utils.EnsureIPv6(ExternalEndpoint);
			var param2 = InboundNodeConnectionParameters.Clone();
			param2.Nonce = Nonce;
			param2.Version = Version;
			param2.AddressFrom = myExternal;
			return param2;
		}

		ulong _Nonce;
		public ulong Nonce
		{
			get
			{
				if (_Nonce == 0)
				{
					_Nonce = RandomUtils.GetUInt64();
				}
				return _Nonce;
			}
			set
			{
				_Nonce = value;
			}
		}



		public bool IsConnectedTo(object endpoint)
		{
			return false;
		}

		public Node FindOrConnect(object endpoint)
		{
			return null;
		}
	}
}
#endif
