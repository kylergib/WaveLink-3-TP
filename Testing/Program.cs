using Fleck;


var server = new WebSocketServer("ws://0.0.0.0:28196");
server.Start(socket =>
{
    socket.OnOpen = () =>
    {
        Console.WriteLine("Client connected: " + socket.ConnectionInfo.ClientIpAddress);
    };

    socket.OnClose = () =>
    {
        Console.WriteLine("Client disconnected");
    };

    socket.OnMessage = message =>
    {
        Console.WriteLine("Received: " + message);

        // Echo back
        socket.Send("Server received: " + message);
    };
});

Console.WriteLine("WebSocket server running on ws://localhost:28196");
Console.ReadLine();
