namespace Lunaris2.Handler.ChatCommand;

public class ChatSettings
{
    public string Url { get; set; }
    public string Model { get; set; }
    public List<Personality> Personalities { get; set; }
}

public class Personality
{
    public string Name { get; set; }
    public string Instruction { get; set; }
}