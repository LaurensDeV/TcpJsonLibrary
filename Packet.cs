using System;

namespace TcpJsonLibrary
{
	public class Packet
	{
		public string Key { get; private set; }
		public dynamic Data { get; private set; }

		public Packet(string key, dynamic data)
		{
			this.Key = key; ;
			this.Data = data;
		}
	}
}
