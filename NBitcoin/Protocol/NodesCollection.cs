﻿#if !NOobject
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NodeEventArgs : EventArgs
	{
		public NodeEventArgs(Node node, bool added)
		{
			_Added = added;
			_Node = node;
		}

		private readonly bool _Added;
		public bool Added
		{
			get
			{
				return _Added;
			}
		}
		private readonly Node _Node;
		public Node Node
		{
			get
			{
				return _Node;
			}
		}
	}

	public interface IReadOnlyNodesCollection : IEnumerable<Node>
	{
		event EventHandler<NodeEventArgs> Added;
		event EventHandler<NodeEventArgs> Removed;

		Node FindByEndpoint(object endpoint);
		Node FindByIp(object ip);
		Node FindLocal();
	}

	public class NodesCollection : IEnumerable<Node>, IReadOnlyNodesCollection
	{
		class Bridge : MessageListener<IncomingMessage>
		{
			MessageProducer<IncomingMessage> _Prod;
			public Bridge(MessageProducer<IncomingMessage> prod)
			{
				_Prod = prod;
			}
			#region MessageListener<IncomingMessage> Members

			public void PushMessage(IncomingMessage message)
			{
				_Prod.PushMessage(message);
			}

			#endregion
		}
		Bridge bridge;
		public NodesCollection()
		{
			bridge = new Bridge(_MessageProducer);
		}

		MessageProducer<IncomingMessage> _MessageProducer = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> MessageProducer
		{
			get
			{
				return _MessageProducer;
			}
		}

		ConcurrentDictionary<Node, Node> _Nodes = new ConcurrentDictionary<Node, Node>();

		public int Count
		{
			get
			{
				return _Nodes.Count;
			}
		}
		public bool Add(Node node)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			if (_Nodes.TryAdd(node, node))
			{
				node.MessageProducer.AddMessageListener(bridge);
				OnNodeAdded(node);
				return true;
			}
			return false;
		}

		public bool Remove(Node node)
		{
			Node old;
			if (_Nodes.TryRemove(node, out old))
			{
				node.MessageProducer.RemoveMessageListener(bridge);
				OnNodeRemoved(old);
				return true;
			}
			return false;
		}

		public event EventHandler<NodeEventArgs> Added;
		public event EventHandler<NodeEventArgs> Removed;

		private void OnNodeAdded(Node node)
		{
			var added = Added;
			if (added != null)
				added(this, new NodeEventArgs(node, true));
		}

		private void OnNodeRemoved(Node node)
		{
			var removed = Removed;
			if (removed != null)
				removed(this, new NodeEventArgs(node, false));
		}

		public Node FindLocal()
		{
			return null;
		}

		public Node FindByIp(object ip)
		{
			return null;
		}



		public Node FindByEndpoint(object endpoint)
		{
			return null;
		}

		private static bool Match(object ip, int? port, Node n)
		{
			return false;
		}


		#region IEnumerable<Node> Members

		public IEnumerator<Node> GetEnumerator()
		{
			return _Nodes.Select(n => n.Key).AsEnumerable().GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public void DisconnectAll(CancellationToken cancellation = default(CancellationToken))
		{
			foreach (var node in _Nodes)
				node.Key.DisconnectAsync();
		}

		public void Clear()
		{
			_Nodes.Clear();
		}
	}
}
#endif
