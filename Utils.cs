using Newtonsoft.Json;

namespace TcpJsonLibrary
{
	public static class Utils
	{
		public static bool ParsePacket(string data, out Packet packet)
		{
			packet = default(Packet);
			try
			{
				int splitIndex = data.IndexOf('|');
				string key = data.Substring(0, splitIndex);
				packet = new Packet(key, JsonConvert.DeserializeObject(data.Substring(splitIndex + 1)));
				return true;
			}
			catch { return false; }
		}
	}
}
