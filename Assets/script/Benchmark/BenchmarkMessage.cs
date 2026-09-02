using System;

// Wire format shared by NetworkBenchmark (sender) and BenchmarkResponder (echo target).
// Matches the standard test message spec:
// { "msg_id": 1001, "timestamp": 1725300000, "payload": "ping_test" }
[Serializable]
public class BenchmarkMessage
{
    public int msg_id;
    public long timestamp;   // unix time in milliseconds, when the message was sent
    public string payload;

    public const string PingPayload = "ping_test";
    public const string PongPayload = "pong_test";

    public static long NowMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
