using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using SharpCompress.Common;
using SharpCompress.Readers;
using Shoko.Commons.Utils;
using Shoko.Server.Repositories;
using Shoko.Server.Settings;
using Shoko.Server.Utilities;

namespace Shoko.Server
{
    public static class AVDumpHelper
    {
        private static readonly string Destination = Path.Combine(ServerSettings.ApplicationPath, "Utilities", "AVDump");
        private static readonly string AVDumpZipDestination = Path.Combine(Destination, "avdump3.zip");

        private const string AVDump2URL = @"https://cdn.anidb.net/client/avdump3/avdump3_8293_stable.zip";

        private static readonly string AvdumpDestination = Path.Combine(Destination, "AVDump3CL");

        private static readonly string[] OldAVDump =
        {
            "AVDump2CL.exe", "AVDump2CL.exe.config", "AVDump2Lib.dll", "AVDump2Lib.dll.config", "CSEBMLLib.dll",
            "Ionic.Zip.Reduced.dll", "libMediaInfo_x64.so", "libMediaInfo_x86.so", "MediaInfo_x64.dll",
            "MediaInfo_x86.dll", "Error",
            "AVD3AniDBModule.dll", "AVDump3CL", "AVDump3CL.dll", "AVDump3CL.exe", "AVDump3CL.runtimeconfig.json", "AVDump3Lib.dll",
            "AVDump3NativeLib-aarch64.so", "AVDump3NativeLib-x64.so", "AVDump3NativeLib.dll", "AVDump3UI.dll", "BXmlLib.dll", "ExtKnot.StringInvariants.dll",
            "Ionic.Zlib.Core.dll", "MediaInfo-aarch64.so", "MediaInfo-x64.so", "MediaInfo.dll", "Microsoft.CodeAnalysis.CSharp.dll", "Microsoft.CodeAnalysis.CSharp.Scripting.dll",
            "Microsoft.CodeAnalysis.dll", "Microsoft.CodeAnalysis.Scripting.dll", "Microsoft.Extensions.DependencyInjection.Abstractions.dll", "Microsoft.Extensions.DependencyInjection.dll",
            "Microsoft.Extensions.Logging.Abstractions.dll", "Microsoft.Extensions.Logging.dll", "Microsoft.Extensions.Options.dll", "Microsoft.Extensions.Primitives.dll",
            "Newtonsoft.Json.dll", "Serilog.dll", "Serilog.Extensions.Logging.dll",
        };

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static event EventHandler<AVDumpEd2kEventArgs> Ed2kProvided;
        public static event EventHandler<AVDumpProgressEventArgs> ProgressChanged;

        private static bool GetAndExtractAVDump()
        {
            if (File.Exists(AVDumpZipDestination)) return ExtractAVDump();
            return DownloadFile(AVDump2URL, AVDumpZipDestination) && ExtractAVDump();
        }

        private static bool ExtractAVDump()
        {
            try
            {
                // First clear out the existing one. 
                DeleteOldAVDump();

                // Now make the new one
                using Stream stream = File.OpenRead(AVDumpZipDestination);
                using var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(Destination, new ExtractionOptions
                        {
                            // This may have serious problems in the future, but for now, AVDump is flat
                            ExtractFullPath = false,
                            Overwrite = true,
                        });
                    }
                }
            }
            catch
            {
                return false;
            }

            try
            {
                File.Delete(AVDumpZipDestination);
            }
            catch
            {
                // eh we tried
            }
            return true;
        }
        
        private static void DeleteOldAVDump()
        {
            var oldPath = Directory.GetParent(Destination).FullName;
            foreach (var name in OldAVDump)
            {
                try
                {
                    var path = Path.Combine(oldPath, name);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        continue;
                    }
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                }
                catch
                {
                    // Eh we tried
                }
            }
        }

        private static bool DownloadFile(string sourceURL, string fileName)
        {
            try
            {
                if (File.Exists(fileName)) return true;
                using var stream = Misc.DownloadWebBinary(sourceURL);
                if (stream == null) return false;
                var destinationFolder = Directory.GetParent(fileName).FullName;
                if (!Directory.Exists(destinationFolder)) Directory.CreateDirectory(destinationFolder);

                using var fileStream = File.Create(fileName);
                CopyStream(stream, fileStream);

                return true;
            }
            catch
            {
                return false;
            }
        }
        public static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, len);
        }
        
        public static string[] DumpFile(int vid)
        {
            var vl = RepoFactory.VideoLocal.GetByID(vid);
            if (vl == null) throw new Exception("Unable to get VideoLocal with id: " + vid);
            var file = vl.GetBestVideoLocalPlace(true)?.FullServerPath;
            if (string.IsNullOrEmpty(file)) throw new Exception("Unable to get file: " + vid);
            return DumpFile(file);
        }

        public static string[] DumpFile(string file)
        {
            try
            {
                if (string.IsNullOrEmpty(file))
                    throw new Exception("File path cannot be null");
                if (!File.Exists(file))
                    throw new Exception("Could not find Video File: " + file);

                var filenameArgs = GetFilenameAndArgsForOS(file);

                Logger.Info($"Dumping File with AVDump: {filenameArgs.Item1} {filenameArgs.Item2}");
                
                var pProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = filenameArgs.Item1,
                        Arguments = filenameArgs.Item2,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    },
                };

                var ed2ks = new List<string>();
                var running = true;
                pProcess.OutputDataReceived += (sender, args) =>
                {
                    var line = args.Data;
                    if (line == null) return;
                    if (line.Contains("ed2k:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        lock(ed2ks)
                            ed2ks.Add(line);
                        Ed2kProvided?.Invoke(null, new AVDumpEd2kEventArgs
                        {
                            Ed2k = line,
                        });
                    }
                    else if (line.Contains("Total"))
                    {
                        var progressLine = line[(line.IndexOf("[") + 1)..(line.IndexOf("]") - 1)];
                        var progress = (decimal) progressLine.Count(a => a == '#') / progressLine.Length;
                        ProgressChanged?.Invoke(null, new AVDumpProgressEventArgs
                        {
                            Progress = progress * 100M,
                        });
                        if (1M - progress < 0.01M) running = false;
                    }
                };
                pProcess.Start();
                pProcess.BeginOutputReadLine();

                while (running)
                {
                    Thread.Sleep(250);
                }

                return ed2ks.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while AVDumping the file \"file\":\n{ex}");
                throw new AggregateException($"An error occurred while AVDumping the file:\n{ex}", ex);
            }
        }

        private static Tuple<string, string> GetFilenameAndArgsForOS(string file)
        {
            var fileName = (char)34 + file + (char)34;

            var args = $"{AvdumpDestination}.dll --Auth={ServerSettings.Instance.AniDb.Username.Trim()}:" +
                       $"{ServerSettings.Instance.AniDb.AVDumpKey?.Trim()} --PrintEd2kLink --PauseBeforeExit {fileName}";
            const string executable = "dotnet";

            return Tuple.Create(executable, args);
        }
    }
}