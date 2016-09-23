using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
					client.Emit("msg", new { msg = msg });
				}
			}
		}

		private static void Server_ClientAccepted(JsonClient client)
		{
			clients.Add(client);
			client.ClientDisconnected += Client_ClientDisconnected;
		}

		private static void Client_ClientDisconnected(JsonClient e)
		{
			clients.RemoveAll(c => c == e);
		}
	}
}
