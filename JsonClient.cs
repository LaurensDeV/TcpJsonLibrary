using Newtonsoft.Json;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpJsonLibrary
{
	public class JsonClient : IDisposable
	{
		public TcpClient tcpClient { get; private set; }
		public const int BUFFER_SIZE = 65536;
		public byte[] buffer = new byte[BUFFER_SIZE];
		public NetworkStream networkStream => tcpClient.GetStream();

		public delegate void PacketReceiveEventHandler(JsonClient sender, PacketReceivedEventArgs e);
		public delegate void ClientDisconnectedEventHandler(JsonClient e);

		public event PacketReceiveEventHandler PacketReceived;
		public event ClientDisconnectedEventHandler ClientDisconnected;

		public JsonClient(string hostname, int port) : this(new TcpClient(hostname, port)) { }

		public JsonClient(TcpClient client)
		{
			this.tcpClient = client;
			if (client.Connected)
				networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, null);
		}

		public JsonClient()
		{
			tcpClient = new TcpClient();
		}

		public bool Connect(string hostname, int port)
		{
			try
			{
				tcpClient = new TcpClient(hostname, port);
				return true;
			}
			catch { return false; }
		}

		public async Task<bool> ConnectAsync(string hostname, int port)
		{
			try
			{
				return await Task.Run(async () =>
				{
					return await tcpClient.ConnectAsync(hostname, port).ContinueWith((o) =>
					{
						networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, null);
						return tcpClient.Connected;
					});
				});
			}
			catch { return false; }
		}

		private void ReadCallback(IAsyncResult ar)
		{
			try
			{
				int len = networkStream.EndRead(ar);
				if (len <= 0)
				{
					Dispose();
					return;
				}
				byte[] data = new byte[len];
				Array.Copy(buffer, data, len);
				Array.Clear(buffer, 0, BUFFER_SIZE);
				string packet = UTF8Encoding.UTF8.GetString(data);

				PacketReceivedEventArgs e;
				if (Utils.ParsePacket(packet, out e))
					PacketReceived?.Invoke(this, e);
				networkStream.BeginRead(buffer, 0, BUFFER_SIZE, ReadCallback, null);
			}
			catch { Dispose(); }
		}

		public void Send(int packetType, dynamic data)
		{
			string msg = $"{packetType}|{JsonConvert.SerializeObject(data)}";
			byte[] byteArr = Encoding.UTF8.GetBytes(msg);
			networkStream.Write(byteArr, 0, byteArr.Length);
		}

		public void SendAsync(int packetType, dynamic data)
		{
			string msg = $"{packetType}|{JsonConvert.SerializeObject(data)}";
			byte[] byteArr = Encoding.UTF8.GetBytes(msg);
			networkStream.BeginWrite(byteArr, 0, byteArr.Length, WriteCallback, null);
		}

		private void WriteCallback(IAsyncResult ar)
		{
			networkStream.EndWrite(ar);
		}

		public void Dispose()
		{
			tcpClient.Close();
			ClientDisconnected?.Invoke(this);
		}
	}
}
