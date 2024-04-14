namespace Lunaris2.Helper;

public static class Async
{
    public static void Run(Func<Task> task)
    {
        _ = Task.Run(task);
    }
}