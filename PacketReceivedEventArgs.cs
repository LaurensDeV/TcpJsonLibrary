using System;

namespace TcpJsonLibrary
{
	public class PacketReceivedEventArgs : EventArgs
	{
		public int PacketType { get; private set; }
		public dynamic Data { get; private set; }

		public PacketReceivedEventArgs(int packetType, dynamic data)
		{
			this.PacketType = packetType;
			this.Data = data;
		}
	}
}
