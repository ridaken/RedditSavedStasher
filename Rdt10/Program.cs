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
            RedditSharp.Reddit reddit = null;
            var authenticated = false;
            var username = "fallingwalls";
            var password = Program.ReadPassword();
            try
            {
                Console.WriteLine("Logging in...");
                reddit = new Reddit(username, password);
                authenticated = reddit.User != null;
            }
            catch (AuthenticationException)
            {
                Console.WriteLine("Incorrect login.");
                authenticated = false;
            }

            //RemovePosts(reddit);
            //RemoveComments(reddit);
        }

        private static void RemoveComments(Reddit reddit)
        {
            var updatedText = @"[removed]";
            var commentList = reddit.User.Comments.Where(x => x.Created < DateTime.Now.AddDays(-7)).ToList();

            foreach (var comment in commentList.Where(x => x.Body.Contains(updatedText)))
            {
                comment.Del();
            }

            using (var writer = new StreamWriter(@"C:/Users/tvokac/Desktop/Comments.csv"))
            {
                writer.WriteLine("Subreddit,Upvotes,Comment,Link,Gilded,Link Title,Created");

                //Its not LinkId, 
                foreach (var post in commentList)
                {
                    writer.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                        Filter(post.Subreddit), //0
                        Filter(post.Upvotes.ToString()), //1
                        Filter(post.Body), //2
                        Filter(post.Shortlink), //3
                        Filter(post.Gilded.ToString()), //4
                        Filter(post.LinkTitle),
                        Filter(post.Created.ToString())));

                    post.EditText(updatedText);
                    post.Del();
                }
                writer.Close();
            }
        }

        private static void RemovePosts(Reddit reddit)
        {
            var postList = reddit.User.Posts;
            using (var writer = new StreamWriter(@"C:/Users/tvokac/Desktop/Posts.csv"))
            {
                writer.WriteLine("Subreddit,Upvotes,Title,Link,Upvotes,Downvotes");
                foreach (var post in postList)
                {
                    writer.WriteLine(string.Format("{0},{4},{2},{3},{1}, {5}", Filter(post.SubredditName), Filter(post.Created.ToString()), Filter(post.Title), Filter(post.Url.ToString()), Filter(post.Upvotes.ToString()), Filter(post.Downvotes.ToString())));

                    post.Remove();
                }
                writer.Close();
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
