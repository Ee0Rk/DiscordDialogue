using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using HtmlAgilityPack;

//----------------------------------------|
// Placement order should be same as shown|
// Start of sentance: ~                   |
// End of sentance:   <                   |
// Index pointer:     >                   |
//----------------------------------------|
// Example:        0  Hello~>1            |
//                 1  there>2             |
//                 2  world!<             |
//----------------------------------------|

#pragma warning disable CS8602
#pragma warning disable CS8600

namespace DiscordDialogoueTrainer
{
    #region classes
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
        // 0 normal; 1 beginning; 2 end
        public word (string text, ulong unique)
        {
            this.text = text;
            this.unique = unique;
        }
        public void addPointer(ulong target)
        {
            int exists = -1;
            int ind = 0;
            foreach(pointer p in this.pointers)
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
    #endregion

    internal class Program
    {
        #region variables
        static char[] delimitors = { ' ', '-' };

        static string[] banWords;
        static string[] inputLines = new string[1];
        static List<string> outputLines = new List<string>();

        public static List<word> words = new List<word>();

        public static UniqueULongGenerator keyGen = new UniqueULongGenerator(ulong.MaxValue);

        public static Stopwatch stopw = new Stopwatch();
        #endregion


        static void Main(string[] args)
        {
            banWords = File.ReadAllLines(@"data\banWords.txt");
            while (true)
            {
                stopw.Start();

                using (WebClient client = new WebClient())
                {
                    //client.Credentials = new NetworkCredential(username, password);
                    client.DownloadFile("https://datastash-ee0rk.pythonanywhere.com/training/daddymartoon", @"input.txt");
                }

                inputLines = File.ReadAllLines("input.txt");
                foreach (string line in inputLines)
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

                foreach (string line in inputLines)
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
                        outputLines.Add(str);
                    }
                    if (word.type == 0)
                    {
                        string str = $"[{word.text}({word.unique})";
                        foreach (pointer pntr in word.pointers)
                        {
                            str += $">{pntr.target}^{pntr.wheight}";
                        }
                        str += "]";
                        outputLines.Add(str);
                    }
                    if (word.type == 2)
                    {
                        string str = $"[{word.text}({word.unique})<]";
                        outputLines.Add(str);
                    }
                }

                //foreach (string line in outputLines)
                //{
                //    Console.WriteLine(line);
                //}

                File.WriteAllLines("output.txt", outputLines.ToArray());
                long length = new FileInfo("output.txt").Length/1000;

                stopw.Stop();
                Console.WriteLine($"\nFinished training:\n{inputLines.Length} Sentances\n{words.Count} Words\nIn: {stopw.ElapsedMilliseconds}MS");
                Console.WriteLine($"\nTrained data size: {length}KB");

                Thread.Sleep(60000);

                outputLines.Clear();
                words.Clear();
                keyGen.Reset();
                stopw.Reset();
                Console.Clear();

                GC.Collect();
            }
        }
    }
}

//----------------------------------------|
// Placement order should be same as shown|
// Start of sentance: ~                   |
// End of sentance:   <                   |
// Index pointer:     >                   |
// Pointer wheight:   ^                   |
//----------------------------------------|