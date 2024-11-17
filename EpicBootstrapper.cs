using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace RuniX_Bootstrap
{
    internal class Program
    {
        static string downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "RuniX");
        static string runixZipUrl = "Zip Url Here";

        static void Main(string[] args)
        {
            Console.Title = "Your Epic name here";
            PrintTitle();

            if (Directory.Exists(downloadDirectory))
                Directory.Delete(downloadDirectory, true);

            Directory.CreateDirectory(downloadDirectory);
            string zipPath = Path.Combine(downloadDirectory, "RuniX.zip");

            Console.WriteLine("Downloading...");
            new WebClient().DownloadFile(runixZipUrl, zipPath);

            Console.WriteLine("Extracting...");
            ZipFile.ExtractToDirectory(zipPath, downloadDirectory);
            File.Delete(zipPath);

            Console.WriteLine("Done!");
        }

        static void PrintTitle()
        {
            Console.WriteLine(@"

            ");
        }
    }
}
