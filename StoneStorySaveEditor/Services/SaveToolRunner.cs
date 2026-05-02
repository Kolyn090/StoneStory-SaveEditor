using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StoneStorySaveEditor.Services
{
    public static class SaveToolRunner
    {
        public static async Task<string> DecryptTextAsync(string encryptedOrFullSaveText)
        {
            return await RunWithTempFilesAsync("decrypt", encryptedOrFullSaveText);
        }

        public static async Task<string> EncryptTextAsync(string decryptedText)
        {
            return await RunWithTempFilesAsync("encrypt", decryptedText);
        }

        private static async Task<string> RunWithTempFilesAsync(string mode, string inputText)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "StoneStorySaveEditor");
            Directory.CreateDirectory(tempDir);

            string id = Guid.NewGuid().ToString("N");
            string inputPath = Path.Combine(tempDir, id + "_input.txt");
            string outputPath = Path.Combine(tempDir, id + "_output.txt");

            try
            {
                File.WriteAllText(inputPath, inputText, Encoding.UTF8);

                string exePath = Path.Combine(AppContext.BaseDirectory, "SaveTool.exe");

                if (!File.Exists(exePath))
                {
                    throw new FileNotFoundException("Could not find SaveTool.exe next to the GUI app.", exePath);
                }

                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                psi.ArgumentList.Add(mode);
                psi.ArgumentList.Add(inputPath);
                psi.ArgumentList.Add(outputPath);

                Process? process = Process.Start(psi);

                if (process == null)
                {
                    throw new Exception("Failed to start SaveTool.exe.");
                }

                using (process)
                {
                    if (process == null)
                    {
                        throw new Exception("Failed to start SaveTool.exe.");
                    }

                    string stdout = await process.StandardOutput.ReadToEndAsync();
                    string stderr = await process.StandardError.ReadToEndAsync();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception(
                            "SaveTool.exe failed with exit code " + process.ExitCode +
                            "\n\nSTDOUT:\n" + stdout +
                            "\n\nSTDERR:\n" + stderr
                        );
                    }

                    if (!File.Exists(outputPath))
                    {
                        throw new Exception(
                            "SaveTool.exe finished, but did not create output file." +
                            "\n\nSTDOUT:\n" + stdout +
                            "\n\nSTDERR:\n" + stderr
                        );
                    }

                    return File.ReadAllText(outputPath, Encoding.UTF8);
                }
            }
            finally
            {
                TryDelete(inputPath);
                TryDelete(outputPath);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore temp cleanup failure.
            }
        }
    }
}