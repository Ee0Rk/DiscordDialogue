using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;

// 8-15 words per sentence
// <@1124775606157058098>

namespace DiscordDialogue
{
    internal class Program
    {
        string dataLocation = "";

        private DiscordSocketClient _client;
        private CommandService _commands;
        Random rand = new Random(69420);

        public static void Main(string[] args)
            => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;

            await _client.LoginAsync(TokenType.Bot, File.ReadAllText(@"data\token.txt"));
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task MessageReceived(SocketMessage msg)
        {
            if (msg.Content.Contains("<@1124775606157058098>"))
            {
                #region god help me for what comes next
                Stopwatch stopw = new Stopwatch();
                stopw.Start();
                var message = msg as SocketUserMessage;
                string finishedMessage = "";


                stopw.Stop();
                finishedMessage += $"\n||Generated in: {stopw.ElapsedMilliseconds}MS||";
                message.ReplyAsync(finishedMessage);
                #endregion
            }
            return Task.CompletedTask;
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}
