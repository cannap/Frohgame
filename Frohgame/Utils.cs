﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using FROHGAME.Core;

/*
 * 
 * Author(s): Purm & cannap
 * 
 */

namespace FROHGAME {
    /// <summary>
    /// Alles was sonst nirgendwo hinpasst und keiner eigenen Klasse bedarf
    /// </summary>
    static public class Utils {
        /// <summary>
        /// Erstellt eine einfache HTML datei nur eine Temporäre Methode 
        /// </summary>
        /// <param name="sFileName">Dateiname ohne .html</param>
        /// <param name="HTMLCode">Quellcode</param>
        static public void HtmlDebug(string sFileName, string HTMLCode) {
            System.IO.StreamWriter myFile = new StreamWriter(sFileName + ".html");
            myFile.Write(HTMLCode);
            myFile.Close();
        }

        /// <summary>
        /// Zufällige Zahl
        /// </summary>
        /// <param name="min">minina</param>
        /// <param name="max">maxima</param>
        /// <returns></returns>
        static public int Random(int min, int max) {
            Random rnd = new Random();
            return rnd.Next(min, max);
        }

        /// <summary>
        /// Such mit Regex nach einem einzelnen Resultat
        /// </summary>
        /// <param name="searchString">Der zu durchsuchende String</param>
        /// <param name="pattern"String Pattern></param>
        /// <returns></returns>
        static public string SimpleRegex(string searchString, string pattern) {
            return SimpleRegex(searchString, pattern, RegexOptions.None);
        }

        /// <summary>
        /// Such mit Regex nach einem einzelnen Resultat
        /// </summary>
        /// <param name="SearchString">Der zu durchsuchende String</param>
        /// <param name="Pattern">String Pattern</param>
        /// <param name="options">Regex Optionen</param>
        /// <returns>result or null</returns>
        static public string SimpleRegex(string SearchString, string Pattern, RegexOptions options) {
            Regex reg = new Regex(Pattern, options);
            Match Result = new Regex(Pattern, options).Match(SearchString);
            if (Result.Groups.Count <= 1) {
                return null;
            } else {
                return Result.Groups[1].Value;
            }
        }

        /// <summary>
        /// entfernt alle zeichen ausser + und - und zahlen und konvertiert zu int
        /// </summary>
        /// <param name="StringToReplace">Der String der durchsucht werden soll</param>
        /// <param name="ReplacePattern">Die Pattern um die Zeichen zu finden um zu replacen</param>
        /// <returns>Int32</returns>
        static public int StringReplaceToInt32(string StringToReplace) {
            return Convert.ToInt32(new Regex("([^0-9-+]*)").Replace(StringToReplace, string.Empty));
        }

        /// <summary>
        /// KOnvertiert einen String in ein Bytearray
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static public byte[] StringToByteArray(string str) {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetBytes(str);
        }

        /// <summary>
        /// Konvertiert ein Bytearray in einen String
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        static public string ByteArrayToString(byte[] arr) {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetString(arr);
        }

    }
}
