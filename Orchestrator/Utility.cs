using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Orchestrator
{
    public static class Utility
    {
        // Calls FFMPEG comand for extracting thumbnails from the video
        public static async Task<string> GetVideoThumbnails(string videoUri)
        {
            string outputFolder = $"output_{Guid.NewGuid()}";
            string fullPathOutFolder = $"D:\\testSet\\{outputFolder}";

            if (!Directory.Exists(fullPathOutFolder))
            {
                Directory.CreateDirectory(fullPathOutFolder);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "c:\\ffmpeg\\bin\\ffmpeg.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = $"-i {videoUri} -vf select=eq(pict_type\\,PICT_TYPE_I)  -vsync 2 -s 480x320 -r 24 -f image2 {fullPathOutFolder}\\thumbnails-%05d.jpeg";

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;


            using (Process exeProcess = Process.Start(startInfo))
            {
                string error = exeProcess.StandardError.ReadToEnd();
                string output = exeProcess.StandardError.ReadToEnd();
                exeProcess.WaitForExit();
            }

            return fullPathOutFolder;
        }

        // Gets all thumbnails' name from the given folder
        public static List<string> GetListOfThumbnails(string folderPath)
        {
            var thumbnails = new List<string>(Directory.GetFiles(folderPath));
            return thumbnails;
        }

        // Gets s sample of thumbnails
        public static List<string> ReduceNumberOfThumbnails(List<string> thumbnails)
        {
            var newList = thumbnails.Where((x, i) => i % 20 == 0).ToList();
            return newList;
        }
    }
}
