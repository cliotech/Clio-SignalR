// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Connections
{
    public class SomeWorkOnNegotiateConnection : PersistentConnection
    {
        private static readonly ConcurrentDictionary<string, Task<bool>> Tasks = new ConcurrentDictionary<string, Task<bool>>();

        protected override Task OnNegotiate(IRequest request, string connectionId)
        {
            var onNegotiateTask = Task.Delay(1000).ContinueWith(t => true);
            Tasks[connectionId] = onNegotiateTask;
            return onNegotiateTask;
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            return Tasks[connectionId].ContinueWith(t => Connection.Send(connectionId, t.Result));
        }
    }
}
