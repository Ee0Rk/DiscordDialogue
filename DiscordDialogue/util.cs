using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics.Eventing.Reader;
using System.Net.Sockets;
using Discord.WebSocket;
using System.Drawing;
using System.Dynamic;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace DiscordDialogue
{

    #region classes
    public static partial class StringUtility
    {
        static readonly char[] _WordBreakChars = new char[] { ' ', '_', '\t', '.', '+', '-', '(', ')', '[', ']', '\"', /*'\'',*/ '{', '}', '!', '<', '>', '~', '`', '*', '$', '#', '@', '!', '\\', '/', ':', ';', ',', '?', '^', '%', '&', '|', '\n', '\r', '\v', '\f', '\0' };
        public static string WordWrap(this string text, int width, params char[] wordBreakChars)
        {
            if (string.IsNullOrEmpty(text) || 0 == width || width >= text.Length)
                return text;
            if (null == wordBreakChars || 0 == wordBreakChars.Length)
                wordBreakChars = _WordBreakChars;
            var sb = new StringBuilder();
            var sr = new StringReader(text);
            string line;
            var first = true;
            while (null != (line = sr.ReadLine()))
            {
                var col = 0;
                if (!first)
                {
                    sb.AppendLine();
                    col = 0;
                }
                else
                    first = false;
                var words = line.Split(wordBreakChars);

                for (var i = 0; i < words.Length; i++)
                {
                    var word = words[i];
                    if (0 != i)
                    {
                        sb.Append(" ");
                        ++col;
                    }
                    if (col + word.Length > width)
                    {
                        sb.AppendLine();
                        col = 0;
                    }
                    sb.Append(word);
                    col += word.Length;
                }
            }
            return sb.ToString();
        }
    }
    public class UniqueULongGenerator
    {
        private ulong current;
        private ulong seed;

        public UniqueULongGenerator(ulong seed)
        {
            this.seed = seed;
            current = seed;
        }

        public ulong Next()
        {
            unchecked
            {
                current = current * 6364136223846793005UL + 1442695040888963407UL;
            }
            return current;
        }

        public void Reset()
        {
            current = seed;
        }
    }
    public class UniqueRandomGenerator
    {
        private static Random random = new Random();
        private static HashSet<uint> usedIds = new HashSet<uint>();

        public static uint GenerateUniqueUInt()
        {
            uint randomId;

            do
            {
                randomId = (uint)random.Next();
            } while (usedIds.Contains(randomId));

            usedIds.Add(randomId);
            return randomId;
        }

        // Optional: You can reset the usedIds set if needed
        public static void ResetUsedIds()
        {
            usedIds.Clear();
        }
    }
    public class pointer
    {
        public uint target = uint.MaxValue;
        public ushort wheight = ushort.MinValue;
        public pointer(uint trg, ushort whgt = ushort.MinValue)
        {
            this.wheight = whgt;
            this.target = trg;
        }
    }
    public class word
    {
        public string text = "";
        public uint unique = uint.MaxValue;
        public List<pointer> pointers = new List<pointer>();
        public byte type = 0;
        public ushort wheight = 0;
        // 0 normal; 1 beginning; 2 end
        public word(string text, uint unique)
        {
            this.text = text;
            this.unique = unique;
        }
        public void addPointer(uint target)
        {
            int exists = -1;
            int ind = 0;
            foreach (pointer p in this.pointers)
            {
                if (p.target == target)
                {
                    exists = ind;
                }
                ind++;
            }
            if (exists == -1)
            {
                this.pointers.Add(new pointer(target, 1));
            }
            else
            {
                this.pointers[exists].wheight++;
            }
            this.wheight = (ushort)this.pointers.Count;
        }
        public override string ToString()
        {
            string str = $"{{{this.text}, {this.unique}, {this.type}}}";
            foreach (pointer pntr in this.pointers)
            {
                str += $"\n{{{pntr.target}, {pntr.wheight}}}";
            }
            return str;
        }
    }
    public class guild
    {
        public SocketGuild _guild;
        public ulong targetChannel;
        public bool useGlobal;
        public string[] bannedWords;
        public guild(SocketGuild sckgld)
        {
            this._guild = sckgld;
        }
        public void Initialize(SocketGuild sckgld)
        {
            this._guild = sckgld;
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
            string template = File.ReadAllText($@"data\cfgTemplate.yaml");
            dynamic cfg;
            dynamic _cfg = deserializer.Deserialize<ExpandoObject>(template);
            try
            {
                cfg = deserializer.Deserialize<ExpandoObject>(File.ReadAllText($@"data\guilds\{sckgld.Id}\properties.yaml"));
            }
            catch
            {
                File.WriteAllText($@"data\guilds\{sckgld.Id}\properties.yaml", template);
                cfg = deserializer.Deserialize<ExpandoObject>(template);
            }
            try
            {
                this.targetChannel = ulong.Parse(cfg.channel);
                this.useGlobal = bool.Parse(cfg.use_global);
                this.bannedWords = ((List<object>)cfg.banned_words).OfType<string>().ToArray();
            }
            catch (Exception ex)
            {
                this.targetChannel = ulong.Parse(_cfg.channel);
                this.useGlobal = bool.Parse(_cfg.use_global);
                this.bannedWords = ((List<object>)_cfg.banned_words).OfType<string>().ToArray();
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }
        public void Append()
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            dynamic cfg = deserializer.Deserialize<ExpandoObject>(File.ReadAllText($@"data\guilds\{this._guild.Id}\properties.yaml"));

            cfg.channel = this.targetChannel.ToString();
            cfg.use_global = this.useGlobal.ToString();
            cfg.banned_words = this.bannedWords;

            var serializer = new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
            string modifiedYaml = serializer.Serialize(cfg);
            File.WriteAllText($@"data\guilds\{this._guild.Id}\properties.yaml", modifiedYaml);
        }
    }
    #endregion
    public static class util
    {
        #region vars
        static Regex sbc = new Regex("\\[[^\\]]*\\]", RegexOptions.IgnoreCase); // square brackets content
        static Regex pc = new Regex("\\([^)]*\\)", RegexOptions.IgnoreCase); // parentheses content

        static char[] nums = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        #endregion
        public static double ConvertBytesToMegabytes(long bytes)
        {
            return Math.Round((bytes / 1024f) / 1024f);
        }
        public static void DrawWrappedAndScaledText(Graphics graphics, string text, Font font, Brush brush, RectangleF area)
        {
            StringFormat format = new StringFormat();
            format.Trimming = StringTrimming.Word;
            format.FormatFlags = StringFormatFlags.LineLimit;

            SizeF textSize = graphics.MeasureString(text, font, new SizeF(area.Width, area.Height), format);

            float scaleX = area.Width / textSize.Width;
            float scaleY = area.Height / textSize.Height;
            float scale = Math.Min(scaleX, scaleY);

            Font scaledFont = new Font(font.FontFamily, font.Size * scale);

            RectangleF scaledArea = new RectangleF(area.Location, new SizeF(area.Width / scale, area.Height / scale));

            Font a;
            float i = scaledFont.Size;
            SizeF _ts = graphics.MeasureString(text, scaledFont);
            if (_ts.Width >= area.Width || _ts.Height >= area.Height)
            {
                while (true)
                {
                    Console.WriteLine("subtract");
                    i -= 0.5f;
                    a = new Font(font.FontFamily, i);
                    SizeF ts = graphics.MeasureString(text, a);
                    if (ts.Width <= area.Width || ts.Height <= area.Height) break;
                }
            }
            else
            {
                while (true)
                {
                    Console.WriteLine("add");
                    i += 0.5f;
                    a = new Font(font.FontFamily, i);
                    SizeF ts = graphics.MeasureString(text, a);
                    if (ts.Width >= area.Width || ts.Height >= area.Height) break;
                }
            }
            
            graphics.DrawString(text, a, brush, scaledArea, format);
        }
        public static Font FindFont(Graphics g,string longString,Size Room,Font PreferedFont)
        {
            SizeF RealSize = g.MeasureString(longString, PreferedFont);
            float HeightScaleRatio = Room.Height / RealSize.Height;
            float WidthScaleRatio = Room.Width / RealSize.Width;

            float ScaleRatio = (HeightScaleRatio < WidthScaleRatio)
               ? ScaleRatio = HeightScaleRatio
               : ScaleRatio = WidthScaleRatio;
            float ScaleFontSize = PreferedFont.Size * ScaleRatio;

            return new Font(PreferedFont.FontFamily, ScaleFontSize);
        }
        public static string[] train(string[] data/*, string[] replace*/)
        {
            List<string> output = new List<string>();
            word[] words = train2(data);
            foreach (var word in words)
            {
                if (word.type == 1)
                {
                    string str = $"[{word.text}({word.unique}^{word.wheight})~";
                    foreach (pointer pntr in word.pointers)
                    {
                        str += $">{pntr.target}^{pntr.wheight}";
                    }
                    str += "]";
                    output.Add(str);
                }
                if (word.type == 0)
                {
                    string str = $"[{word.text}({word.unique}^{word.wheight})";
                    foreach (pointer pntr in word.pointers)
                    {
                        str += $">{pntr.target}^{pntr.wheight}";
                    }
                    str += "]";
                    output.Add(str);
                }
                if (word.type == 2)
                {
                    string str = $"[{word.text}({word.unique}^{word.wheight})<]";
                    output.Add(str);
                }
            }
            return output.ToArray();
        }
        public static word[] train2(string[] data)
        {
            char[] delimitors = ((List<object>)Program.cfg.delimitors).OfType<char>().ToArray();
            List<word> words = new List<word>();
            uint indice = 0;
            uint _line = 0;
            foreach (string line in data)
            {
                foreach (string _word in line.Split(delimitors))
                {
                    words.Add(new word(_word, indice));
                    indice++;
                    Console.CursorLeft = 0;
                    Console.CursorTop = 0;
                    Console.Write($"Adding words I{indice} | L{_line} | A{data.Length} ");
                }
                _line++;
            }

            Console.Write("Removing duplicates ");
            words = words.GroupBy(item => item.text)
                         .SelectMany(group => group.Distinct())
                         .ToList();


            _line = 0;
            /*foreach (string line in data)
            {
                string[] _w = line.Split(delimitors);
                if (_w.Length > 3)
                {
                    int maxInd = _w.Length - 1;
                    for (int i = 0; i < _w.Length; i++)
                    {
                        string curWord = _w[i];
                        string nextWord = null;
                        if (i != maxInd)
                        {
                            nextWord = _w[i + 1];
                        }
                        if (i == 0)
                        {
                            words.Find(item => item.text == curWord).type = 1;
                            words.Find(item => item.text == nextWord).type = 0;
                            words.Find(item => item.text == curWord).addPointer(words.Find(item => item.text == nextWord).unique);
                        }
                        else if (i != 0 & i != maxInd)
                        {
                            words.Find(item => item.text == curWord).type = 0;
                            words.Find(item => item.text == curWord).addPointer(words.Find(item => item.text == nextWord).unique);
                        }
                        else if (i == maxInd)
                        {
                            words.Find(item => item.text == curWord).type = 2;
                        }
                        Console.CursorLeft = 0;
                        Console.CursorTop = 0;
                        Console.Write($"Deserializing I{i} | L{_line} | A{data.Length} ");
                    }
                }
                _line++;
            }*/
            Dictionary<string, word> wordDictionary = words.ToDictionary(item => item.text);
            foreach (string line in data)
            {
                string[] _w = line.Split(delimitors);
                if (_w.Length > 3)
                {
                    int maxInd = _w.Length - 1;
                    for (int i = 0; i < _w.Length; i++)
                    {
                        string curWord = _w[i];
                        string nextWord = (i != maxInd) ? _w[i + 1] : null;

                        word curWordObj = wordDictionary[curWord];
                        curWordObj.type = (i == (byte)0) ? (byte)1 : (i == maxInd) ? (byte)2 : (byte)0;

                        if (nextWord != null)
                        {
                            word nextWordObj = wordDictionary[nextWord];
                            curWordObj.addPointer(nextWordObj.unique);
                        }

                        Console.CursorLeft = 0;
                        Console.CursorTop = 0;
                        Console.Write($"Deserializing I{i} | L{_line} | A{data.Length} ");
                    }
                }
                _line++;
            }
            return words.ToArray();
        }
        public static word[] serialize(string[] data)
        {
            List<word> output = new List<word>();

            foreach (string line in data)
            {
                string content = sbc.Match(line).Value;
                string[] a = pc.Match(content).Value.Split('^');
                uint wordUnique = uint.Parse(a[0]);
                ushort wordWheight = ushort.Parse(a[1]);

                // walking the word
                string concat = "";
                int ind = 0;
                foreach (char c in content)
                {
                    if (c == '(')
                    {
                        break;
                    }
                    else
                    {
                        concat += c;
                    }
                    ind++;
                }
                word curWord = new word(concat, wordUnique);
                // concat = "";

                // walking the pointers
                string content2 = content.Replace($"({wordUnique}^{wordWheight})", "");
                bool isPointer = false;
                string curPointer = "";
                string curWheight = "";
                int pointerDepth = 0;
                List<pointer> pointers = new List<pointer>();
                
                for (int i = ind - 1; i < content2.Length; i++)
                {
                    char c = content2[i];
                    if (i == ind - 1)
                    {
                        if (c == '<')
                        {
                            curWord.type = 2;
                            break;
                        }
                        else if (c == '~')
                        {
                            curWord.type = 1;
                        }
                        else
                        {
                            curWord.type = 0;
                        }
                    }
                    else
                    {
                        if (c == '>')
                        {
                            isPointer = true;
                            if (pointerDepth > 0)
                            {
                                pointers.Add(new pointer(uint.Parse(curPointer), ushort.Parse(curWheight)));
                                curPointer = "";
                                curWheight = "";
                                pointerDepth++;
                            }
                        }
                        else if (c == '^')
                        {
                            isPointer = false;
                        }
                        else if (nums.Contains(c))
                        {
                            if (isPointer)
                            {
                                curPointer += c;
                            }
                            else
                            {
                                curWheight += c;
                            }
                        }
                    }
                }
            }

            return output.ToArray();
        }
    }
}
