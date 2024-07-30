using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new(IPAddress.Any, 4221);
server.Start();
while (true){
    Socket socket = server.AcceptSocket(); // wait for client
    Task connection = new (() => handleConn(socket));
    connection.Start();
}

async static void handleConn(Socket socket){
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
            string echoResponse = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: " + requestedURL[2].Length + "\r\n\r\n" + requestedURL[2];
            await socket.SendAsync(Encoding.UTF8.GetBytes(echoResponse));
            break;
        case "user-agent":
            int agentIndex = Array.FindIndex(data, str => str.StartsWith("User-Agent", StringComparison.InvariantCultureIgnoreCase));
            string userAgent = data[agentIndex].Split(":")[1].Trim();
            string agentResponse = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: " + userAgent.Length + "\r\n\r\n" + userAgent;
            await socket.SendAsync(Encoding.UTF8.GetBytes(agentResponse));
            break;
        case "files":
            string fileUrl = Environment.GetCommandLineArgs()[2] + requestedURL[2];
            if (!File.Exists(fileUrl)){
                await socket.SendAsync(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
            }
            string fileContent = File.ReadAllText(fileUrl);
            string fileResponse = "HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: " + fileContent.Length + "\r\n\r\n" + fileContent;
            await socket.SendAsync(Encoding.UTF8.GetBytes(fileResponse));
            break;
        default:
            await socket.SendAsync(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
            break;
        }
    socket.Close();
}

// curl -i -X GET http://localhost:4221/