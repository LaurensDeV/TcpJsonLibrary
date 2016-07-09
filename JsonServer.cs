using System;
using System.Net.Sockets;
using System.Net;

namespace TcpJsonLibrary
{
	public class JsonServer
	{
		private TcpListener _listener;
		public bool IsListening { get; private set; }

		public delegate void ClientAcceptedDelegate(JsonClient client);
		public event ClientAcceptedDelegate ClientAccepted;

		public JsonServer(IPAddress address, int port)
		{
			_listener = new TcpListener(address, port);
		}

		public void Start()
		{
			if (IsListening)
				return;
			IsListening = true;

			_listener.Start();

			_listener.BeginAcceptTcpClient(AcceptTcpClient, null);
		}

		public void Stop()
		{
			if (!IsListening)
				return;
			IsListening = false;

			_listener.Stop();
		}

		public void AcceptTcpClient(IAsyncResult ar)
		{
			TcpClient tcpClient = _listener.EndAcceptTcpClient(ar);

			_listener.BeginAcceptTcpClient(AcceptTcpClient, null);

			ClientAccepted?.Invoke(new JsonClient(tcpClient));
		}
	}
}
