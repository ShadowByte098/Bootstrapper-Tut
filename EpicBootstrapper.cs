using System;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;

namespace Tutorial_Bootstrapper
{
    internal class Program
    {
        static readonly string ProjectName = "App";
        static readonly string VersionUrl = "https://github.com/version.txt";
        static readonly string ZipUrl = "https://github.com/App.zip";
        static readonly string ExeName = "App.exe";

        static readonly string BaseDir = Directory.GetCurrentDirectory();
        static readonly string DownloadDir = Path.Combine(BaseDir, ProjectName);
        static readonly string ExtractDir = Path.Combine(DownloadDir, "extracted");
        static readonly string LocalVersionFile = Path.Combine(ExtractDir, "version.txt");

        static void Main(string[] args)
        {
            Console.Clear();
            WriteAnimatedTitle();

            bool needsUpdate = IsFirstRun() || CheckForUpdates();
            if (needsUpdate)
            {
                WriteStatus("Updating files...", ConsoleColor.Yellow);
                AnimateDots("Cleaning directory");
                CleanDirectory(DownloadDir);

                string zip = DownloadZip();
                if (zip != null)
                {
                    ExtractZip(zip);
                    DownloadVersionFile();
                    LaunchApp();
                }
            }
            else
            {
                WriteStatus("Up to date — launching", ConsoleColor.Green);
                LaunchApp();
            }

            WriteStatus("\nDone. This window will close in 3 seconds.", ConsoleColor.Cyan);
            Thread.Sleep(3000);
        }

        static void WriteAnimatedTitle()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
        foreach (var line in TitleArt)
        {
            Console.WriteLine(line);
            Thread.Sleep(100);
        }
            Console.ResetColor();
            Console.WriteLine();
        }

        static readonly string[] TitleArt = new string[]
        {
            @"___________      __               .__       .__ ._.",
            @"\__    ___/_ ___/  |_  ___________|__|____  |  || |",
            @"  |    | |  |  \   __\/  _ \_  __ \  \__  \ |  || |",
            @"  |    | |  |  /|  | (  <_> )  | \/  |/ __ \|  |_\\|",
            @"  |____| |____/ |__|  \____/|__|  |__(____  /____/_",
            @"                                          \/     \/"
       };


        static void AnimateDots(string message, int dotCount = 3, int delay = 300)
        {
            Console.Write(message);
            for (int i = 0; i < dotCount; i++)
            {
                Console.Write(".");
                Thread.Sleep(delay);
            }
            Console.WriteLine();
        }

        static bool IsFirstRun() => !Directory.Exists(ExtractDir);

        static bool CheckForUpdates()
        {
            WriteStatus("Checking for updates...", ConsoleColor.Yellow);
            try
            {
                Directory.CreateDirectory(ExtractDir);
                using (var wc = new WebClient())
                {
                    string remote = wc.DownloadString(VersionUrl).Trim();
                    string local = File.Exists(LocalVersionFile) ? File.ReadAllText(LocalVersionFile).Trim() : "";

                    if (local != remote)
                    {
                        WriteStatus("Update available!", ConsoleColor.Cyan);
                        return true;
                    }
                }
            }
            catch
            {
                WriteStatus("Could not check updates; will download anyway.", ConsoleColor.Red);
                return true;
            }
            return false;
        }

        static void CleanDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        static string DownloadZip()
        {
            WriteStatus("Downloading files...", ConsoleColor.Yellow);
            Console.WriteLine();

            string outPath = Path.Combine(DownloadDir, ProjectName + ".zip");
            var done = new ManualResetEvent(false);

            using (var wc = new WebClient())
            {
                wc.DownloadProgressChanged += (s, e) =>
                {
                    if (e.ProgressPercentage <= 99)
                        ShowProgress("Download", e.ProgressPercentage, ConsoleColor.Cyan);
                };

                wc.DownloadFileCompleted += (s, e) =>
                {
                    ShowProgress("Download", 100, ConsoleColor.Cyan);
                    Console.WriteLine();
                    WriteStatus("Download complete.", ConsoleColor.Green);
                    done.Set();
                };

                wc.DownloadFileAsync(new Uri(ZipUrl), outPath);
                done.WaitOne();
            }

            return outPath;
        }

        static void ExtractZip(string zipPath)
        {
            WriteStatus("Extracting files...", ConsoleColor.Yellow);
            Console.WriteLine();

            try
            {
                if (Directory.Exists(ExtractDir))
                    Directory.Delete(ExtractDir, true);
                Directory.CreateDirectory(ExtractDir);

                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    long total = archive.Entries.Count;
                    long done = 0;
                    foreach (var entry in archive.Entries)
                    {
                        string target = Path.Combine(ExtractDir, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(target, true);
                            File.SetAttributes(target, FileAttributes.Normal);
                        }

                        done++;
                        int pct = (int)((done * 100) / total);
                        ShowProgress("Extract", pct, ConsoleColor.Magenta);
                    }
                }

                File.Delete(zipPath);
                Console.WriteLine();
                WriteStatus("Extraction complete.", ConsoleColor.Green);

                var files = Directory.GetFiles(ExtractDir);
                var dirs = Directory.GetDirectories(ExtractDir);
                if (files.Length == 0 && dirs.Length == 1)
                {
                    WriteStatus("Flattening folder...", ConsoleColor.DarkCyan);
                    string nested = dirs[0];
                    foreach (var f in Directory.GetFiles(nested))
                        File.Move(f, Path.Combine(ExtractDir, Path.GetFileName(f)));
                    foreach (var d in Directory.GetDirectories(nested))
                        Directory.Move(d, Path.Combine(ExtractDir, Path.GetFileName(d)));
                    Directory.Delete(nested, true);
                }
            }
            catch (Exception ex)
            {
                WriteStatus("Extraction failed: " + ex.Message, ConsoleColor.Red);
            }
        }

        static void DownloadVersionFile()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.DownloadFile(VersionUrl, LocalVersionFile);
                }
            }
            catch
            {
                WriteStatus("Failed to update version.txt.", ConsoleColor.Red);
            }
        }

        static void LaunchApp()
        {
            try
            {
                string[] found = Directory.GetFiles(ExtractDir, ExeName, SearchOption.AllDirectories);
                if (found.Length == 0)
                {
                    WriteStatus("Executable not found.", ConsoleColor.Red);
                    return;
                }

                WriteStatus("Launching " + ExeName + "...", ConsoleColor.Green);
                var exe = found[0];
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = Path.GetDirectoryName(exe),
                    UseShellExecute = true
                };

                try
                {
                    Process.Start(psi);
                }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5)
                {
                    WriteStatus("Access denied.", ConsoleColor.Red);
                    Console.Write("Run as administrator? (Y/N): ");
                    if (Console.ReadKey(true).Key == ConsoleKey.Y)
                    {
                        psi.Verb = "runas";
                        Process.Start(psi);
                    }
                    else
                    {
                        WriteStatus("Canceled by user.", ConsoleColor.DarkGray);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteStatus("Launch failed: " + ex.Message, ConsoleColor.Red);
            }
        }

        static void ShowProgress(string label, int pct, ConsoleColor color)
        {
            const int width = 30;
            int filled = pct * width / 100;
            string bar = new string('#', filled) + new string('-', width - filled);

            string text = $"{label.PadRight(8)} [{bar}] {pct}%";
            Console.ForegroundColor = color;
            Console.Write("\r" + text.PadRight(Console.WindowWidth - 1));
            Console.ResetColor();
        }

        static void WriteStatus(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }
    }
}
