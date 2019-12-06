#if !NOobject
using NBitcoin.Crypto;
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Logging;

namespace NBitcoin.Protocol
{

	/// <summary>
	/// The AddressManager, keep a set of peers discovered on the network in cache can update their actual states.
	/// Replicate AddressManager of Bitcoin Core, the Buckets and BucketPosition are not guaranteed to be coherent with Bitcoin Core
	/// </summary>
	public class AddressManager : IBitcoinSerializable
	{

		internal class AddressInfo : IBitcoinSerializable
		{
			#region IBitcoinSerializable Members

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref _Address);
				stream.ReadWrite(ref source);
				stream.ReadWrite(ref nLastSuccess);
				stream.ReadWrite(ref nAttempts);

			}

			internal int nAttempts;
			internal long nLastSuccess;
			byte[] source = new byte[16];

			public DateTimeOffset LastSuccess
			{
				get
				{
					return Utils.UnixTimeToDateTime((uint)nLastSuccess);
				}
				set
				{
					nLastSuccess = Utils.DateTimeToUnixTime(value);
				}
			}

			public object Source
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			NetworkAddress _Address;
			public int nRandomPos = -1;
			public int nRefCount;
			public bool fInTried;
			internal long nLastTry;
			internal DateTimeOffset nTime
			{
				get
				{
					return Address.Time;
				}
				set
				{
					Address.Time = value;
				}
			}


			public AddressInfo()
			{

			}
			public AddressInfo(NetworkAddress addr, object addrSource)
			{
				Address = addr;
				Source = addrSource;
			}

			public bool IsTerrible
			{
				get
				{
					return _IsTerrible(DateTimeOffset.UtcNow);
				}
			}

			internal DateTimeOffset LastTry
			{
				get
				{
					return Utils.UnixTimeToDateTime((uint)nLastSuccess);
				}
				set
				{
					nLastTry = Utils.DateTimeToUnixTime(value);
				}
			}

			public NetworkAddress Address
			{
				get
				{
					return _Address;
				}
				set
				{
					_Address = value;
				}
			}

			#endregion

			internal int GetNewBucket(uint256 nKey)
			{
				return GetNewBucket(nKey, Source);
			}

			internal int GetNewBucket(uint256 nKey, object src)
			{
				return 0;
			}

			private ulong Cheap(uint256 v)
			{
				return Utils.ToUInt64(v.ToBytes(), true);
			}

			internal int GetBucketPosition(uint256 nKey, bool fNew, int nBucket)
			{
				UInt64 hash1 = Cheap(
					Hashes.Hash256(
						nKey.ToBytes()
						.Concat(new byte[] { (fNew ? (byte)'N' : (byte)'K') })
						.Concat(Utils.ToBytes((uint)nBucket, false))
						.Concat(Address.GetKey())
					.ToArray()));
				return (int)(hash1 % ADDRMAN_BUCKET_SIZE);
			}

			internal int GetTriedBucket(uint256 nKey)
			{
				return 0;
			}

			internal bool _IsTerrible(DateTimeOffset now)
			{
				if (nLastTry != 0 && LastTry >= now - TimeSpan.FromSeconds(60)) // never remove things tried in the last minute
					return false;

				if (Address.Time > now + TimeSpan.FromSeconds(10 * 60)) // came in a flying DeLorean
					return true;

				if (Address.ntime == 0 || now - Address.Time > TimeSpan.FromSeconds(ADDRMAN_HORIZON_DAYS * 24 * 60 * 60)) // not seen in recent history
					return true;

				if (nLastSuccess == 0 && nAttempts >= AddressManager.ADDRMAN_RETRIES) // tried N times and never a success
					return true;

				if (now - LastSuccess > TimeSpan.FromSeconds(ADDRMAN_MIN_FAIL_DAYS * 24 * 60 * 60) && nAttempts >= AddressManager.ADDRMAN_MAX_FAILURES) // N successive failures in the last week
					return true;

				return false;
			}

			internal bool Match(NetworkAddress addr)
			{
				return false;
			}

			internal double Chance
			{
				get
				{
					return GetChance(DateTimeOffset.UtcNow);
				}
			}

			//! Calculate the relative chance this entry should be given when selecting nodes to connect to
			internal double GetChance(DateTimeOffset nNow)
			{
				double fChance = 1.0;

				var nSinceLastSeen = nNow - nTime;
				var nSinceLastTry = nNow - LastTry;

				if (nSinceLastSeen < TimeSpan.Zero)
					nSinceLastSeen = TimeSpan.Zero;
				if (nSinceLastTry < TimeSpan.Zero)
					nSinceLastTry = TimeSpan.Zero;

				// deprioritize very recent attempts away
				if (nSinceLastTry < TimeSpan.FromSeconds(60 * 10))
					fChance *= 0.01;

				// deprioritize 66% after each failed attempt, but at most 1/28th to avoid the search taking forever or overly penalizing outages.
				fChance *= Math.Pow(0.66, Math.Min(nAttempts, 8));

				return fChance;
			}
		}

		//! total number of buckets for tried addresses
		internal const int ADDRMAN_TRIED_BUCKET_COUNT = 256;

		//! total number of buckets for new addresses
		internal const int ADDRMAN_NEW_BUCKET_COUNT = 1024;

		//! maximum allowed number of entries in buckets for new and tried addresses
		internal const int ADDRMAN_BUCKET_SIZE = 64;

		//! over how many buckets entries with tried addresses from a single group (/16 for IPv4) are spread
		internal const int ADDRMAN_TRIED_BUCKETS_PER_GROUP = 8;

		//! over how many buckets entries with new addresses originating from a single group are spread
		internal const int ADDRMAN_NEW_BUCKETS_PER_SOURCE_GROUP = 64;

		//! in how many buckets for entries with new addresses a single address may occur
		const int ADDRMAN_NEW_BUCKETS_PER_ADDRESS = 8;

		//! how old addresses can maximally be
		internal const int ADDRMAN_HORIZON_DAYS = 30;

		//! after how many failed attempts we give up on a new node
		internal const int ADDRMAN_RETRIES = 3;

		//! how many successive failures are allowed ...
		internal const int ADDRMAN_MAX_FAILURES = 10;

		//! ... in at least this many days
		internal const int ADDRMAN_MIN_FAIL_DAYS = 7;

		//! the maximum percentage of nodes to return in a getaddr call
		const int ADDRMAN_GETADDR_MAX_PCT = 23;

		//! the maximum number of nodes to return in a getaddr call
		const int ADDRMAN_GETADDR_MAX = 2500;


#if !NOFILEIO
		public static AddressManager LoadPeerFile(string filePath, Network expectedNetwork = null)
		{
			var addrman = new AddressManager();
			byte[] data, hash;
			using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
			{
				data = new byte[fs.Length - 32];
				fs.Read(data, 0, data.Length);
				hash = new byte[32];
				fs.Read(hash, 0, 32);
			}
			var actual = Hashes.Hash256(data);
			var expected = new uint256(hash);
			if (expected != actual)
				throw new FormatException("Invalid address manager file");

			BitcoinStream stream = new BitcoinStream(data);
			stream.Type = SerializationType.Disk;
			uint magic = 0;
			stream.ReadWrite(ref magic);
			if (expectedNetwork != null && expectedNetwork.Magic != magic)
			{
				throw new FormatException("This file is not for the expected network");
			}
			addrman.ReadWrite(stream);
			return addrman;
		}
		public void SavePeerFile(string filePath, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));

			MemoryStream ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			stream.Type = SerializationType.Disk;
			stream.ReadWrite(network.Magic);
			stream.ReadWrite(this);
			var hash = Hashes.Hash256(ms.ToArray());
			stream.ReadWrite(hash.AsBitcoinSerializable());

			string dirPath = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrWhiteSpace(dirPath))
			{
				Directory.CreateDirectory(dirPath);
			}
			File.WriteAllBytes(filePath, ms.ToArray());
		}
#endif

		AddressInfo Find(object addr)
		{
			int unused;
			return Find(addr, out unused);
		}
		AddressInfo Find(object addr, out int pnId)
		{
			if (!mapAddr.TryGetValue(addr, out pnId))
				return null;
			return mapInfo.TryGet(pnId);
		}
		public AddressManager()
		{
			Clear();
		}


		private void Clear()
		{
			vRandom = new List<int>();
			nKey = new uint256(RandomUtils.GetBytes(32));
			vvNew = new int[ADDRMAN_NEW_BUCKET_COUNT, ADDRMAN_BUCKET_SIZE];
			for (int i = 0; i < ADDRMAN_NEW_BUCKET_COUNT; i++)
				for (int j = 0; j < ADDRMAN_BUCKET_SIZE; j++)
					vvNew[i, j] = -1;

			vvTried = new int[ADDRMAN_TRIED_BUCKET_COUNT, ADDRMAN_BUCKET_SIZE];
			for (int i = 0; i < ADDRMAN_TRIED_BUCKET_COUNT; i++)
				for (int j = 0; j < ADDRMAN_BUCKET_SIZE; j++)
					vvTried[i, j] = -1;

			nIdCount = 0;
			nTried = 0;
			nNew = 0;
		}

		byte nVersion = 1;
		byte nKeySize = 32;
		internal uint256 nKey;
		internal int nNew;
		internal int nTried;
		List<int> vRandom;

		int[,] vvNew;
		int[,] vvTried;

		Dictionary<int, AddressInfo> mapInfo = new Dictionary<int, AddressInfo>();
		Dictionary<object, int> mapAddr = new Dictionary<object, int>();
		private int nIdCount;

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			lock (cs)
			{
				Check();
				if (!stream.Serializing)
					Clear();
				stream.ReadWrite(ref nVersion);
				stream.ReadWrite(ref nKeySize);
				if (!stream.Serializing && nKeySize != 32)
					throw new FormatException("Incorrect keysize in addrman deserialization");
				stream.ReadWrite(ref nKey);
				stream.ReadWrite(ref nNew);
				stream.ReadWrite(ref nTried);

				int nUBuckets = ADDRMAN_NEW_BUCKET_COUNT ^ (1 << 30);
				stream.ReadWrite(ref nUBuckets);
				if (nVersion != 0)
				{
					nUBuckets ^= (1 << 30);
				}
				if (!stream.Serializing)
				{
					// Deserialize entries from the new table.
					for (int n = 0; n < nNew; n++)
					{
						
					}

					nIdCount = nNew;

					// Deserialize entries from the tried table.
					int nLost = 0;
					for (int n = 0; n < nTried; n++)
					{
						
					}

					nTried -= nLost;

					// Deserialize positions in the new table (if possible).
					for (int bucket = 0; bucket < nUBuckets; bucket++)
					{
						int nSize = 0;
						stream.ReadWrite(ref nSize);
						for (int n = 0; n < nSize; n++)
						{
							int nIndex = 0;
							stream.ReadWrite(ref nIndex);
							if (nIndex >= 0 && nIndex < nNew)
							{
								AddressInfo info = mapInfo[nIndex];
								int nUBucketPos = info.GetBucketPosition(nKey, true, bucket);
								if (nVersion == 1 && nUBuckets == ADDRMAN_NEW_BUCKET_COUNT && vvNew[bucket, nUBucketPos] == -1 && info.nRefCount < ADDRMAN_NEW_BUCKETS_PER_ADDRESS)
								{
									info.nRefCount++;
									vvNew[bucket, nUBucketPos] = nIndex;
								}
							}
						}
					}

					// Prune new entries with refcount 0 (as a result of collisions).
					int nLostUnk = 0;
					foreach (var kv in mapInfo.ToList())
					{
						if (kv.Value.fInTried == false && kv.Value.nRefCount == 0)
						{
							Delete(kv.Key);
							nLostUnk++;
						}
					}
				}
				else
				{
					Dictionary<int, int> mapUnkIds = new Dictionary<int, int>();
					int nIds = 0;
					foreach (var kv in mapInfo)
					{
						mapUnkIds[kv.Key] = nIds;
						AddressInfo info = kv.Value;
						if (info.nRefCount != 0)
						{
							assert(nIds != nNew); // this means nNew was wrong, oh ow
							info.ReadWrite(stream);
							nIds++;
						}
					}
					nIds = 0;
					foreach (var kv in mapInfo)
					{
						AddressInfo info = kv.Value;
						if (info.fInTried)
						{
							assert(nIds != nTried); // this means nTried was wrong, oh ow
							info.ReadWrite(stream);
							nIds++;
						}
					}

					for (int bucket = 0; bucket < ADDRMAN_NEW_BUCKET_COUNT; bucket++)
					{
						int nSize = 0;
						for (int i = 0; i < ADDRMAN_BUCKET_SIZE; i++)
						{
							if (vvNew[bucket, i] != -1)
								nSize++;
						}
						stream.ReadWrite(ref nSize);
						for (int i = 0; i < ADDRMAN_BUCKET_SIZE; i++)
						{
							if (vvNew[bucket, i] != -1)
							{
								int nIndex = mapUnkIds[vvNew[bucket, i]];
								stream.ReadWrite(ref nIndex);
							}
						}
					}
				}
				Check();
			}
		}

		#endregion

		//! Add a single address.
		public bool Add(NetworkAddress addr, object source)
		{
			return Add(addr, source, TimeSpan.Zero);
		}

		object cs = new object();
		public bool Add(NetworkAddress addr)
		{
			return false;
		}
		public bool Add(NetworkAddress addr, object source, TimeSpan nTimePenalty)
		{
			bool fRet = false;
			lock (cs)
			{
				Check();
				fRet |= Add_(addr, source, nTimePenalty);
				Check();
			}
			return fRet;
		}
		public bool Add(IEnumerable<NetworkAddress> vAddr, object source)
		{
			return Add(vAddr, source, TimeSpan.FromSeconds(0));
		}
		public bool Add(IEnumerable<NetworkAddress> vAddr, object source, TimeSpan nTimePenalty)
		{
			int nAdd = 0;
			lock (cs)
			{
				Check();
				foreach (var addr in vAddr)
					nAdd += Add_(addr, source, nTimePenalty) ? 1 : 0;
				Check();
			}
			return nAdd > 0;
		}

		private bool Add_(NetworkAddress addr, object source, TimeSpan nTimePenalty)
		{
			return true;
		}

		private void ClearNew(int nUBucket, int nUBucketPos)
		{
			// if there is an entry in the specified bucket, delete it.
			if (vvNew[nUBucket, nUBucketPos] != -1)
			{
				int nIdDelete = vvNew[nUBucket, nUBucketPos];
				AddressInfo infoDelete = mapInfo[nIdDelete];
				assert(infoDelete.nRefCount > 0);
				infoDelete.nRefCount--;
				infoDelete.nRefCount = Math.Max(0, infoDelete.nRefCount);
				vvNew[nUBucket, nUBucketPos] = -1;
				if (infoDelete.nRefCount == 0)
				{
					Delete(nIdDelete);
				}
			}
		}

		private void Delete(int nId)
		{
			

		}

		private void SwapRandom(int nRndPos1, int nRndPos2)
		{
			if (nRndPos1 == nRndPos2)
				return;

			assert(nRndPos1 < vRandom.Count && nRndPos2 < vRandom.Count);

			int nId1 = vRandom[nRndPos1];
			int nId2 = vRandom[nRndPos2];

			assert(mapInfo.ContainsKey(nId1));
			assert(mapInfo.ContainsKey(nId2));

			mapInfo[nId1].nRandomPos = nRndPos2;
			mapInfo[nId2].nRandomPos = nRndPos1;

			vRandom[nRndPos1] = nId2;
			vRandom[nRndPos2] = nId1;
		}

		private AddressInfo Create(NetworkAddress addr, object addrSource, out int pnId)
		{
			pnId = 0;
			return null;
		}

		private AddressInfo Find(NetworkAddress addr, out int nId)
		{
			nId = 0;
			return null;
		}

		internal bool DebugMode
		{
			get;
			set;
		}
		internal void Check()
		{
			if (!DebugMode)
				return;
			lock (cs)
			{
				assert(Check_() == 0);
			}
		}

		private int Check_()
		{
			return 0;
		}



		public void Good(NetworkAddress addr)
		{
			Good(addr, DateTimeOffset.UtcNow);
		}

		public void Good(NetworkAddress addr, DateTimeOffset nTime)
		{
			lock (cs)
			{
				Check();
				Good_(addr, nTime);
				Check();
			}
		}

		private void Good_(NetworkAddress addr, DateTimeOffset nTime)
		{
			int nId;
			AddressInfo pinfo = Find(addr, out nId);

			// if not found, bail out
			if (pinfo == null)
				return;

			AddressInfo info = pinfo;

			// check whether we are talking about the exact same CService (including same port)
			if (!info.Match(addr))
				return;

			// update info
			info.LastSuccess = nTime;
			info.LastTry = nTime;
			info.nAttempts = 0;
			// nTime is not updated here, to avoid leaking information about
			// currently-connected peers.

			// if it is already in the tried set, don't do anything else
			if (info.fInTried)
				return;

			// find a bucket it is in now
			int nRnd = GetRandInt(ADDRMAN_NEW_BUCKET_COUNT);
			int nUBucket = -1;
			for (int n = 0; n < ADDRMAN_NEW_BUCKET_COUNT; n++)
			{
				int nB = (n + nRnd) % ADDRMAN_NEW_BUCKET_COUNT;
				int nBpos = info.GetBucketPosition(nKey, true, nB);
				if (vvNew[nB, nBpos] == nId)
				{
					nUBucket = nB;
					break;
				}
			}

			// if no bucket is found, something bad happened;
			// TODO: maybe re-add the node, but for now, just bail out
			if (nUBucket == -1)
				return;

			// move nId to the tried tables
			MakeTried(info, nId);
		}

		private void MakeTried(AddressInfo info, int nId)
		{
			// remove the entry from all new buckets
			for (int bucket = 0; bucket < ADDRMAN_NEW_BUCKET_COUNT; bucket++)
			{
				int pos = info.GetBucketPosition(nKey, true, bucket);
				if (vvNew[bucket, pos] == nId)
				{
					vvNew[bucket, pos] = -1;
					info.nRefCount--;
				}
			}
			nNew--;

			assert(info.nRefCount == 0);

			// which tried bucket to move the entry to
			int nKBucket = info.GetTriedBucket(nKey);
			int nKBucketPos = info.GetBucketPosition(nKey, false, nKBucket);

			// first make space to add it (the existing tried entry there is moved to new, deleting whatever is there).
			if (vvTried[nKBucket, nKBucketPos] != -1)
			{
				// find an item to evict
				int nIdEvict = vvTried[nKBucket, nKBucketPos];
				assert(mapInfo.ContainsKey(nIdEvict));
				AddressInfo infoOld = mapInfo[nIdEvict];

				// Remove the to-be-evicted item from the tried set.
				infoOld.fInTried = false;
				vvTried[nKBucket, nKBucketPos] = -1;
				nTried--;

				// find which new bucket it belongs to
				int nUBucket = infoOld.GetNewBucket(nKey);
				int nUBucketPos = infoOld.GetBucketPosition(nKey, true, nUBucket);
				ClearNew(nUBucket, nUBucketPos);
				assert(vvNew[nUBucket, nUBucketPos] == -1);

				// Enter it into the new set again.
				infoOld.nRefCount = 1;
				vvNew[nUBucket, nUBucketPos] = nIdEvict;
				nNew++;
			}
			assert(vvTried[nKBucket, nKBucketPos] == -1);

			vvTried[nKBucket, nKBucketPos] = nId;
			nTried++;
			info.fInTried = true;
		}

		private static void assert(bool value)
		{
			if (!value)
				throw new InvalidOperationException("Bug in AddressManager, should never happen, contact NBitcoin developers if you see this exception");
		}
		//! Mark an entry as connection attempted to.
		public void Attempt(NetworkAddress addr)
		{
			Attempt(addr, DateTimeOffset.UtcNow);
		}
		//! Mark an entry as connection attempted to.
		public void Attempt(NetworkAddress addr, DateTimeOffset nTime)
		{
			lock (cs)
			{
				Check();
				Attempt_(addr, nTime);
				Check();
			}
		}

		private void Attempt_(NetworkAddress addr, DateTimeOffset nTime)
		{
			
		}

		//! Mark an entry as currently-connected-to.
		public void Connected(NetworkAddress addr)
		{
			Connected(addr, DateTimeOffset.UtcNow);
		}

		//! Mark an entry as currently-connected-to.
		public void Connected(NetworkAddress addr, DateTimeOffset nTime)
		{
			lock (cs)
			{
				Check();
				Connected_(addr, nTime);
				Check();
			}
		}
		void Connected_(NetworkAddress addr, DateTimeOffset nTime)
		{
			int unused;
			AddressInfo pinfo = Find(addr, out unused);

			// if not found, bail out
			if (pinfo == null)
				return;

			AddressInfo info = pinfo;

			// check whether we are talking about the exact same CService (including same port)
			if (!info.Match(addr))
				return;

			// update info
			var nUpdateInterval = TimeSpan.FromSeconds(20 * 60);
			if (nTime - info.nTime > nUpdateInterval)
				info.nTime = nTime;
		}

		/// <summary>
		/// Choose an address to connect to.
		/// </summary>
		/// <returns>The network address of a peer, or null if none are found</returns>
		public NetworkAddress Select()
		{
			AddressInfo addrRet = null;
			lock (cs)
			{
				Check();
				addrRet = Select_();
				Check();
			}
			return addrRet == null ? null : addrRet.Address;
		}

		private AddressInfo Select_()
		{
			if (vRandom.Count == 0)
				return null;

			var rnd = new Random();

			// Use a 50% chance for choosing between tried and new table entries.
			if (nTried > 0 && (nNew == 0 || GetRandInt(2) == 0))
			{
				// use a tried node
				double fChanceFactor = 1.0;
				while (true)
				{
					int nKBucket = GetRandInt(ADDRMAN_TRIED_BUCKET_COUNT);
					int nKBucketPos = GetRandInt(ADDRMAN_BUCKET_SIZE);
					while (vvTried[nKBucket, nKBucketPos] == -1)
					{
						nKBucket = (nKBucket + rnd.Next(ADDRMAN_TRIED_BUCKET_COUNT)) % ADDRMAN_TRIED_BUCKET_COUNT;
						nKBucketPos = (nKBucketPos + rnd.Next(ADDRMAN_BUCKET_SIZE)) % ADDRMAN_BUCKET_SIZE;
					}
					int nId = vvTried[nKBucket, nKBucketPos];
					assert(mapInfo.ContainsKey(nId));
					AddressInfo info = mapInfo[nId];
					if (GetRandInt(1 << 30) < fChanceFactor * info.Chance * (1 << 30))
						return info;
					fChanceFactor *= 1.2;
				}
			}
			else
			{
				// use a new node
				double fChanceFactor = 1.0;
				while (true)
				{
					int nUBucket = GetRandInt(ADDRMAN_NEW_BUCKET_COUNT);
					int nUBucketPos = GetRandInt(ADDRMAN_BUCKET_SIZE);
					while (vvNew[nUBucket, nUBucketPos] == -1)
					{
						nUBucket = (nUBucket + rnd.Next(ADDRMAN_NEW_BUCKET_COUNT)) % ADDRMAN_NEW_BUCKET_COUNT;
						nUBucketPos = (nUBucketPos + rnd.Next(ADDRMAN_BUCKET_SIZE)) % ADDRMAN_BUCKET_SIZE;
					}
					int nId = vvNew[nUBucket, nUBucketPos];
					assert(mapInfo.ContainsKey(nId));
					AddressInfo info = mapInfo[nId];
					if (GetRandInt(1 << 30) < fChanceFactor * info.Chance * (1 << 30))
						return info;
					fChanceFactor *= 1.2;
				}
			}
		}

		private static int GetRandInt(int max)
		{
			return (int)(RandomUtils.GetUInt32() % (uint)max);
		}

		/// <summary>
		/// Return a bunch of addresses, selected at random.
		/// </summary>
		/// <returns></returns>
		public NetworkAddress[] GetAddr()
		{
			NetworkAddress[] result = null;
			lock (cs)
			{
				Check();
				result = GetAddr_().ToArray();
				Check();
			}
			return result;
		}
		IEnumerable<NetworkAddress> GetAddr_()
		{
			List<NetworkAddress> vAddr = new List<NetworkAddress>();
			int nNodes = ADDRMAN_GETADDR_MAX_PCT * vRandom.Count / 100;
			if (nNodes > ADDRMAN_GETADDR_MAX)
				nNodes = ADDRMAN_GETADDR_MAX;
			// gather a list of random nodes, skipping those of low quality
			for (int n = 0; n < vRandom.Count; n++)
			{
				if (vAddr.Count >= nNodes)
					break;

				int nRndPos = GetRandInt(vRandom.Count - n) + n;
				SwapRandom(n, nRndPos);
				assert(mapInfo.ContainsKey(vRandom[n]));

				AddressInfo ai = mapInfo[vRandom[n]];
				if (!ai.IsTerrible)
					vAddr.Add(ai.Address);
			}
			return vAddr;
		}

		public int Count
		{
			get
			{
				return vRandom.Count;
			}
		}

		internal void DiscoverPeers(Network network, NodeConnectionParameters parameters, int peerToFind)
		{
			TimeSpan backoff = TimeSpan.Zero;
			Logs.NodeServer.LogTrace("Discovering nodes");

			int found = 0;

			{
				while (found < peerToFind)
				{
					Thread.Sleep(backoff);
					backoff = backoff == TimeSpan.Zero ? TimeSpan.FromSeconds(1.0) : TimeSpan.FromSeconds(backoff.TotalSeconds * 2);
					if (backoff > TimeSpan.FromSeconds(10.0))
						backoff = TimeSpan.FromSeconds(10.0);

					parameters.ConnectCancellation.ThrowIfCancellationRequested();

					Logs.NodeServer.LogTrace("Remaining peer to get {remainingPeerCount}", (-found + peerToFind));

					List<NetworkAddress> peers = new List<NetworkAddress>();
					peers.AddRange(this.GetAddr());
					if (peers.Count == 0)
					{
						PopulateTableWithDNSNodes(network, peers).GetAwaiter().GetResult();
						PopulateTableWithHardNodes(network, peers);
						peers = new List<NetworkAddress>(peers.OrderBy(a => RandomUtils.GetInt32()));
						if (peers.Count == 0)
							return;
					}

					CancellationTokenSource peerTableFull = new CancellationTokenSource();
					CancellationToken loopCancel = CancellationTokenSource.CreateLinkedTokenSource(peerTableFull.Token, parameters.ConnectCancellation).Token;
					try
					{
						Parallel.ForEach(peers, new ParallelOptions()
						{
							MaxDegreeOfParallelism = 5,
							CancellationToken = loopCancel,
						}, p =>
						{
							using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
							using (var cancelConnection = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, loopCancel))
							{
								Node n = null;
								try
								{
									var param2 = parameters.Clone();
									param2.ConnectCancellation = cancelConnection.Token;
									var addrman = param2.TemplateBehaviors.Find<AddressManagerBehavior>();
									param2.TemplateBehaviors.Clear();
									param2.TemplateBehaviors.Add(addrman);
									n = Node.Connect(network, p.Endpoint, param2);
									n.VersionHandshake(cancelConnection.Token);
									n.MessageReceived += (s, a) =>
									{
										var addr = (a.Message.Payload as AddrPayload);
										if (addr != null)
										{
											Interlocked.Add(ref found, addr.Addresses.Length);
											backoff = TimeSpan.FromSeconds(0);
											if (found >= peerToFind)
												peerTableFull.Cancel();
										}
									};
									n.SendMessageAsync(new GetAddrPayload());
									loopCancel.WaitHandle.WaitOne(2000);
								}
								catch
								{
								}
								finally
								{
									if (n != null)
										n.DisconnectAsync();
								}
							}
							if (found >= peerToFind)
								peerTableFull.Cancel();
							else
								Logs.NodeServer.LogInformation("Need {neededPeerCount} more peers", (-found + peerToFind));
						});
					}
					catch (OperationCanceledException)
					{
						if (parameters.ConnectCancellation.IsCancellationRequested)
							throw;
					}
				}
			}
		}

		private static Task PopulateTableWithDNSNodes(Network network, List<NetworkAddress> peers)
		{
			return Task.WhenAll(network.DNSSeeds
				.Select(async dns =>
				{
					try
					{
						return (await dns.GetAddressNodesAsync(network.DefaultPort).ConfigureAwait(false)).Select(o => new NetworkAddress(o)).ToArray();
					}
					catch
					{
						return new NetworkAddress[0];
					}
				})
				.ToArray());
		}

		private static void PopulateTableWithHardNodes(Network network, List<NetworkAddress> peers)
		{
			peers.AddRange(network.SeedNodes);
		}
	}
}
#endif
