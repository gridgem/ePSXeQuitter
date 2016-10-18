using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using WindowsInput;
using WindowsInput.Native;

namespace ePSXeQuitter
{
    class GameTitle
    {
        private ePSXeQuitterSettings settings;
        private string titleId = string.Empty;

        private List<ePSXeQuitterStateFile> stateFiles;
        private string stateFolder;
        private string stateBackupFolder;

        private InputSimulator inputSimulator;

        public GameTitle()
        {
            settings = new ePSXeQuitterSettings();
            stateFolder = settings.StateFolderPath;
            if (!Directory.Exists(stateFolder))
            {
                WriteLog(@"Couldn't find state folder: " + stateFolder);
                Application.Current.Shutdown();
            }
            stateBackupFolder = settings.StateBackupFolderPath;
            if (!Directory.Exists(stateBackupFolder))
            {
                Directory.CreateDirectory(stateBackupFolder);
            }

            inputSimulator = new InputSimulator();

            // 
            List<string> args = new List<string>(Environment.GetCommandLineArgs());
            args.RemoveAt(0);
            string titlePath = GetTitlePath(args);
            if (File.Exists(titlePath))
            {
                titleId = GetTitleIdFromFile(titlePath);
            }
            else if (Directory.Exists(titlePath))
            {
                titleId = GetTitleIdFromFolder(titlePath);
            }
            else
            {
                WriteLog(@"Couldn't detect the title path: " + titlePath);
                Application.Current.Shutdown();
            }

            // check title ID
            if (string.IsNullOrEmpty(titleId))
            {
                WriteLog(@"Couldn't detect the title ID.");
                Application.Current.Shutdown();
            }

            // check region
            if (Regex.Match(titleId, @"^(PBPX|SCED|SCES|SLED|SLEH|SLES)").Success)
            {
                ePSXeQuitterSettings.Region = Regions.EU;
            }
            else if (Regex.Match(titleId, @"^(ESPM|PAPX|PCPD|PCPX|SCPM|SCPS|SCZS|SIPS|SLBM|SLKA|SLPM|SLPS)").Success)
            {
                ePSXeQuitterSettings.Region = Regions.JP;
            }
            else
            {
                ePSXeQuitterSettings.Region = Regions.US;
            }
        }

        private string GetTitlePath(List<string> args)
        {
            bool loadbin_flag = false;
            bool cdrom_flag = false;
            foreach (string arg in args)
            {
                if (arg == "-loadbin")
                {
                    loadbin_flag = true;
                }
                else if (loadbin_flag == true)
                {
                    return arg;
                }
                else if (arg == "-cdrom")
                {
                    cdrom_flag = true;
                }
                else if (cdrom_flag == true)
                {
                    return arg + @":\";
                }
            }
            return @"";
        }

        private string GetTitleIdFromFolder(string driveLetter)
        {
            return GetTitleIdFromFile(Path.Combine(driveLetter, "SYSTEM.CNF"));
        }

        private string GetTitleIdFromFile(string isoFilePath)
        {
            FileStream fs = new FileStream(isoFilePath, FileMode.Open, FileAccess.Read);
            Regex wanted = new Regex(@"BOOT\s*=\s*cdrom:\\*(?<titleId>(?:ESPM|LSP|PAPX|PBPX|PCPD|PCPX|SCED|SCES|SCPM|SCPS|SCUS|SCZS|SIPS|SLBM|SLED|SLEH|SLES|SLKA|SLPM|SLPS|SLUS|SPUS)_\d\d\d\.\d\d)", RegexOptions.IgnoreCase);

            string titleId = "";
            byte[] bs = new byte[0x1000];
            for (;;)
            {
                int readSize = fs.Read(bs, 0, bs.Length);
                if (readSize == 0)
                {
                    break;
                }

                Match m = wanted.Match(Encoding.ASCII.GetString(bs));
                if (m.Success)
                {
                    titleId = m.Groups["titleId"].Value;
                    break;
                }
                fs.Seek(-25, SeekOrigin.Current); // 25 = "BOOT = cdrom:\AAAA_123.45".Length
            }
            fs.Close();

            return titleId;
        }

        public void PseudoLoadState(string selectedStateFilePath)
        {
            switch (settings.StateSaveType)
            {
                case StateSaveTypes.Cyclic:
                    PseudoLoadStateCyclic(selectedStateFilePath);
                    break;
                case StateSaveTypes.Direct:
                    break;
                case StateSaveTypes.Select:
                    break;
            }
        }

        private void PseudoLoadStateCyclic(string selectedStateFilePath)
        {
            ClearStateBackupFolder();

            // move all files same name as selected file
            foreach (string stateFilePath in GetStateFiles(stateFolder))
            {
                File.Move(stateFilePath, Path.Combine(stateBackupFolder, Path.GetFileName(stateFilePath)));
            }

            // copy back to original folder
            for (int i = 0; i < settings.StateCount; i++)
            {
                string selectedStateFileName = Path.GetFileName(selectedStateFilePath);
                string copyDstFilePath = Path.ChangeExtension(selectedStateFilePath, ".00" + i.ToString());
                File.Copy(Path.Combine(stateBackupFolder, selectedStateFileName), copyDstFilePath);
            }

            // simulate key down
            inputSimulator.Keyboard.KeyDown(VirtualKeyCode.F3);
            inputSimulator.Keyboard.Sleep(200);
            inputSimulator.Keyboard.KeyUp(VirtualKeyCode.F3);
            inputSimulator.Keyboard.Sleep(200);
            inputSimulator.Keyboard.KeyDown(VirtualKeyCode.F3);
            inputSimulator.Keyboard.Sleep(200);
            inputSimulator.Keyboard.KeyUp(VirtualKeyCode.F3);

            // delete copied state files in original folder
            foreach (string copiedStateFile in GetStateFiles(stateFolder))
            {
                File.Delete(copiedStateFile);
            }

            // move back state files to orignal folder
            foreach (string orignalFile in GetStateFiles(stateBackupFolder))
            {
                File.Move(orignalFile, Path.Combine(stateFolder, Path.GetFileName(orignalFile)));
            }
        }

        public void PseudoSaveState(int selectedIndex)
        {
            switch (settings.StateSaveType)
            {
                case StateSaveTypes.Cyclic:
                    PseudoSaveStateCyclic(selectedIndex);
                    break;
                case StateSaveTypes.Direct:
                    break;
                case StateSaveTypes.Select:
                    break;
            }
        }

        public void PreviewPseudoSaveState()
        {
            ClearStateBackupFolder();

            // detect all state files of the title
            List<string> stateFilesPath = GetStateFiles(stateFolder);

            // move state files to backup folder
            foreach (string stateFilePath in stateFilesPath)
            {
                string stateFileName = Path.GetFileName(stateFilePath);
                File.Move(stateFilePath, Path.Combine(stateBackupFolder, stateFileName));
                File.Move(stateFilePath + @".pic", Path.Combine(stateBackupFolder, stateFileName + @".pic"));
            }

            // save state temporary
            inputSimulator.Keyboard.KeyDown(VirtualKeyCode.F1);
            inputSimulator.Keyboard.Sleep(200);
            inputSimulator.Keyboard.KeyUp(VirtualKeyCode.F1);
            /*
            inputSimulator.Keyboard.Sleep(200);
            inputSimulator.Keyboard.KeyDown(VirtualKeyCode.F1);
            inputSimulator.Keyboard.Sleep(200);
            inputSimulator.Keyboard.KeyUp(VirtualKeyCode.F1);
            */

            // change extention of the temporary state file and move to backup folder
            List<string> temporaryStateFilesPath = GetStateFiles(stateFolder);
            if (temporaryStateFilesPath.Count != 1)
            {
                Application.Current.Shutdown();
            }
            else
            {
                string temporaryStateFilePath = temporaryStateFilesPath[0];
                try
                {
                    var dstPath = Path.Combine(stateBackupFolder, Path.ChangeExtension(Path.GetFileName(temporaryStateFilePath), @".tmp"));
                    File.Move(temporaryStateFilePath, dstPath);
                    File.Move(temporaryStateFilePath + @".pic", dstPath + @".pic");
                }
                catch { }
            }

            // move back proper state files to original state folder
            foreach (string stateFilePath in stateFilesPath)
            {
                var dstPath = Path.Combine(stateBackupFolder, Path.GetFileName(stateFilePath));
                File.Move(dstPath, stateFilePath);
                File.Move(dstPath + @".pic", stateFilePath + @".pic");
            }
        }

        private void PseudoSaveStateCyclic(int selectedIndex)
        {
            string ext = @".00" + selectedIndex.ToString();

            // delete state file if it exists same name as the temporary state file in original state folder
            foreach (string stateFilePath in GetStateFiles(stateFolder))
            {
                if (Path.GetExtension(stateFilePath) == ext)
                {
                    try
                    {
                        File.Delete(stateFilePath);
                        File.Delete(stateFilePath + @".pic");
                    }
                    catch { }
                }
            }

            // change extention of the temporary state file and
            // move the temporary state file to original state folder
            List<string> temporaryStateFilesPath = GetStateFiles(stateBackupFolder);
            if (temporaryStateFilesPath.Count != 1)
            {
                Application.Current.Shutdown();
            }
            else
            {
                string temporaryStateFilePath = temporaryStateFilesPath[0];
                try
                {
                    var dstPath = Path.Combine(stateFolder, Path.ChangeExtension(Path.GetFileName(temporaryStateFilePath), ext));
                    File.Move(temporaryStateFilePath, dstPath);
                    File.Move(temporaryStateFilePath + @".pic", dstPath + @".pic");
                }
                catch { }
            }
        }

        public void ClearStateBackupFolder()
        {
            foreach (string temporaryStateFilePath in GetStateFiles(stateBackupFolder))
            {
                try
                {
                    File.Delete(temporaryStateFilePath);
                    File.Delete(temporaryStateFilePath + @".pic");
                }
                catch { }
            }
        }

        public List<string> GetStateFiles()
        {
            return GetStateFiles(stateFolder);
        }

        private List<string> GetStateFiles(string searchFolder)
        {
            string[] files = Directory.GetFiles(searchFolder, titleId + "*");
            List<string> _files = new List<string>();
            foreach (string file in files)
            {
                if (Path.GetExtension(file) != ".pic")
                {
                    _files.Add(file);
                }
            }
            return _files;
        }

        private void WriteLog(string log)
        {
            string appendText = DateTime.Now.ToString() + "\t" + log + Environment.NewLine;
            File.AppendAllText(@"log.txt", appendText);
        }
    }
}
