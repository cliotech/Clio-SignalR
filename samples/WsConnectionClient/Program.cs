﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Newtonsoft.Json;

namespace WsConnectionClient
{
    class Program
    {
        private static CsmPushLoadClient _client;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Press any key to connect");
            Console.ReadKey();

            var url = "wss://csmqa1.fihtrader.com/csmstream";
            var connectionToken = @"LU/WNPD/na3OfAWr1Qxt0HBWtl2flN8zoH6rWr/djWQnHPieV8SU7L4v9+QPoc6rQK50mntDj4J/sCoj49UD1BkK5AoJtHlOldxLPxb+AFMLIT+A";
            var jwt = @"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IjE3NDUxMjkiLCJyb2xlIjoiRlhuZXRfQ3VzdG9tZXJzIiwiU2Vzc2lvbklEIjoiY2IzOTAxMjktZjdlNS00Y2ZiLTg3YzktZjg2NGI5YTYwZjViIiwiQXBwbGljYXRpb25JRCI6IjIwIiwiQWNjb3VudE51bWJlciI6IjE3NDUxMjkiLCJuYmYiOjE1ODcwMjY1NTIsImV4cCI6MTU4NzA2OTc1MiwiaWF0IjoxNTg3MDI2NTUyLCJhdWQiOiJodHRwczovL3RyYWRlcnFhMS52ZXN0bGUuY29tIn0.GcYdb_qUCx9atRNYTHeNih4r-Le1wW9mH_JkJ-nyjR0";

            _client = new CsmPushLoadClient(state => Console.WriteLine(state), count => Console.Title = $@"Total Csm Updates:{count}");
            await _client.Connect(url, connectionToken, jwt);
            Console.WriteLine("Press any key to disconnect and display metrics");
            Console.ReadKey();
            Console.WriteLine("Stopping");

            var metrics = _client.StopAndCollectMetrics();
            Console.Write(JsonConvert.SerializeObject(metrics, Formatting.Indented));
            Console.WriteLine();
            Console.ReadKey();
            _client.Dispose();
        }

    }

    public class CsmPushLoadClient : IDisposable
    {
        private readonly Action<ConnectionState> _onConnectionStateChanged;
        private readonly Action<long> _onCsmUpdate;

        public class Metrics
        {
            const double ElapsedMsEpsilon = 0.0001;
            private readonly ConcurrentQueue<long> _samples = new ConcurrentQueue<long>();
            private double _connectTillConnectedMs = ElapsedMsEpsilon;
            private double _connectTillFirstClientStateMs = ElapsedMsEpsilon;
            private readonly Stopwatch _sw = new Stopwatch();

            public double ConnectTillConnectedMs => _connectTillConnectedMs;

            public double ConnectTillFirstClientStateMs => _connectTillFirstClientStateMs;
            public double AvgUpdateFrequencyMs => _samples.Average();
            public double MaxUpdateFrequencyMs => _samples.Max();
            public int SamplesCount => _samples.Count;

            public void StartConnecting() => _sw.Start();

            public void Connected() =>
                Interlocked.CompareExchange(ref _connectTillConnectedMs, _sw.ElapsedMilliseconds, ElapsedMsEpsilon);

            public void ClientStateUpdate()
            {
                _samples.Enqueue(_sw.ElapsedMilliseconds);
                if (_connectTillFirstClientStateMs <= ElapsedMsEpsilon && _samples.TryDequeue(out var firstClientState))
                    Interlocked.CompareExchange(ref _connectTillFirstClientStateMs, firstClientState, ElapsedMsEpsilon);
                _sw.Restart();
            }
        }

        private Connection _connection;
        private readonly Metrics _metrics = new Metrics();

        public CsmPushLoadClient(Action<ConnectionState> onConnectionStateChanged = null, Action<long> onCsmUpdate = null)
        {
            _onConnectionStateChanged = onConnectionStateChanged;
            _onCsmUpdate = onCsmUpdate;
        }

        public async Task Connect(string url, string connectionToken, string jwt)
        {
            var query = $"rawWs=true&connectionToken={Uri.EscapeDataString(connectionToken)}&jwt={jwt}";

            _connection = new Connection(url, query);
            _connection.StateChanged += Connection_StateChanged;
            _connection.Received += Connection_Received;

            _metrics.StartConnecting();

            await _connection.Start(new WebSocketTransport());
            await _connection.Send(
                @"{""methodName"":""RegisterQuotesAndGetInitialAll"",""params"":{""instruments"":[3631,3588,12086,12055,3607,12037,303,12036,12078,4150,12066,3661,3630,4143,12122],""symbol"":null}}");
        }

        private void Connection_StateChanged(StateChange obj)
        {
            Debug.WriteLine($"{obj.OldState}->{obj.NewState}");
            _onConnectionStateChanged?.Invoke(obj.NewState);

            if (obj.NewState == ConnectionState.Connected)
                _metrics.Connected();
        }

        private void Connection_Received(string message)
        {
            Debug.WriteLine(message);

            if (message.Contains(@"""status"":1, ""clientStateResult"":1"))
            {
                _metrics.ClientStateUpdate();
                _onCsmUpdate?.Invoke(_metrics.SamplesCount);
            }

            if (message.Contains(@"""action"":""ack"""))
                _connection.Send(@"{""ack"":1}");
        }

        public Metrics StopAndCollectMetrics()
        {
            _connection.Stop();
            return _metrics;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
