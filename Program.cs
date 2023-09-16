namespace ChessEngine;
public static class Program
{
    public static void Main(string[] args)
    {
        EngineUCI engine = new EngineUCI();
        string message = string.Empty;

        while (message != "quit")
        {
            message = Console.ReadLine();
            engine.ReceivedMessage(message);
        }
    }
}
