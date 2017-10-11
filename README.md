# TcpJsonLibrary
Simple tcp server that uses Json for packets


# Example usage

### Client
```csharp
JsonClient client = new JsonClient();

client.Connect("127.0.0.1", 1234);

client.On("msg", (data) =>
{
  Console.WriteLine(data.Message);
});
```

### Server
```csharp
clients = new List<JsonClient>();

server = new JsonServer(IPAddress.Any, 1234);

server.Start();

server.ClientAccepted += Server_ClientAccepted;

for (;;)
{
  string msg = Console.ReadLine();

  foreach (var client in clients)
  {
    client.Emit("msg", new { Message = msg });
  }
}
```

![alt text](https://image.prntscr.com/image/A1uOvuJYQWCl5TerS249IA.png)
