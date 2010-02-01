﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace GTA
{
    public struct Model
    {
        #region item name code
        internal static bool IDELoaded = false;
        internal static Dictionary<string, int> Models = new Dictionary<string, int>();

        internal static void LoadIDE()
        {
            if (IDELoaded)
            {
                return;
            }

            IDELoaded = true; // though we may not succeed, this is for the better - no need trying again if we fail

            var gtadat = "";
            var reader = File.OpenText(@"data\gta.dat");
            gtadat += reader.ReadToEnd();
            reader.Close();

            // also load default.dat
            reader = File.OpenText(@"data\default.dat");
            gtadat += reader.ReadToEnd();
            reader.Close();

            // read any definition files
            var matches = Regex.Matches(gtadat, "^IDE (.*?)$", RegexOptions.Multiline);

            // pre-compile the line regex
            var objsLine = new Regex("^([0-9]*?), (.*?), (.*?), (.*?), (.*?)$", RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                string filename = match.Groups[1].Value.Trim();

                // no exception checking, because if the file did not exist
                // the game wouldn't even be able to load
                StreamReader ide = File.OpenText(filename);
                string data = ide.ReadToEnd();
                ide.Close();

                // and match again
                var imatches = objsLine.Matches(data);

                foreach (Match line in imatches)
                {
                    Models[line.Groups[2].Value.Trim().ToLower()] = int.Parse(line.Groups[1].Value);
                }
            }
        }
        #endregion

        private int _id;

        public Model(string name)
        {
            LoadIDE();

            _id = Models[name.ToLower()];
        }

        public Model(int id)
        {
            _id = id;
        }

        public void Load()
        {
            Internal.Function.Call(0x0247, ID);

            while (!Loaded)
            {
                GTAUtils.Wait(0);
            }
        }

        public void Release()
        {
            Internal.Function.Call(0x0249, ID);
        }

        public static implicit operator Model(int source)
        {
            return new Model(source);
        }

        public static implicit operator Model(string source)
        {
            return new Model(source);
        }

        public static implicit operator Internal.Parameter(Model source)
        {
            return new Internal.Parameter(source.ID);
        }

        public static bool operator ==(Model left, Model right) {
            return left.Equals(right);
        }

        public static bool operator !=(Model left, Model right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public override bool Equals(object obj)
        {
            return (this.GetHashCode() == obj.GetHashCode());
        }

        public override string ToString()
        {
            return "#" + _id.ToString();
        }

        public bool Loaded
        {
            get
            {
                try
                {
                    if (Internal.Function.Call(0x0248, _id))
                    {
                        return true;
                    }
                }
                catch (AccessViolationException) { }

                return false;
            }
        }

        public bool Exists
        {
            get
            {
                try
                {
                    Internal.Function.Call(0x0248, _id);
                    return true;
                }
                catch (AccessViolationException)
                {
                    return false;
                }
            }
        }

        public int ID
        {
            get
            {
                return _id;
            }
        }

        public static void Load(Model model)
        {
            model.Load();
        }

        public static void Release(Model model)
        {
            model.Release();
        }


    }
}
