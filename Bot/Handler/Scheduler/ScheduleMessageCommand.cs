using System.Globalization;
using System.Text;
using Discord.WebSocket;
using Hangfire;
using Lunaris2.Handler.ChatCommand;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.SlashCommand;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NCrontab;
using OllamaSharp;
using static System.DateTime;

namespace Lunaris2.Handler.Scheduler;

public record ScheduleMessageCommand(SocketSlashCommand Message) : IRequest;

public class ScheduleMessageHandler : IRequestHandler<ScheduleMessageCommand>
{
    private readonly ChatSettings _chatSettings;
    private readonly OllamaApiClient _ollama;
    private readonly ISender _mediator;

    private readonly string _cronInstruction = "You are only able to respond in CRON Format. " +
                                              "Current time is: " + Now.ToString("yyyy-MM-dd HH:mm") + ". and it is " +
                                              Now.DayOfWeek + ". " +
                                              "Please use the a format parsable by ncrontab." +
                                              "The user will describe the CRON format and you can only answer with the CRON format the user describes.";
    
    private readonly string _dateInstruction = "You are only able to respond in Date Format. " +
                                               "Current time is: " + Now.ToString("dd/MM/yyyy HH:mm:ss") + ". and it is " +
                                               Now.DayOfWeek + ". " +
                                               "Please use the following format: dd/MM/yyyy HH:mm:ss. Convert following to date string with the current time as a context";
    
    public ScheduleMessageHandler(
        IOptions<ChatSettings> chatSettings, 
        ISender mediator)
    {
        _mediator = mediator;
        _chatSettings = chatSettings.Value;

        var uri = new Uri(_chatSettings.Url);
        _ollama = new OllamaApiClient(uri)
        {
            SelectedModel = _chatSettings.Model
        };
    }

    public async Task Handle(ScheduleMessageCommand request, CancellationToken cancellationToken)
    {
        var userDateInput = request.Message.GetOptionValueByName<string>(Option.Time);
        var userMessage = request.Message.GetOptionValueByName<string>(Option.Message);
        var recurring = request.Message.GetOptionValueByName<bool>(Option.IsRecurring);

        if (recurring)
        {
            await ScheduleRecurringJob(request, userMessage, userDateInput, cancellationToken);
        }
        else
        {
            await ScheduleJob(request, userMessage, userDateInput, cancellationToken);
            
            await request.Message.Channel.SendMessageAsync("Message scheduled successfully.");
        }
    }

    private async Task ScheduleRecurringJob(
        ScheduleMessageCommand request,
        string message,
        string userDateInput,
        CancellationToken cancellationToken)
    {
        using var setTyping = request.Message.Channel.EnterTypingState();
        var cron = string.Empty;
        var jobManager = new RecurringJobManager();
        const int retries = 5;
        var userMessage = $"{_cronInstruction}: {userDateInput}";
        
        for (var tries = 0; tries < retries; tries++)
        {
            var textToCronResponse = await GenerateResponse(userMessage, cancellationToken);
            var isValid = CrontabSchedule.TryParse(textToCronResponse).ToString().IsNullOrEmpty();
            
            if(isValid)
            {
                await request.Message.Channel.SendMessageAsync("Sorry, I didn't understand that date format. Please try again.");
                continue;
            }
            
            cron = textToCronResponse;
                
            break;
        }
        var recurringJobId = $"channel_{request.Message.ChannelId}_{request.Message.Id}";

        jobManager.AddOrUpdate(
            recurringJobId,
            () => _mediator.Send(new ProcessMessageCommand { Context = request.Message.ChannelId, Content = message}, cancellationToken),
            cron
        );
            
        setTyping.Dispose();
        await request.Message.Channel.SendMessageAsync("Message scheduled successfully.");
    }

    private async Task ScheduleJob(
        ScheduleMessageCommand request,
        string userMessage,
        string executeAt,
        CancellationToken cancellationToken
    )
    {
        var dateFormat = $"{_dateInstruction}: {executeAt}";

        var formattedDate = await GenerateResponse(dateFormat, cancellationToken);
        
        var date = ParseExact(formattedDate, "dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);

        BackgroundJob.Schedule(
            () => _mediator.Send(
                new ProcessMessageCommand { Context = request.Message.ChannelId, Content = userMessage },
                cancellationToken),
            date);
    }

    private async Task<string> GenerateResponse(string userMessage, CancellationToken cancellationToken)
    {
        var response = new StringBuilder();

        var chatContext = _ollama.Chat(stream => response.Append(stream.Message?.Content ?? ""));
        
        await chatContext.Send(userMessage, cancellationToken);
        
        return response.ToString();
    }
}