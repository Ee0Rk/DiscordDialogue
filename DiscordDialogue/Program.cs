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
using System.Drawing;
using System.Net;
using Discord.Rest;
using System.Drawing.Text;
using System.Collections.ObjectModel;
using System.Dynamic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Runtime.Remoting.Messaging;
// 8-15 words per sentence
// <@1124775606157058098>
namespace DiscordDialogue
{
    internal class Program
    {
        #region var
        private DiscordSocketClient _client;
        private CommandService _commands;
        Random rand = new Random(69420);

        public volatile List<guild> guilds = new List<guild>();

        List<KeyValuePair<string,string>> replacements = new List<KeyValuePair<string, string>>();
        string[] bannedWords;
        string[] quotes;
        char[] delimitors;
        Bitmap portrait;
        Bitmap silhouette;
        string root;
        FontFamily roboto;
        List<ulong> devs = new List<ulong>();
        string gldCfgTemplate;

        dynamic cfg;
        #endregion

        public static void Main(string[] args)
            => new Program().RunBotAsync().GetAwaiter().GetResult();
        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            });
            _commands = new CommandService();

            #region cache
            Stopwatch stopw = Stopwatch.StartNew();
            Console.WriteLine("Caching...");
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            cfg = deserializer.Deserialize<ExpandoObject>(File.ReadAllText("data\\config.yaml"));

            silhouette = new Bitmap(cfg.assets["pictures"]["silhouette"]);
            portrait = new Bitmap(cfg.assets["pictures"]["portrait"]);
            quotes = File.ReadAllLines(cfg.assets["misc"]["quotes"]);
            delimitors = ((List<object>)cfg.delimitors).OfType<string>().ToArray().SelectMany(str => str).ToArray();
            PrivateFontCollection collection = new PrivateFontCollection();
            collection.AddFontFile(cfg.assets["fonts"]["thin"]);
            roboto = new FontFamily("Roboto", collection);
            bannedWords = ((List<object>)cfg.bannedWords).OfType<string>().ToArray();
            root = Directory.GetParent(Application.ExecutablePath).ToString();
            gldCfgTemplate = File.ReadAllText(cfg.assets["misc"]["gldCfgTemplate"]);
            Directory.CreateDirectory("data\\tmp");

            foreach (string d in ((List<object>)cfg.developer["developers"]).OfType<string>().ToArray())devs.Add(ulong.Parse(d));
            foreach (string b in ((List<object>)cfg.replace).OfType<string>().ToArray())
            {
                string[] c = b.Split('§');
                replacements.Add( new KeyValuePair<string, string>(c[0], c[1].Replace("½", "")));
            }

            Console.WriteLine($"Cached! {stopw.ElapsedMilliseconds}MS {util.ConvertBytesToMegabytes(Process.GetCurrentProcess().PrivateMemorySize64)}MB");
            GC.Collect();
            stopw.Stop();
            #endregion

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
            _client.GuildAvailable += GuildFound;
            await _client.LoginAsync(TokenType.Bot, File.ReadAllText(cfg.tokens["discord"]));
            await _client.StartAsync();
            await Task.Delay(-1);
        }
        private Task MessageReceived(SocketMessage msg)
        {
            #region setup
            Stopwatch st = Stopwatch.StartNew();
            var message = msg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            SocketGuild guild = context.Guild;
            guild _guild = guilds.Find(item => item._guild.Id == guild.Id);
            Process proc = Process.GetCurrentProcess();
            string nickname = "";
            if (_guild == null) Console.WriteLine("Guild was null");
            string[] coms = msg.Content.Split(' ');
            #endregion
            #region check if eligable
            bool isEligable = true;
            bool isEligableLocal = true;
            foreach (string banWord in bannedWords)
            {
                if (msg.Content.ToLower().Contains(banWord.ToLower()))
                {
                    isEligable = false;
                    isEligableLocal = false;
                }
            }
            if (msg.Content.Split(delimitors).Length < 3)
            {
                isEligable = false;
                isEligableLocal = false;
            }
            if (isEligable)
            {
                using (StreamWriter sw = File.AppendText(@"data\global\globalSentances.txt"))
                {
                    sw.WriteLine(message.CleanContent);
                }
            }
            if (isEligableLocal)
            {
                using (StreamWriter sw = File.AppendText($@"data\guilds\{guild.Id}\sentances.txt"))
                {
                    sw.WriteLine(message.CleanContent);
                }
            }
            #endregion

            if (message.Author is SocketGuildUser guildUser)
            {
                var botUser = guildUser.Guild.CurrentUser;
                nickname = botUser.Nickname;
            }
            if (msg.Author.IsBot) return Task.CompletedTask;
            if (coms[0] == "!train")
            {
                Stopwatch stopw = new Stopwatch();
                stopw.Start();
                IUserMessage _msg = message.ReplyAsync("Training...").Result;
                try
                {
                    Uri uriResult;
                    bool result = Uri.TryCreate(msg.Content.Split(' ')[1], UriKind.Absolute, out uriResult)
                        && uriResult.Scheme == Uri.UriSchemeHttp;
                    if (!result)
                    {
                        using (WebClient client = new WebClient())
                        {
                            //client.Credentials = new NetworkCredential(username, password);
                            client.DownloadFile(msg.Content.Split(' ')[1], $@"data\guilds\{context.Guild.Id}\sentances.txt");
                        }
                        long length = new FileInfo($@"data\guilds\{context.Guild.Id}\sentances.txt").Length / 1000;
                        string[] data = util.train(File.ReadAllLines($@"data\guilds\{context.Guild.Id}\sentances.txt"));
                        File.WriteAllLines($@"data\guilds\{context.Guild.Id}\dataset.txt", data);
                        long length2 = new FileInfo($@"data\guilds\{context.Guild.Id}\dataset.txt").Length / 1000;
                        _msg.ModifyAsync(__msg => __msg.Content = $"Success. {length}KB in {stopw.ElapsedMilliseconds}MS to {length2}KB");
                        //message.ReplyAsync($"Success. {length}KB in {stopw.ElapsedMilliseconds}MS to {length2}KB");
                    }
                    else
                    {
                        message.ReplyAsync($"Invalid url. \"{msg.Content.Split(' ')[1]}\"");
                    }
                }
                catch (Exception ex)
                {
                    message.ReplyAsync($"Invalid url. {ex.Message}");
                }
                stopw.Stop();
            }
            if (coms[0] == "!setchannel")
            {
                _guild.targetChannel = msg.Channel.Id;
                _guild.Append();
                message.ReplyAsync($"Channel set to: #{msg.Channel.Name} ({msg.Channel.Id})");
            }
            if (coms[0] == "!quote")
            {
                string quote = quotes[rand.Next(0, quotes.Length - 1)];
                //string quote = quotes[9];

                Bitmap bmp = new Bitmap(512, 256);
                Graphics g = Graphics.FromImage(bmp);
                Font nickFont = util.FindFont(g, nickname, new Size(180, 180), SystemFonts.DefaultFont);

                g.DrawImage(portrait, 0, 0, 192, 256);
                g.DrawImage(silhouette, 0,0, 512,256);
                g.DrawString(nickname, nickFont, Brushes.White, Point.Empty);
                g.DrawString(quote, new Font(roboto, 10, FontStyle.Regular), Brushes.White, layoutRectangle: new RectangleF(190, 0, 300, 256));

                bmp.Save($@"{root}\data\tmp\{msg.Id}.png");
                context.Channel.SendFileAsync($@"{root}\data\tmp\{msg.Id}.png", msg.Author.Mention,false,messageReference:message.Reference).Wait();
                File.Delete($@"{root}\data\tmp\{msg.Id}.png");
            }
            if (coms[0] == "!useglobal")
            {
                _guild.useGlobal = !_guild.useGlobal;
                _guild.Append();
                message.ReplyAsync($"Global dataset set to: {_guild.useGlobal}");
            }
            if (coms[0] == "!video")
            {
                string[] videos = ((List<object>)cfg.assets["videos"]).OfType<string>().ToArray();
                string video = videos[rand.Next(0, videos.Length - 1)];
                context.Channel.SendFileAsync(video, msg.Author.Mention+" "+Path.GetFileName(video), false, messageReference: message.Reference);
            }
            if (coms[0] == "<@1124775606157058098>")
            {
                #region god help me for what comes next
                Stopwatch stopw = new Stopwatch();
                stopw.Start();  
                string finishedMessage = "";
                EmbedBuilder eb = new EmbedBuilder();

                finishedMessage += "im a nigger";

                eb.AddField($"Famous words of {nickname}",finishedMessage);
                eb.Description = $"||Generated in: {stopw.ElapsedMilliseconds}MS\nMemory used: {util.ConvertBytesToMegabytes(proc.PrivateMemorySize64)}MB||";
                GC.Collect();
                stopw.Stop();
                message.ReplyAsync(embed:eb.Build());
                #endregion
            }
            #region dev commands
            if (devs.Contains(message.Author.Id))
            {
                if (coms[0] == "!reset")
                {
                    Directory.Delete("data\\guilds", true);
                    Directory.Delete("data\\users", true);
                    Directory.CreateDirectory("data\\guilds");
                    Directory.CreateDirectory("data\\users");
                    foreach (guild gld in guilds)
                    {
                        Directory.CreateDirectory($@"data\guilds\{gld._guild.Id}");
                        File.WriteAllText($@"data\guilds\{gld._guild.Id}\properties.yaml", gldCfgTemplate);
                    }
                }
                if (coms[0] == "!root")
                {
                    /*
                    Directory.Delete("data\\guilds", true);
                    Directory.Delete("data\\users", true);
                    Directory.CreateDirectory("data\\guilds");
                    Directory.CreateDirectory("data\\users");
                    foreach (guild gld in guilds)
                    {
                        Directory.CreateDirectory($@"data\guilds\{gld._guild.Id}");
                        File.WriteAllText($@"data\guilds\{gld._guild.Id}\properties.yaml", gldCfgTemplate);
                    }
                    */
                }
                if (coms[0] == "!dump")
                {
                    var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                    dynamic _cfg = deserializer.Deserialize<ExpandoObject>(File.ReadAllText($@"data\dumpTemplate.yaml"));
                    foreach (SocketCategoryChannel cat in guild.CategoryChannels)
                    {
                        foreach (SocketChannel chn in cat.Channels)
                        {
                        }
                    }
                    foreach (SocketUser usr in guild.Users)
                    {
                    }
                }
            }
            #endregion

            guilds[guilds.FindIndex(item => item._guild == guild)] = _guild;
            GC.Collect();

            Console.WriteLine($"MessageReceived in: {st.ElapsedMilliseconds}MS");
            return Task.CompletedTask;
        }
        private Task GuildFound(SocketGuild guild)
        {
            if (!Directory.Exists(@"data\guilds"))
            {
                Directory.CreateDirectory(@"data\guilds");
            }
            string[] dirs = Directory.GetDirectories(@"data\guilds");
            ulong[] storedGuilds = new ulong[dirs.Length];
            bool isStored = false;

            for (int i = 0; i < dirs.Length; i++)
            {
                ulong unique = ulong.Parse(Path.GetFileName(dirs[i]));
                storedGuilds[i] = unique;
                if (guild.Id == unique) 
                {
                    isStored = true;
                }
            }
            if (isStored == false)
            {
                Directory.CreateDirectory($@"data\guilds\{guild.Id}");
                File.WriteAllText($@"data\guilds\{guild.Id}\properties.yaml", gldCfgTemplate);
                File.CreateText($@"data\guilds\{guild.Id}\sentances.txt").Dispose();
                File.CreateText($@"data\guilds\{guild.Id}\dataset.txt").Dispose();

                guild gld = new guild(guild);
                gld.Initialize(gld._guild);
                guilds.Add(gld);
            }
            else
            {
                guild gld = new guild(guild);
                gld.Initialize(gld._guild);
                guilds.Add(gld);
            }
            return Task.CompletedTask;
        }
    }
}
