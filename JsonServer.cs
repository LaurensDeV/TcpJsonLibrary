using System;
using System.Net;
using System.Net.Sockets;

namespace TcpJsonLibrary
{
	public class JsonServer : TcpListener
	{
		public delegate void ClientAcceptedDelegate(JsonClient client);
		public event ClientAcceptedDelegate ClientAccepted;

		public JsonServer(IPAddress address, int port) : base(address, port) { }

		public new void Start()
		{
			if (Active)
				return;

			base.Start();
			BeginAcceptTcpClient(AcceptTcpClient, null);
		}

		public new void Stop()
		{
			if (!Active)
				return;
		
			base.Stop();
		}

		public void AcceptTcpClient(IAsyncResult ar)
		{
			TcpClient tcpClient = this.EndAcceptTcpClient(ar);

			this.BeginAcceptTcpClient(AcceptTcpClient, null);

			ClientAccepted?.Invoke(new JsonClient(tcpClient));
		}
	}
}
