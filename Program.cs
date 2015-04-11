using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MMCTeam.MT.Analysis.Graphological;
using MMCTeam.MT.Analysis.Lexematic;
using MMCTeam.MT.Analysis.Morphological;
using Nest;
using Languages = MMCTeam.MT.Analysis.Common.Languages;

namespace MMCTeam.MT.Analysis.Syntactical.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Starting analysis...");
            ThreadPool.QueueUserWorkItem(state => DoWork());
            System.Console.ReadKey();
        }

        public static void DoWork()
        {
            try
            {

                string kvaziDictionaryFilePath = @"C:\001\kvazi.txt";
                string specialDictionaryFilePath = @"C:\001\spec.txt";
                string expertDictionaryFilePath = @"C:\001\expert.txt";

                string lexematicResultPath = "Data/result.lexem.txt";
                string morphologicalResultPath = "Data/result.morph.txt";
                string syntacticResultPath = "Data/result.syntac.txt";

                // var graphologicalAnalyzerResult = GraphologicalAnalyzerResult.Load(graphologicalFilePath);

                var client = new Nest.ElasticClient(new Nest.ConnectionSettings(new Uri("http://localhost:9200")));
                //if (!client.IndexExists(x => x.Index("tweet")).Exists)
                //{
                    client.CreateIndex("tweet");
                //}

                /*
                var tweets = new string[]
                {
                    "У Ватикані зараз Великодня служба Божа Святішого Отця Франциска.",
                    "Чим заповнять українські екрани після того, як російські серіали приберуть з ефіру "
                };

                 */

                //foreach (var tweet in tweets)
                string tweet;
                while (FileReader.ReadFile(out tweet, @"C:\001\file.txt"))
                {
                    var rawTweet = new GraphologicalAnalyzerResult
                    {
                        GraphologicalItems = new List<GraphologicalItem> { new GraphologicalItem { Text = tweet, ParagraphType = ParagraphType.Default } }
                    };

                    var lexematicAnalyzer = new LexematicAnalyzer(rawTweet);
                    var lexematicAnalyzerResult = lexematicAnalyzer.Analyze(Languages.Russian);

                    var morphologicalAnalyzer = new MorphologicalAnalyzer(
                        lexematicAnalyzerResult, kvaziDictionaryFilePath, specialDictionaryFilePath, expertDictionaryFilePath);
                    var morphologicalAnalyzerResult = morphologicalAnalyzer.Analyze(Morphological.Languages.Russian);

                    var syntacticalAnalyzer = new SyntacticalAnalyzer(morphologicalAnalyzerResult);
                    var syntacticalAnalyzerResult = syntacticalAnalyzer.Analyze();

                   // var client = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")));
                    var indexResponse = client.Index(
                        new Tweet { Text = tweet, Syntax = syntacticalAnalyzerResult.ToString() },
                        d => d.Index("tweet"));
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
            }
            System.Console.WriteLine("Press any key to exit");
        }
    }

    public class Tweet
    {
        public string Text;
        public string Syntax;
    }

    public class FileReader
    {
        /*
         *  считывает побайтно фаил и возвращает считываемую одну строку. 
         *  запоминает индекс байта и при следующем считыванием возвращает следующую строку
         */

        public static bool EndOfStream = false;
        private string[] textStrings;
       // private static string path = "file.txt";
        private static char divideSymbol = '.';
        // private static string[] notReadingTags;
        private static char[] notReadingSpecSymb;
        private static long index = 0;
        private static bool isInit = false;


        public static void Init()
        {

            notReadingSpecSymb = new char[]
            {
                '\0', '\a', '\b', '\t', '\n', '\v', '\f', '\r'
            };

            isInit = true;
        }

        public static bool ReadFile(out string line, string path)
        {
            if (!isInit)
                Init();

            line = "";
            FileStream file = null;
            byte[] b = new byte[1];
            string symbol = "";

            try
            {
                file = new FileStream(path, FileMode.Open, FileAccess.Read);
                file.Seek(index, SeekOrigin.Begin);
                long lenghtFile = file.Length;

                while (lenghtFile > index)
                {

                    file.Read(b, 0, 1);
                    index++;


                    symbol = Encoding.Default.GetString(b);
                    bool nextSymb = false;
                    foreach (char notReadSmb in notReadingSpecSymb)
                    {
                        if (notReadSmb.ToString() == symbol)
                            nextSymb = true;
                    }
                    if (nextSymb)
                        continue;

                    line += symbol;

                    if (symbol == divideSymbol.ToString())
                    {
                        file.Close();
                        return true;
                    }

                }

                index = 0;
                EndOfStream = true;
            }
            catch (Exception exception)
            {
              //  Console.WriteLine(exception.Message.ToString());
                if (file != null)
                    file.Close();

                return false;
            }

            file.Close();
            return false;
        }
    }
}