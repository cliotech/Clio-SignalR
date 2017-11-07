// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class TestConnection : PersistentConnection
    {
        internal static ConnectionBehavior Behavior { get; set; }

        protected override Task OnNegotiate(IRequest request, string connectionId)
        {
            return base.OnNegotiate(request, connectionId).ContinueWith(t => Debug.WriteLine($"OnNegotiate connectionId:{connectionId}"));
        }

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            return Connection.Send(connectionId, "OnConnected").ContinueWith(t => HeartBeat(connectionId));
        }

        private Task HeartBeat(string connectionId)
        {
            return Task.Delay(3000).ContinueWith(x =>
            {
                Connection.Send(connectionId, $"HeartBeat:{DateTime.UtcNow}");
            }).ContinueWith(t => HeartBeat(connectionId));
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            if (Behavior == ConnectionBehavior.Echo)
            {
                Connection.Send(connectionId, data);
            }
            else if (Behavior == ConnectionBehavior.Broadcast)
            {
                Connection.Broadcast(data);
            }
            return Task.FromResult<object>(null);
        }
    }

    public enum ConnectionBehavior
    {
        ListenOnly,
        Echo,
        Broadcast
    }
}