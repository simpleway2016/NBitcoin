#if !NOobject
using System;
using System.Collections.Generic;

using System.Text;

namespace NBitcoin.Protocol
{
	public class objectSettings
	{
		/// <summary>
		/// Set <see cref="object.ReceiveTimeout"/> value before connecting
		/// </summary>
		public TimeSpan? ReceiveTimeout { get; set; }

		/// <summary>
		/// Set <see cref="object.SendTimeout"/> value before connecting
		/// </summary>
		public TimeSpan? SendTimeout { get; set; }


		/// <summary>
		/// Set <see cref="object.ReceiveBufferSize"/> value before connecting
		/// </summary>
		public int? ReceiveBufferSize
		{
			get;
			set;
		}
		/// <summary>
		/// Set <see cref="object.SendBufferSize"/> value before connecting
		/// </summary>
		public int? SendBufferSize
		{
			get;
			set;
		}

		public void SetobjectProperties(object s)
		{
			
			////////////////////////
		}

		public objectSettings Clone()
		{
			return new objectSettings()
			{
				ReceiveTimeout = ReceiveTimeout,
				SendTimeout = SendTimeout,
				SendBufferSize = SendBufferSize,
				ReceiveBufferSize = ReceiveBufferSize
			};
		}
	}
}
#endif
