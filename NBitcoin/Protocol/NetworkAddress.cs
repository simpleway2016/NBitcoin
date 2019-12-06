#if !NOobject
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NetworkAddress : IBitcoinSerializable
	{
		public NetworkAddress()
		{

		}
		public NetworkAddress(object endpoint)
		{
			Endpoint = endpoint;
		}
		public NetworkAddress(object address, int port)
		{
			
		}

		internal uint ntime;
		ulong service = 1;
		byte[] ip = new byte[16];
		ushort port;

		public ulong Service
		{
			get
			{
				return service;
			}
			set
			{
				service = value;
			}
		}

		public TimeSpan Ago
		{
			get
			{
				return DateTimeOffset.UtcNow - Time;
			}
			set
			{
				Time = DateTimeOffset.UtcNow - value;
			}
		}

		public void Adjust()
		{
			var nNow = Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow);
			if (ntime <= 100000000 || ntime > nNow + 10 * 60)
				ntime = nNow - 5 * 24 * 60 * 60;
		}

		public object Endpoint
		{
			get
			{
				return null;
			}
			set
			{
				
			}
		}

		public DateTimeOffset Time
		{
			get
			{
				return Utils.UnixTimeToDateTime(ntime);
			}
			set
			{
				ntime = Utils.DateTimeToUnixTime(value);
			}
		}
		uint version = 100100;
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Type == SerializationType.Disk)
			{
				stream.ReadWrite(ref version);
			}
			if (
				stream.Type == SerializationType.Disk ||
				(stream.ProtocolCapabilities.SupportTimeAddress && stream.Type != SerializationType.Hash))
				stream.ReadWrite(ref ntime);
			stream.ReadWrite(ref service);
			stream.ReadWrite(ref ip);
			using (stream.BigEndianScope())
			{
				stream.ReadWrite(ref port);
			}
		}

		#endregion

		public void ZeroTime()
		{
			this.ntime = 0;
		}

		internal byte[] GetKey()
		{
			var vKey = new byte[18];
			Array.Copy(ip, vKey, 16);
			vKey[16] = (byte)(port / 0x100);
			vKey[17] = (byte)(port & 0x0FF);
			return vKey;
		}
	}
}
#endif
