using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedditSharp;
using System.Security.Authentication;
using System.Text;
using RedditSharp.Things;

namespace Rdt10
{
    class Program
    {
        
        static void Main(string[] args)
        {
            Reddit reddit = null;
            var authenticated = false;
            
            while (!authenticated)
            {
                Console.Write("Enter your reddit username: ");
                var username = Console.ReadLine();
                Console.Write("Enter your reddit password: ");
                var password = ReadPassword();

                try
                {
                    Console.WriteLine("Logging in...");
                    reddit = new Reddit(username, password);
                    authenticated = reddit.User != null;
                }
                catch (AuthenticationException)
                {
                    Console.WriteLine("Incorrect login. Try again.");
                    authenticated = false;
                }
            }

            try
            {
                PersistSaved(reddit);
            }
            catch (Exception e)
            {
                Console.WriteLine("There was a problem: ");
                Console.WriteLine(e.Message);
                if(e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                }
                Console.ReadLine();
            }
        }

        private static void PersistSaved(Reddit reddit)
        {
            var output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RedditSaved.csv");

            Console.WriteLine("Retreiving saved items. This may take several minutes if you have a lot of saved stuff.");
            var savedList = reddit.User.GetSaved();

            var failedEntries = new List<VotableThing>();
            using (var writer = new StreamWriter(output, append:false))
            {
                writer.WriteLine("Subreddit,Upvotes,Content,Comments URL,Content URL,Date posted");
                var i = 1;

                foreach (var post in savedList)
                {
                    try
                    {
                        Console.WriteLine($"Backing up saved item {i}...");
                        if (post.GetType() == typeof(Comment))
                        {
                            var comment = (Comment)post;
                            writer.WriteLine(string.Format(
                                $"{Filter(comment.Subreddit)}," +
                                $"{Filter(comment.Upvotes.ToString())}," +
                                $"{Filter(comment.Body)}," +
                                $"{Filter(comment.Shortlink)}," +
                                $"(comment, not post)," +
                                $"{Filter(comment.Created.ToString())},"));
                        }
                        else if (post.GetType() == typeof(Post))
                        {
                            var p = (Post)post;
                            writer.WriteLine(string.Format(
                                $"{Filter(p.SubredditName)}," +
                                $"{Filter(p.Upvotes.ToString())}," +
                                $"{Filter(p.Title)}," +
                                $"{Filter(p.Shortlink)}," +
                                $"{Filter(p.Url.ToString())}," +
                                $"{Filter(p.Created.ToString())},"));
                        }
                        else
                        {
                            //Theoretically, this shouldn't hit
                            writer.WriteLine(string.Format(
                                $"unknown subreddit," +
                                $"{Filter(post.Upvotes.ToString())}," +
                                $"unknown content," +
                                $"{Filter(post.Shortlink)}," +
                                $"unknown content," +
                                $"{Filter(post.Created.ToString())},"));
                        }
                    }
                    catch
                    {
                        failedEntries.Add(post);
                        Console.WriteLine("Having trouble with this one. Skipping it and including it in the log file.");
                    }
                    finally
                    {
                        i++;
                    }
                }
                writer.Close();
            }
            Console.WriteLine("File written to " + output);

            if (failedEntries.Any())
            {
                Console.WriteLine($"{failedEntries.Count()} items could not be properly processed. If there is a log file in the folder where the exe lives, check there for more information.");
            }

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        private static void TryWriteLog(List<VotableThing> failedList)
        {
            try
            {
                using (var writer = new StreamWriter(@"./log.txt"))
                {
                    writer.WriteLine("The following items could not be processed: ");
                    foreach (var post in failedList)
                    {
                        writer.WriteLine(string.Format($"{Filter(post.Shortlink)},"));
                    }
                    writer.Close();
                }
            }
            catch
            {
                //"Can't really log anything if the log won't log, so I'll die silently for now."
            }
        }

        public static string ReadPassword()
        {
            var passbits = new Stack<string>();
            //keep reading
            for (ConsoleKeyInfo cki = Console.ReadKey(true); cki.Key != ConsoleKey.Enter; cki = Console.ReadKey(true))
            {
                if (cki.Key == ConsoleKey.Backspace)
                {
                    if (passbits.Count() > 0)
                    {
                        //rollback the cursor and write a space so it looks backspaced to the user
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        passbits.Pop();
                    }
                }
                else
                {
                    Console.Write("*");
                    passbits.Push(cki.KeyChar.ToString());
                }
            }
            string[] pass = passbits.ToArray();
            Array.Reverse(pass);
            Console.Write(Environment.NewLine);
            return string.Join(string.Empty, pass);
        }

        public static string Filter(string str)
        {
            bool mustQuote = (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
            if (mustQuote)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\"");
                foreach (char nextChar in str)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                        sb.Append("\"");
                }
                sb.Append("\"");
                return sb.ToString();
            }

            return str;
        }
    }
}
