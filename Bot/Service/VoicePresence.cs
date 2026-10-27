namespace Lunaris2.Service;

public readonly record struct VoiceMember(ulong Id, bool IsBot);

public static class VoicePresence
{
    public static bool ShouldLeave(IEnumerable<VoiceMember> members, ulong botId)
    {
        var botPresent = false;
        var humans = 0;

        foreach (var member in members)
        {
            if (member.Id == botId)
            {
                botPresent = true;
                continue;
            }

            if (!member.IsBot)
                humans++;
        }

        return botPresent && humans == 0;
    }
}
