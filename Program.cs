using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crawler
{
    class Program
    {
        public static readonly string PageUrl;
        public static readonly string SectionTagType;
        public static readonly string SectionId;
        public static readonly int NumberOfWords;
        public static readonly string[] ExcludeWords;

        static Program()
        {
            var appSettings = ConfigurationManager.AppSettings;

            PageUrl = appSettings["PageUrl"] ?? "";
            SectionTagType = appSettings["SectionTagType"] ?? "h2";
            SectionId = appSettings["SectionId"] ?? "hitory";
            NumberOfWords = Convert.ToInt32(appSettings["NumberOfWords"] ?? "10");
            ExcludeWords = appSettings["ExcludeWords"] ?.Split(',');
            Console.WriteLine("Configs loaded..");
        }
        static void Main(string[] args)
        {
            var content = GetHtmlSectionContent();
            //var content = "Childhood friends Bill Gates and Paul Allen sought to make a business utilizing their shared skills in computer programming.[16] In 1972, they founded Traf-O-Data which sold a rudimentary computer to track and analyze automobile traffic data. Gates enrolled at Harvard while Allen pursued a degree in computer science at Washington State University, though he later dropped out of school to work at Honeywell.[17] The January 1975 issue of Popular Electronics featured Micro Instrumentation and Telemetry Systems's (MITS) Altair 8800 microcomputer,[18] which inspired Allen to suggest that they could program a BASIC interpreter for the device. Gates called MITS and claimed that he had a working interpreter, and MITS requested a demonstration. Allen worked on a simulator for the Altair while Gates developed the interpreter, and it worked flawlessly when they demonstrated it to MITS in March 1975 in Albuquerque, New Mexico. MITS agreed to distribute it, marketing it as Altair BASIC.[15]:108, 112–114 Gates and Allen established Microsoft on April 4, 1975, with Gates as the CEO,[19] and Allen suggested the name \"Micro - Soft\", short for micro-computer software.[20][21] In August 1977, the company formed an agreement with ASCII Magazine in Japan, resulting in its first international office of \"ASCII Microsoft\".[22] Microsoft moved its headquarters to Bellevue, Washington in January 1979.[19]";
            Crawl(content);
        }

        private static string GetHtmlSectionContent()
        {
            var client = new WebClient();
            string html = client.DownloadString(PageUrl);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var root = doc.DocumentNode;
            var nodes = root.Descendants()
                .Where(n => n.Name.Equals("body"))
                .Single()
                .Descendants();
            var content = new StringBuilder();
            var sectionStarted = false;
            foreach (var node in nodes)
            {
                if (sectionStarted && node.Name.ToLower() == SectionTagType.ToLower())
                    break;
                if (sectionStarted && node.NodeType == HtmlNodeType.Text  && (!string.IsNullOrWhiteSpace(node.InnerText)))
                {
                    content.AppendLine(node.InnerText); 
                }

                if (node.GetAttributeValue("id", "").Equals(SectionId))
                    sectionStarted = true;                
            }
            
            return content.ToString();
        }

        private static void Crawl(string content)
        {
            var wordsFrequency = new Dictionary<string, int>();
            var tokens = content.Split(new[] { '\n', '\r', ' ', '\t' , ',', '(', '{', '[', ')', '}', ']', '\'', '"', '.', '-'});
            foreach(var t in tokens)
            {
                var token = t.Trim();
                if (string.IsNullOrWhiteSpace(token))
                    continue;
                if (wordsFrequency.ContainsKey(token))
                {
                    wordsFrequency[token] += 1;
                }
                else
                    wordsFrequency[token] = 1;
            }

            Exclude(wordsFrequency);

            var topWords = wordsFrequency.OrderByDescending(m => m.Value).Take(NumberOfWords);
            Console.WriteLine("word # of occurences");
            foreach(var word in topWords)
            {
                Console.WriteLine($"{word.Key} {word.Value}");
            }
        }

        private static void Exclude(Dictionary<string, int> pairs)
        {
            var removeKeys = new List<string>(); 
            foreach (var pair in pairs)
            {
                if (ExcludeWords.Contains(pair.Key))
                    removeKeys.Add(pair.Key);
            }

            foreach(var key in removeKeys)
            {
                pairs.Remove(key);
            }
        }
    }
}
