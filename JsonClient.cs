using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
		private Dictionary<string, Queue<Action<dynamic>>> Callbacks;
		private Dictionary<string, Action<dynamic>> OnPacketAction;

		public delegate void ClientDisconnectedEventHandler(JsonClient e);
		public event ClientDisconnectedEventHandler ClientDisconnected;

		public JsonClient(string hostname, int port) : this(new TcpClient(hostname, port)) { }

		public JsonClient(TcpClient client) : this()
		{
			this.tcpClient = client;
			if (client.Connected)
				networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, null);
		}

		public JsonClient()
		{
			OnPacketAction = new Dictionary<string, Action<dynamic>>();
			Callbacks = new Dictionary<string, Queue<Action<dynamic>>>();
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
				string packetStr = UTF8Encoding.UTF8.GetString(data);

				Packet packet;
				if (Utils.ParsePacket(packetStr, out packet))
				{
					if (Callbacks.ContainsKey(packet.Key))
					{
						Action<dynamic> callback = Callbacks[packet.Key].Dequeue();
						callback.Invoke(packet.Data);
					}
					else if (OnPacketAction.ContainsKey(packet.Key))
						OnPacketAction[packet.Key].Invoke(packet.Data);
				}
				networkStream.BeginRead(buffer, 0, BUFFER_SIZE, ReadCallback, null);
			}
			catch { Dispose(); }
		}

		public void On(string key, Action<dynamic> action, dynamic callback = null)
		{
			if (OnPacketAction.ContainsKey(key))
				OnPacketAction[key] = action;
			else
				OnPacketAction.Add(key, action);
			if (callback != null)
				Emit(key, callback);
		}

		public void Emit(string key, dynamic json, Action<dynamic> callback = null)
		{
			addCallback(key, callback);
			string msg = $"{key}|{JsonConvert.SerializeObject(json)}";
			byte[] byteArr = Encoding.UTF8.GetBytes(msg);
			networkStream.BeginWrite(byteArr, 0, byteArr.Length, WriteCallback, null);
		}

		private void addCallback(string key, Action<dynamic> callback)
		{
			if (callback != null)
			{
				if (!Callbacks.ContainsKey(key))
					Callbacks.Add(key, new Queue<Action<dynamic>>());
				Callbacks[key].Enqueue(callback);
			}
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
