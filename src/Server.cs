using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new(IPAddress.Any, 4221);
server.Start();
while (true){
Socket socket = server.AcceptSocket(); // wait for client

byte[] buffer = new byte[1_024];
int received = await socket.ReceiveAsync(buffer);
string message = Encoding.UTF8.GetString(buffer);
string[] data = message.Split("\r\n");
string[] requestedURL = data[0].Split(" ")[1].Split("/");


switch (requestedURL[1]){
    case "":
        await socket.SendAsync(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n"));
        break;
    case "echo":
        string response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: " + requestedURL[2].Length.ToString() + "\r\n\r\n" + requestedURL[2];
        await socket.SendAsync(Encoding.UTF8.GetBytes(response));
        break;
    default:
        await socket.SendAsync(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
        break;
}
socket.Close();
}



// curl -i -X GET http://localhost:4221/