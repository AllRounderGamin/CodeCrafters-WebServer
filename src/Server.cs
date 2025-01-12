using System.Data.SqlTypes;
using System.IO.Compression;
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
    string requestType = data[0].Split(" ")[0];
    int encodingIndex = Array.FindIndex(data, str => str.StartsWith("Accept-Encoding", StringComparison.InvariantCultureIgnoreCase));
    string encodingMes = "";
    string encodingMethod = "";
    if (encodingIndex != -1){
        if (data[encodingIndex].Split(":")[1].Contains("gzip", StringComparison.InvariantCultureIgnoreCase)){
            encodingMethod = "gzip";
            encodingMes = "Content-Encoding: gzip\r\n";
        }
    }


    switch (requestedURL[1]){
        case "":
            await socket.SendAsync(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n"));
            break;
        case "echo":
            if (encodingMethod == "gzip"){
                byte[] mesBytes = Encoding.UTF8.GetBytes(requestedURL[2]);
                using MemoryStream mem = new();
                using GZipStream gzip = new(mem, CompressionMode.Compress);
                gzip.Write(mesBytes);
                gzip.Flush();
                gzip.Close();
                byte[] compressedBytes = mem.ToArray();
                byte[] compressedEchoResponse = Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\n{encodingMes}Content-Type: text/plain\r\nContent-Length: " + compressedBytes.Length + "\r\n\r\n");
                compressedEchoResponse = [..compressedEchoResponse, ..compressedBytes];
                await socket.SendAsync(compressedEchoResponse);
            } else{
                string echoResponse = $"HTTP/1.1 200 OK\r\n{encodingMes}Content-Type: text/plain\r\nContent-Length: " + requestedURL[2].Length + "\r\n\r\n" + requestedURL[2];
                await socket.SendAsync(Encoding.UTF8.GetBytes(echoResponse));
            }
            break;
        case "user-agent":
            int agentIndex = Array.FindIndex(data, str => str.StartsWith("User-Agent", StringComparison.InvariantCultureIgnoreCase));
            string userAgent = data[agentIndex].Split(":")[1].Trim();
            string agentResponse = $"HTTP/1.1 200 OK\r\n{encodingMes}Content-Type: text/plain\r\nContent-Length: " + userAgent.Length + "\r\n\r\n" + userAgent;
            await socket.SendAsync(Encoding.UTF8.GetBytes(agentResponse));
            break;
        case "files":
            if (requestType == "GET"){
                string fileUrl = Environment.GetCommandLineArgs()[2] + requestedURL[2];
                if (!File.Exists(fileUrl)){
                    await socket.SendAsync(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
                }
                string fileContent = File.ReadAllText(fileUrl);
                string fileResponse = $"HTTP/1.1 200 OK\r\n{encodingMes}Content-Type: application/octet-stream\r\nContent-Length: " + fileContent.Length + "\r\n\r\n" + fileContent;
                await socket.SendAsync(Encoding.UTF8.GetBytes(fileResponse));
            } else if (requestType == "POST"){
                string fileUrl = Environment.GetCommandLineArgs()[2] + requestedURL[2];
                string fileContent = data[^1].Trim('\x00');
                File.WriteAllText(fileUrl, fileContent);
                await socket.SendAsync(Encoding.UTF8.GetBytes("HTTP/1.1 201 Created\r\n\r\n"));
            }
            break;
        default:
            await socket.SendAsync(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
            break;
        }
    socket.Close();
}

// curl -i -X GET http://localhost:4221/

// Refactor to make response messages an object that can be made with parameters, current solution for adding encoding mes is very bandaid-y
