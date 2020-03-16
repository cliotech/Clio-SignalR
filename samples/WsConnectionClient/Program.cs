using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace WsConnectionClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connection = CreateConnection();
            connection.StateChanged += Connection_StateChanged;
            connection.Received += m =>
            {
                Connection_Received(m);
                if (m.Contains(@"""action"":""ack"""))
                    connection.Send(@"{""ack"":1}");
            };
            await connection.Start(new WebSocketTransport());
            await connection.Send(
                @"{""methodName"":""RegisterQuotesAndGetInitialAll"",""params"":{""instruments"":[3631,3588,12086,12055,3607,12037,303,12036,12078,4150,12066,3661,3630,4143,12122],""symbol"":null}}");

            Console.ReadKey();
        }

        private static void Connection_StateChanged(StateChange obj) => Console.WriteLine($"{obj.OldState}->{obj.NewState}");

        private static void Connection_Received(string message) => Console.WriteLine(message);

        private static Connection CreateConnection()
        {
            var url = "wss://csmqa1.fihtrader.com/csmstream";
            var query = "rawWs=true&connectionToken=Xaj6rOL%2F%2FCp2bGdUcb1K2r1SuU5ra6C18vXlhXO2FQBIp53fDnrm8giy76sqS5n0TvwebFUudrpCtFBZzPF%2F1RTnHZNq6os6QRURGIl%2BHLNkayla&jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IjE3NDUxMjkiLCJyb2xlIjoiRlhuZXRfQ3VzdG9tZXJzIiwiU2Vzc2lvbklEIjoiNzBmOThkZjMtOWEzYi00NzAzLTk4ODYtOTEyMjIwNmU4ZTJlIiwiQXBwbGljYXRpb25JRCI6IjIwIiwiQWNjb3VudE51bWJlciI6IjE3NDUxMjkiLCJuYmYiOjE1ODQzOTIxMDEsImV4cCI6MTU4NDQzNTMwMSwiaWF0IjoxNTg0MzkyMTAxLCJhdWQiOiJodHRwczovL3RyYWRlcnFhMS52ZXN0bGUuY29tIn0._BhkrMUKjmZHsvUngj97qrNReaz0-bOI33Hq-526YXU";
            return new Connection(url, query);
        }
    }
}
