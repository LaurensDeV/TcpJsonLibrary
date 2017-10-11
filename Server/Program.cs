using System;
using System.Collections.Generic;
using System.Net;
using TcpJsonLibrary;

namespace Server
{
	class Program
	{
		static List<JsonClient> clients;

		static JsonServer server;
		static void Main(string[] args)
		{
			clients = new List<JsonClient>();

			server = new JsonServer(IPAddress.Any, 1234);

			server.Start();

			server.ClientAccepted += Server_ClientAccepted;

			for (;;)
			{
				string msg = Console.ReadLine();

				foreach (var client in clients)
				{
					client.Emit("broadcast", new { Message = msg });
				}
			}
		}

		private static void Server_ClientAccepted(JsonClient client)
		{
			clients.Add(client);
			Console.WriteLine($"Client connected ({client.Client.RemoteEndPoint})");
			client.ClientDisconnected += Client_ClientDisconnected;
			UpdateTitle();
		}

		private static void Client_ClientDisconnected(JsonClient client)
		{
			Console.WriteLine($"Client disconnected ({client.Client.RemoteEndPoint})");
			clients.RemoveAll(c => c == client);
			UpdateTitle();
		}

		static void UpdateTitle()
		{
			Console.Title = $"Users connected: {clients.Count}";
		}
	}
}
