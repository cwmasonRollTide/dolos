﻿using MediatR;
using Amazon.SQS;
using Amazon.SQS.Model;
using DolosTranscriptParser.Commands.ParseTranscript;
using DolosTranscriptParser.Commands.SavePrompts;

namespace DolosTranscriptParser.Services.Background;

public class SqsPollingService : BackgroundService
{
    private ISender _mediator;

    public SqsPollingService(ISender mediator)
    {
        _mediator = mediator;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = new AmazonSQSClient();

        while (!stoppingToken.IsCancellationRequested)
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL"),
                MaxNumberOfMessages = 5
            };
            ReceiveMessageResponse? response = await client.ReceiveMessageAsync(request, stoppingToken);
            foreach(Message? message in response.Messages)
            {
                Console.WriteLine("New message received, url:");
                Console.WriteLine(message.Body);
                var promptTokens = await _mediator.Send(new ParseTranscriptRequest
                {
                    TranscriptUrl = message.Body
                }, stoppingToken);
                Console.WriteLine($"Transcript processed successfully for url: {message.Body}");
                var saveToStorage = await _mediator.Send(new SavePromptsRequest
                {
                    Guest = promptTokens.Guest,
                    Prompts = promptTokens.Prompts
                });
                if (saveToStorage.Success) Console.WriteLine($"Successfully saved interview prompt and completions");
            }
        }
    }
}