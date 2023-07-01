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
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;

namespace DiscordDialogue
{
    internal class Program
    {
        string dataLocation = "";

        private DiscordSocketClient _client;
        private CommandService _commands;

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

        private Task MessageReceived()
        {
            return Task.CompletedTask;
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}
