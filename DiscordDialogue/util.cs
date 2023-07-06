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
    public class pointer
    {
        public ulong target = ulong.MaxValue;
        public ulong wheight = ulong.MinValue;
        public pointer(ulong trg, ulong whgt = ulong.MinValue)
        {
            this.wheight = whgt;
            this.target = trg;
        }
    }
    public class word
    {
        public string text = "";
        public ulong unique = ulong.MaxValue;
        public List<pointer> pointers = new List<pointer>();
        public int type = 0;
        public int wheight = 0;
        // 0 normal; 1 beginning; 2 end
        public word(string text, ulong unique)
        {
            this.text = text;
            this.unique = unique;
        }
        public void addPointer(ulong target)
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
            this.wheight = this.pointers.Count;
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
            try
            {
                this.targetChannel = ulong.Parse(File.ReadAllText($@"data\guilds\{sckgld.Id}\channel.txt"));
            }
            catch
            {
                this.targetChannel = ulong.MaxValue;
            }
            this.useGlobal = bool.Parse(File.ReadAllText($@"data\guilds\{sckgld.Id}\useGlobal.txt"));
            this.bannedWords = File.ReadAllLines($@"data\guilds\{sckgld.Id}\bannedWords.txt");
        }
        public void Append()
        {
            File.WriteAllText($@"data\guilds\{this._guild.Id}\channel.txt", this.targetChannel.ToString());
            File.WriteAllText($@"data\guilds\{this._guild.Id}\useGlobal.txt", this.useGlobal.ToString());
            File.WriteAllLines($@"data\guilds\{this._guild.Id}\bannedWords.txt", this.bannedWords);
        }
    }
    #endregion
    public static class util
    {
        public static Font FindFont(
           System.Drawing.Graphics g,
           string longString,
           Size Room,
           Font PreferedFont)
        {
            // you should perform some scale functions!!!
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
            char[] delimitors = { ' ', '-' };
            List<word> words = new List<word>();
            UniqueULongGenerator keyGen = new UniqueULongGenerator(ulong.MaxValue);

            foreach (string line in data)
            {
                foreach (string _word in line.Split(delimitors))
                {
                    words.Add(new word(_word, keyGen.Next()));
                }
            }

            var duplicates = words
                .GroupBy(item => item.text)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group.Skip(1))
                .ToList();
            foreach (var duplicate in duplicates)
            { words.Remove(duplicate); }

            foreach (string line in data)
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
                            words.Find(item => item.text == curWord).type = 1;
                            words.Find(item => item.text == curWord).addPointer(words.Find(item => item.text == nextWord).unique);
                        }
                        else if (i == maxInd)
                        {
                            words.Find(item => item.text == curWord).type = 2;
                        }
                    }
                }
            }

            foreach (var word in words)
            {
                if (word.type == 1)
                {
                    string str = $"[{word.text}({word.unique})~";
                    foreach (pointer pntr in word.pointers)
                    {
                        str += $">{pntr.target}^{pntr.wheight}";
                    }
                    str += "]";
                    output.Add(str);
                }
                if (word.type == 0)
                {
                    string str = $"[{word.text}({word.unique})";
                    foreach (pointer pntr in word.pointers)
                    {
                        str += $">{pntr.target}^{pntr.wheight}";
                    }
                    str += "]";
                    output.Add(str);
                }
                if (word.type == 2)
                {
                    string str = $"[{word.text}({word.unique})<]";
                    output.Add(str);
                }
            }
            return output.ToArray();
        }
        public static word[] train2(string[] data)
        {
            char[] delimitors = { ' ', '-' };
            List<word> words = new List<word>();
            UniqueULongGenerator keyGen = new UniqueULongGenerator(ulong.MaxValue);
            foreach (string line in data)
            {
                foreach (string _word in line.Split(delimitors))
                {
                    words.Add(new word(_word, keyGen.Next()));
                }
            }
            var duplicates = words
                .GroupBy(item => item.text)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group.Skip(1))
                .ToList();
            foreach (var duplicate in duplicates)
            { words.Remove(duplicate); }
            foreach (string line in data)
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
                            words.Find(item => item.text == curWord).type = 1;
                            words.Find(item => item.text == curWord).addPointer(words.Find(item => item.text == nextWord).unique);
                        }
                        else if (i == maxInd)
                        {
                            words.Find(item => item.text == curWord).type = 2;
                        }
                    }
                }
            }
            return words.ToArray();
        }
        public static word[] serialize(string[] data)
        {
            List<word> output = new List<word>();

            foreach (string line in data)
            {
                bool isReading = false;
                bool isUnique = false;
                bool isWheight = false;
                bool isPointer = false;

                int type = -1;

                string unique = "";
                List<string> pointerUnique = new List<string>();
                List<string> pointerWheight = new List<string>();

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];

                    if (c == '<')
                    { type = 2; break; }
                    else if (c == '~')
                    { type = 1; continue; }
                    else if (c == '[')
                    {isReading = true;}
                    else if (c == ']')
                    {isReading = false; }
                    else if (c == '(')
                    { isUnique = true; isReading = false; }
                    else if (c == ')')
                    { isUnique = false; isReading = false; }
                    else if (c == '>')
                    { isPointer = true; isReading = false; }
                    else if (c == '^')
                    { isWheight = true; isPointer = false; }
                    else
                    {

                    }
                }
            }

            return output.ToArray();
        }
    }
}
