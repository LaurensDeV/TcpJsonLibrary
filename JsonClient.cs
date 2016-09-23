using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpJsonLibrary
{
	public class JsonClient : TcpClient, IDisposable
	{
		public const int BUFFER_SIZE = 1024;
		private byte[] buffer;
		private byte[] lenBuffer;
		private NetworkStream networkStream => GetStream();
		private Dictionary<string, Queue<Action<dynamic>>> Callbacks;
		private Dictionary<string, Action<dynamic>> OnPacketAction;
		public Func<string, string> encryption = null;
		public Func<string, string> decryption = null; 

		public delegate void ClientDisconnectedEventHandler(JsonClient e);
		public event ClientDisconnectedEventHandler ClientDisconnected;

		public JsonClient(string hostname, int port) : this(new TcpClient(hostname, port)) { }

		public JsonClient(TcpClient client) : this()
		{
			Client = client.Client;
			if (Connected)
				networkStream.BeginRead(lenBuffer, 0, lenBuffer.Length, ReceiveCallback, null);
		}

		public JsonClient()
		{
			buffer = new byte[BUFFER_SIZE];
			lenBuffer = new byte[4];
			OnPacketAction = new Dictionary<string, Action<dynamic>>();
			Callbacks = new Dictionary<string, Queue<Action<dynamic>>>();
		}

		public new async Task ConnectAsync(string hostname, int port)
		{
			await base.ConnectAsync(hostname, port).ContinueWith((o) =>
			{
				if (Connected)
					networkStream.BeginRead(lenBuffer, 0, lenBuffer.Length, ReceiveCallback, null);
			});
		}

		public new void Connect(string hostname, int port)
		{
			base.Connect(hostname, port);
			networkStream.BeginRead(lenBuffer, 0, lenBuffer.Length, ReceiveCallback, null);
		}

		public void SetEncryption(Func<string, string> method)
		{
			encryption = method;
		}

		public void SetDecryption(Func<string, string> method)
		{
			decryption = method;
		}

		private void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				int len = networkStream.EndRead(ar);
				if (len <= 0)
				{
					Close();
					return;
				}
				int toReceive = BitConverter.ToInt32(lenBuffer, 0);
				ReceivePacket(new MemoryStream(), toReceive);
			}
			catch { Close(); }
		}

		private void ReceivePacket(MemoryStream ms, int toReceive)
		{
			try
			{
				int len = networkStream.Read(buffer, 0, Math.Min(toReceive, buffer.Length));
				if (len <= 0)
				{
					Close();
					return;
				}
				toReceive -= len;
				ms.Write(buffer, 0, len);
				if (toReceive - len > 0)
				{
					ReceivePacket(ms, toReceive - len);
					return;
				}
				ms.Position = 0L;

				using (BinaryReader br = new BinaryReader(ms))
				{
					string key = br.ReadString();

					string dataStr = br.ReadString();

					if (decryption != null )
						dataStr = decryption.Invoke(dataStr);

					dynamic data = JsonConvert.DeserializeObject(dataStr);

					lock (Callbacks)
					{
						if (Callbacks.ContainsKey(key))
						{
							Action<dynamic> callback = Callbacks[key].Dequeue();
							callback.Invoke(data);
						}
						else if (OnPacketAction.ContainsKey(key))
							OnPacketAction[key].Invoke(data);
					}
				}
				ms.Close();
			}
			catch (SocketException) { Close(); }
			catch (Exception) {  /*Don't handle*/ }
			networkStream.BeginRead(lenBuffer, 0, lenBuffer.Length, ReceiveCallback, null);
		}

		public void On(string key, Action<dynamic> action)
		{
			lock (OnPacketAction)
			{
				if (OnPacketAction.ContainsKey(key))
					OnPacketAction[key] = action;
				else
					OnPacketAction.Add(key, action);
			}
		}

		public void Emit(string key, dynamic json, Action<dynamic> callback = null)
		{
			addCallback(key, callback);

			using (MemoryStream ms = new MemoryStream())
			{
				ms.Position = 4L;
				using (BinaryWriter bw = new BinaryWriter(ms))
				{
					bw.Write(key);

					string jsonStr = JsonConvert.SerializeObject(json);

					if (encryption != null && decryption != null)
						jsonStr = encryption.Invoke(jsonStr);

					bw.Write(jsonStr);
					ms.Position = 0L;
					bw.Write((int)ms.Length);
				}
				byte[] byteArr = ms.ToArray();
				networkStream.BeginWrite(byteArr, 0, byteArr.Length, WriteCallback, null);
			}
		}

		private void addCallback(string key, Action<dynamic> callback)
		{
			if (callback == null)
				return;

			lock (Callbacks)
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

		public new void Close()
		{			base.Close();
			ClientDisconnected?.Invoke(this);
		}

		public void Dispose()
		{
			Callbacks.Clear();
		}
	}
}
