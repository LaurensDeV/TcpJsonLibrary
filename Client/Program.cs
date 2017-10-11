using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpJsonLibrary;

namespace Client
{
	class Program
	{
		static void Main(string[] args)
		{
			JsonClient client = new JsonClient();

			client.Connect("127.0.0.1", 1234);

			Console.WriteLine($"Connected to {client.Client.RemoteEndPoint}");

			client.On("msg", (data) =>
			{
				Console.WriteLine(data.Message);
			});

			client.On("broadcast", (data) =>
			{
				Console.WriteLine($"Server Broadcast - {data.Message}");
			});

			Console.ReadKey();
		}
	}
}
