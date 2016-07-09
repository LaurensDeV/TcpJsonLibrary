using Newtonsoft.Json;

namespace TcpJsonLibrary
{
	public static class Utils
	{
		public static bool ParsePacket(string data, out PacketReceivedEventArgs e)
		{
			e = default(PacketReceivedEventArgs);
			try
			{
				int splitIndex = data.IndexOf('|');
				int id = int.Parse(data.Substring(0, splitIndex));
				dynamic json = JsonConvert.DeserializeObject(data.Substring(splitIndex + 1));
				e = new PacketReceivedEventArgs(id, json);
				return true;
			}
			catch { return false; }
		}
	}
}
