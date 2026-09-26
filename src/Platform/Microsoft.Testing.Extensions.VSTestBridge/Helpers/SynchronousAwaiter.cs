// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Testing.Extensions.VSTestBridge.Helpers;

/// <summary>
/// A helper class to provide synchronous awaiter. Because of vstest sync APIs we need to wait synchronously.
/// </summary>
internal static class SynchronousAwaiter
{
    // busyWait defaults to false: every awaited call site publishes to IMessageBus.PublishAsync (or the
    // attachment-publishing loop), whose entire call chain (AsynchronousMessageBus, AsyncConsumerDataProcessor/
    // BlockingConsumerDataProcessor, MessageBusProxy) is ConfigureAwait(false) end to end and never captures a
    // SynchronizationContext, so blocking on GetAwaiter().GetResult() cannot deadlock here. Spinning via
    // SpinWait instead burns a full CPU core for the whole wait duration (measured ~2-3x more CPU time than a
    // blocking wait once the awaited task takes any real time to complete, e.g. background report-writer I/O
    // or GC pauses under load), which is pure wasted energy on a path that runs once per recorded test result.
    public static void Await(this Task valueTask, bool busyWait = false)
    {
        if (busyWait)
        {
            var spin = default(SpinWait);
            while (!valueTask.IsCompleted)
            {
                spin.SpinOnce();
            }

            // We want to observe the exception
            valueTask.GetAwaiter().GetResult();
        }
        else
        {
            valueTask.GetAwaiter().GetResult();
        }
    }
}
