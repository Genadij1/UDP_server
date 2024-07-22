using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Server
{
    private static readonly ConcurrentDictionary<IPEndPoint, DateTime> _activeClients = new ConcurrentDictionary<IPEndPoint, DateTime>();
    private static readonly ConcurrentDictionary<IPEndPoint, int> _clientRequests = new ConcurrentDictionary<IPEndPoint, int>();
    private const int MaxRequestsPerHour = 10;
    private static readonly TimeSpan RequestLimitPeriod = TimeSpan.FromHours(1);
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(10);

    static async Task Main()
    {
        UdpClient udpServer = new UdpClient(8888);
        Console.WriteLine("Server started. Waiting for requests...");

        while (true)
        {
            var result = await udpServer.ReceiveAsync();
            IPEndPoint clientEndPoint = result.RemoteEndPoint;
            string requestMessage = Encoding.ASCII.GetString(result.Buffer);

            // Update last activity time
            _activeClients[clientEndPoint] = DateTime.Now;

            // Handle client request
            if (_clientRequests.TryGetValue(clientEndPoint, out int requestCount))
            {
                if (DateTime.Now - _activeClients[clientEndPoint] > RequestLimitPeriod)
                {
                    _clientRequests[clientEndPoint] = 0; // Reset count if the period has passed
                }
                else if (requestCount >= MaxRequestsPerHour)
                {
                    // Limit exceeded
                    await udpServer.SendAsync(Encoding.ASCII.GetBytes("Request limit exceeded. Try again later."), clientEndPoint);
                    continue;
                }
            }
            else
            {
                _clientRequests[clientEndPoint] = 0;
            }

            // Process request
            string responseMessage = GetPrice(requestMessage);
            _clientRequests[clientEndPoint]++;

            // Send response
            byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessage);
            await udpServer.SendAsync(responseBytes, responseBytes.Length, clientEndPoint);
        }
    }

    private static string GetPrice(string request)
    {
        return request switch
        {
            "processor" => "$200",
            "memory" => "$100",
            "harddisk" => "$80",
            _ => "Unknown component"
        };
    }
}
