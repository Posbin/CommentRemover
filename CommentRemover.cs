using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommentRemover
{
    static class CommentRemover
    {
        private const string BeginStrOfOneLineCom = "//";
        private const string BeginStrOfMulLinesCom = "/*";
        private const string EndStrOfMulLinesCom = "*/";

        public static void Remove(string rootFolder, string[] targetExtensions)
        {
            List<string> files = GetFilesList(rootFolder, targetExtensions);
            foreach (string file in files)
            {
                RemoveCommentOfOneFile(file);
            }
        }

        private static List<string> GetFilesList(string rootFolder, string[] targetExtensions)
        {
            List<string> filesList = new List<string>();
            foreach (string extension in targetExtensions)
            {
                string[] files = Directory.GetFiles(rootFolder, extension, SearchOption.AllDirectories);
                filesList.AddRange(files);
            }
            return filesList;
        }

        private static void RemoveCommentOfOneFile(string file)
        {
                string tmpFile = Path.GetTempFileName();
                StreamReader sr = new StreamReader(file);
                StreamWriter sw = new StreamWriter(tmpFile);
                bool mulLinesCom = false;
                while (sr.Peek() > -1)
                {
                    string line = TrimComment(sr.ReadLine(), ref mulLinesCom);
                    if (line != null)
                    {
                        sw.WriteLine(line.TrimEnd(' '));
                    }
                }
                sr.Close();
                sw.Close();
                File.Copy(tmpFile, file, true);
                File.Delete(tmpFile);
        }

        private static string TrimComment(string line, ref bool mulLinesCom)
        {
            if (mulLinesCom)
            {
                line = TrimMulLinesCom(line, ref mulLinesCom);
                if (line == null || line.Trim().Length == 0)
                {
                    // The line is comment only.
                    return null;
                }
            }
            int beginComIdx = GetBeginIdxOfComment(line, ref mulLinesCom);
            if (beginComIdx >= 0)
            {
                string orgLine = line;
                line = orgLine.Remove(beginComIdx);
                if (mulLinesCom)
                {
                    string afterLineOfComIdx = orgLine.Remove(0, beginComIdx + BeginStrOfMulLinesCom.Length);
                    line += TrimComment(afterLineOfComIdx, ref mulLinesCom);
                }
                if (line.Trim().Length == 0)
                {
                    // If trimmed line is empty, the line won't be outputed.
                    return null;
                }
            }
            return line;
        }

        private static string TrimMulLinesCom(string line, ref bool mulLinesCom)
        {
            int endIdx = GetEndIdxOfMulLinesCom(line, 0);
            if (endIdx >= 0)
            {
                mulLinesCom = false;
                return line.Remove(0, endIdx + 1);
            }
            return null;
        }

        private static int GetBeginIdxOfComment(string line, ref bool mulLinesCom)
        {
            int beginIdxOfOneLineCom = GetBeginIdx(line, BeginStrOfOneLineCom);
            int beginIdxOfMulLinesCom = GetBeginIdx(line, BeginStrOfMulLinesCom);
            if (beginIdxOfOneLineCom >= 0 && beginIdxOfMulLinesCom >= 0)
            {
                return Math.Min(beginIdxOfOneLineCom, beginIdxOfMulLinesCom);
            }
            else if (beginIdxOfOneLineCom >= 0 && beginIdxOfMulLinesCom < 0)
            {
                return beginIdxOfOneLineCom;
            }
            else if (beginIdxOfOneLineCom < 0 && beginIdxOfMulLinesCom >= 0)
            {
                mulLinesCom = true;
                return beginIdxOfMulLinesCom;
            }
            return -1;
        }

        private static int GetBeginIdx(string line, string beginStr)
        {
            Dictionary<int, int> strFields = GetStringFields(line);
            for (int i = 0; i <= line.Length - beginStr.Length; i++)
            {
                // skip string field
                if (strFields.Keys.Contains(i))
                {
                    i = strFields[i];
                    continue;
                }
                if (line.Substring(i, beginStr.Length) == beginStr)
                {
                    return i;
                }
            }
            return -1;
        }

        private static int GetEndIdxOfMulLinesCom(string line, int beginIdx)
        {
            for (int i = beginIdx; i < line.Length - 1; i++)
            {
                if (line.Substring(i, EndStrOfMulLinesCom.Length) == EndStrOfMulLinesCom)
                {
                    return i + 1;
                }
            }
            return -1;
        }

        // Key: begin index of string
        // Value: end index of string
        private static Dictionary<int, int> GetStringFields(string line)
        {
            Dictionary<int, int> dic = new Dictionary<int, int>();
            int i = 0;
            int beginIdx = 0;
            while ((beginIdx = line.IndexOf('"', i)) > 0)
            {
                bool isLiteral = (beginIdx > 0 && line[beginIdx - 1] == '@');
                int endIdx = GetEndIdxOfString(line, beginIdx, isLiteral);
                if (line.Length <= endIdx)
                {
                    break;
                }
                dic.Add(beginIdx, endIdx);
                i = endIdx + 1;
            }
            return dic;
        }

        private static int GetEndIdxOfString(string line, int beginIdx, bool isLiteral)
        {
            int i = beginIdx + 1;
            while (i < line.Length)
            {
                if (IsEscSeq(line, i, isLiteral))
                {
                    i += 2;
                    continue;
                }
                if (line[i] == '"')
                {
                    return i;
                }
                i++;
            }
            return i;
        }

        private static bool IsEscSeq(string line, int index, bool isLiteral)
        {
            bool isEscSeq = false;
            if (index < line.Length - 1)
            {
                isEscSeq |= line.Substring(index, 2) == "\\\"";
                isEscSeq |= isLiteral && line.Substring(index, 2) == "\"\"";
            }
            return isEscSeq;
        }
    }
}