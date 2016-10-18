using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IniParser;
using IniParser.Model;

namespace ePSXeQuitter
{
    public enum StateSaveTypes
    {
        Cyclic,
        Direct,
        Select
    }

    public enum Regions
    {
        EU,
        JP,
        US
    }

    class ePSXeQuitterSettings
    {
        // exe path
        private string _ePSXePath;
        public string ePSXePath
        {
            get { return _ePSXePath; }
            set
            {
                _ePSXePath = value;
                StateFolderPath = Path.Combine(Directory.GetParent(_ePSXePath).ToString(), @"sstates");
                StateBackupFolderPath = Path.Combine(Directory.GetParent(_ePSXePath).ToString(), @"sstates_eQ");
            }
        }

        // state folder path
        private string stateFolderPath;
        public string StateFolderPath
        {
            get { return stateFolderPath; }
            set { stateFolderPath = value; }
        }

        // state backup folder path
        private string stateBackupFolderPath;
        public string StateBackupFolderPath
        {
            get { return stateBackupFolderPath; }
            set { stateBackupFolderPath = value; }
        }

        // hotkey
        private string hotkeys;
        public string Hotkeys
        {
            get { return hotkeys; }
            set { hotkeys = value; }
        }

        // state type
        private StateSaveTypes stateSaveType;
        public StateSaveTypes StateSaveType
        {
            get { return stateSaveType; }
            set { stateSaveType = value; }
        }

        // state count
        private int stateCount;
        public int StateCount {
            get { return stateCount; }
            set { stateCount = value; }
        }

        // Region
        private static Regions region;
        public static Regions Region
        {
            get { return region; }
            set { region = value; }
        }

        public ePSXeQuitterSettings()
        {
            ePSXePath = @"ePSXe.exe";
            StateFolderPath = @"sstates";
            StateBackupFolderPath = @"sstates_eQ";
            hotkeys = "LeftShoulder, Guide";
            stateSaveType = StateSaveTypes.Cyclic;
            stateCount = 5;

            string iniFile = @"ePSXeQuitter.ini";
            if (!File.Exists(iniFile))
            {
                return;
            }

            var parser = new FileIniDataParser();
            IniData ini = parser.ReadFile(iniFile);

            if (ini["ePSXe"]["ExePath"] != null)
            {
                ePSXePath = ini["ePSXe"]["ExePath"];
            }

            if (ini["ePSXe"]["StateFolder"] != null)
            {
                StateFolderPath = ini["ePSXe"]["StateFolder"];
            }

            if (ini["ePSXe"]["StateBackupFolder"] != null)
            {
                StateBackupFolderPath = ini["ePSXe"]["StateBackupFolder"];
            }

            if (ini["ePSXe"]["Hotkey"] != null)
            {
                Hotkeys = ini["ePSXe"]["Hotkey"];
            }

            /*
            if (ini["ePSXe"]["StateSaveType"] != null)
            {
                try
                {
                    StateSaveTypes iniValue = (StateSaveTypes)Enum.Parse(typeof(StateSaveTypes), ini["ePSXe"]["StateSaveType"]);
                    if (Enum.IsDefined(typeof(StateSaveTypes), iniValue))
                    {
                        StateSaveType = iniValue;
                    }
                }
                catch { }
            }

            if (ini["ePSXe"]["StateCount"] != null)
            {
                try
                {
                    StateCount = int.Parse(ini["ePSXe"]["StateCount"]);
                }
                catch { }
            }

            if (ini["ePSXe"]["Region"] != null)
            {
                try
                {
                    Region = (Regions)Enum.Parse(typeof(Regions), ini["ePSXe"]["Region"]);
                }
                catch { }
            }
            */
        }
    }
}
