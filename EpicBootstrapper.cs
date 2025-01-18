using System;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;

namespace Bootstrap_Tut
{
    internal class Program
    {
        static string appName = "YOUR APP NAME";
        static string zipName = "YouZip.zip";
        static string exeName = "App.exe";
        static string downloadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appName);
        static string versionUrl = "URL TO YOUR VERSION.TXT FILE";
        static string ZipUrl = "URL TO YOUR ZIP FILE";
        static string localVersionFile = Path.Combine(downloadDirectory, "version.txt");

        static void Main(string[] args)
        {
            SetupConsole();
            PrintTitle();

            if (CheckForUpdates() || IsFirstRun())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{appName} updated. Starting download...");

                EnsureCleanDirectory(downloadDirectory);
                string downloadedFile = DownloadFile(zipName);

                if (downloadedFile != null)
                {
                    ExtractAndRun(downloadedFile);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{appName} is up to date!");

                string exePath = FindExecutablePath(Path.Combine(downloadDirectory, appName), exeName);
                if (!string.IsNullOrEmpty(exePath))
                {
                    LaunchApp(exePath);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Could not find {exeName}.");
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nClosing...");
            Thread.Sleep(2000);
            Console.WriteLine("Goodbye!");
            Thread.Sleep(500);
            Environment.Exit(0);
        }

        static bool CheckForUpdates()
        {
            try
            {
                using (var client = new WebClient())
                {
                    string remoteVersion = client.DownloadString(versionUrl).Trim();
                    string localVersion = null;

                    try
                    {
                        if (File.Exists(localVersionFile))
                        {
                            localVersion = File.ReadAllText(localVersionFile).Trim();
                        }
                    }
                    catch
                    {
                    }

                    if (string.IsNullOrEmpty(localVersion) || localVersion != remoteVersion)
                    {
                        File.WriteAllText(localVersionFile, remoteVersion);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false; // No update needed
        }

        static bool IsFirstRun()
        {
            return !Directory.Exists(Path.Combine(downloadDirectory, appName));
        }

        static void SetupConsole()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
        }

        static void PrintTitle()
        {
            string title = $@"
___________      __               .__       .__   
\__    ___/_ ___/  |_  ___________|__|____  |  |  
  |    | |  |  \   __\/  _ \_  __ \  \__  \ |  |  
  |    | |  |  /|  | (  <_> )  | \/  |/ __ \|  |__
  |____| |____/ |__|  \____/|__|  |__(____  /____/
                                          \/      
                                       ";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(title);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"\nWelcome to {appName}!");
            Console.ResetColor();
        }

        static void EnsureCleanDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Cleaning up previous files...");
                Directory.Delete(directoryPath, true);
            }
            Directory.CreateDirectory(directoryPath);
        }

        static string DownloadFile(string fileName)
        {
            try
            {
                string filePath = Path.Combine(downloadDirectory, fileName);

                using (var client = new WebClient())
                {
                    client.DownloadFile(ZipUrl, filePath);
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\nFile downloaded");
                return filePath;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error downloading the file: {ex.Message}");
                return null;
            }
        }

        static void ExtractAndRun(string zipFilePath)
        {
            try
            {
                string extractPath = Path.Combine(downloadDirectory, appName);
                EnsureCleanDirectory(extractPath);

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Extracting {zipName}...");

                ZipFile.ExtractToDirectory(zipFilePath, extractPath);
                File.Delete(zipFilePath);

                string exePath = FindExecutablePath(extractPath, exeName);
                if (!string.IsNullOrEmpty(exePath))
                {
                    LaunchApp(exePath);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{exeName} not found in extracted files.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error extracting {zipName}: {ex.Message}");
            }
        }

        static void LaunchApp(string exePath)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Launching {exeName}");

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = true
            });
        }

        static string FindExecutablePath(string directory, string exeName)
        {
            try
            {
                string[] files = Directory.GetFiles(directory, exeName, SearchOption.AllDirectories);
                return files.Length > 0 ? files[0] : null;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error finding executable: {ex.Message}");
                return null;
            }
        }
    }
}
