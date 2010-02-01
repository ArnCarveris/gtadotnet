﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GTA
{
    public class Game
    {
        private static int _curID = 0;

        public static Dictionary<string, string> CustomGXTs
        {
            get;
            set;
        }

        public static Pad GamePad { get; set; }
        public static Pad GamePad2 { get; set; }

        static Game()
        {
            CustomGXTs = new Dictionary<string, string>();

            TextHook.RegisterCallback(key =>
            {
                if (CustomGXTs.ContainsKey(key))
                {
                    return CustomGXTs[key];
                }

                return null;
            });

            GamePad = new Pad(new IntPtr(0xB73458));
            GamePad2 = new Pad(new IntPtr(0xB7358C));
        }

        public static Time Time
        {
            get
            {
                VarPointer h = new VarPointer(0);
                VarPointer m = new VarPointer(0);

                Internal.Function.Call(0x00bf, h, m);

                int hh = (int)h;
                int mm = (int)m;

                return new Time() { Hours = hh, Minutes = mm };
            }
            set
            {
                Internal.Function.Call(0x00c0, value.Hours, value.Minutes);
            }
        }

        public static void PlaySound(int soundID)
        {
            PlaySound(soundID, new Vector3(0, 0, 0));
        }

        public static void PlaySound(int soundID, Vector3 at)
        {
            Internal.Function.Call(0x097A, at, soundID);
        }

        #region fading
        public static void Fade(bool fadeIn)
        {
            Fade(fadeIn, true);
        }

        public static void Fade(bool fadeIn, bool wait)
        {
            if (fadeIn)
            {
                FadeIn();
            }
            else
            {
                FadeOut();
            }

            if (wait)
            {
                while (Fading)
                {
                    GTAUtils.Wait(0);
                }
            }
        }

        public static void FadeIn()
        {
            Internal.Function.Call(0x016a, 500, true);
        }

        public static void FadeOut()
        {
            Internal.Function.Call(0x016a, 500, false);
        }

        public static bool Fading
        {
            get
            {
                return Internal.Function.Call(0x016b);
            }
        }
        #endregion

        #region HUD
        public static void ShowHUD(bool enabled)
        {
            Internal.Function.Call(0x0826, enabled);
        }

        public static void ShowRadar(bool enabled)
        {
            Internal.Function.Call(0x0581, enabled);
        }

        public static void ShowZoneName(bool enabled)
        {
            Internal.Function.Call(0x09ba, enabled);
        }

        public static unsafe void RawHUDEnabled(bool enabled)
        {
            *(byte*)0xBA6769 = (enabled) ? (byte)1 : (byte)0;
        }
        #endregion

        #region text
        public static void DisplayTextBox(string text)
        {
            Internal.Function.Call(0x03E5, RegisterKey(text));
        }

        public static void DisplayText(string text, int time)
        {
            Internal.Function.Call(0x00BC, RegisterKey(text), time, 1);
        }

        public static void DisplayMission(string text)
        {
            DisplayStyledText(text, 1000, TextStyle.MissionName);
        }

        public static void DisplayStyledText(string text, int time, TextStyle style)
        {
            DisplayStyledText(text, time, (int)style);
        }

        public static void DisplayStyledText(string text, int time, int type)
        {
            Internal.Function.Call(0x00ba, RegisterKey(text), time, type);
        }

        private static string RegisterKey(string value)
        {
            string key = "NET" + _curID.ToString("X4"); // unique identifier based on key
            CustomGXTs[key] = value;

            _curID++;

            // prevent overflow/full memory
            if (_curID >= 0xFFF)
            {
                _curID = 0;
            }

            return key;
        }
        #endregion
    }
}
