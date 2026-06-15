namespace HexWar.LoadTests;

public class Program
{
    public static async Task Main(string[] args)
    {
        var serverUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
        var gameCount = args.Length > 1 ? int.Parse(args[1]) : 10;
        var duration = args.Length > 2 ? int.Parse(args[2]) : 60;

        var test = new WebSocketLoadTests(serverUrl, gameCount, duration);
        await test.RunAsync();
    }
}