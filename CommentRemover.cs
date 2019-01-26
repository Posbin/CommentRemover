using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommentRemover
{
    class CommentRemover
    {
        private const string BeginStrOfSglLineCom = "//";
        private const string BeginStrOfMulLineCom = "/*";
        private const string EndStrOfMulLineCom = "*/";

        private string[] targetExtensions = null;
        private int rewrittenFiles = 0;
        private int removedLines = 0;
        private int removedPartOfLines = 0;

        public CommentRemover()
        {
            targetExtensions = new string[] { "*.cs" };
        }

        public void Remove(string path)
        {
            rewrittenFiles = 0;
            removedLines = 0;
            removedPartOfLines = 0;
            if (Directory.Exists(path))
            {
                RemoveForDirectory(path);
            }
            else if (File.Exists(path))
            {
                RemoveForFile(path);
            }
            else
            {
                Console.WriteLine("[WARN]: {0} is not exists.", path);
            }
        }

        private List<string> GetFilesList(string rootDir)
        {
            var filesList = new List<string>();
            foreach (string extension in targetExtensions)
            {
                string[] files = Directory.GetFiles(rootDir, extension, SearchOption.AllDirectories);
                filesList.AddRange(files);
            }
            return filesList;
        }

        private void RemoveForDirectory(string path)
        {
            Console.WriteLine("[Dir]: {0}", path);
            var files = GetFilesList(path);
            foreach (string file in files)
            {
                RemoveForOneFile(file);
            }
            Console.WriteLine("  {0} files rewritten. (included {1} files in directory.)", rewrittenFiles, files.Count);
            Console.WriteLine("  {0} lines removed.", removedLines);
            Console.WriteLine("  {0} lines removed a part.", removedPartOfLines);
        }

        private void RemoveForFile(string path)
        {
            bool isTargetExtension = targetExtensions.Contains("*" + Path.GetExtension(path));
            if (!isTargetExtension)
            {
                Console.WriteLine("[WARN]: The extension of {0} is out of target.", path);
                return;
            }

            Console.WriteLine("[File]: {0}", path);
            RemoveForOneFile(path);
            Console.WriteLine("  {0} lines removed.", removedLines);
            Console.WriteLine("  {0} lines removed a part.", removedPartOfLines);
        }

        private void RemoveForOneFile(string path)
        {
            string tmpFile = Path.GetTempFileName();
            using (StreamReader sr = new StreamReader(path))
            using (StreamWriter sw = new StreamWriter(tmpFile))
            {
                bool mulLinesCom = false;
                while (sr.Peek() > -1)
                {
                    string orgLine = sr.ReadLine();
                    string newLine = TrimComment(orgLine, ref mulLinesCom);
                    if (newLine == null)
                    {
                        removedLines++;
                    }
                    else
                    {
                        if (orgLine != newLine)
                        {
                            removedPartOfLines++;
                        }
                        sw.WriteLine(newLine);
                    }
                }
            }

            bool fileIsRewritten = (removedLines > 0 || removedPartOfLines > 0);
            if (fileIsRewritten)
            {
                rewrittenFiles++;
                File.Copy(tmpFile, path, true);
            }
            File.Delete(tmpFile);
        }

        private string TrimComment(string line, ref bool mulLinesCom)
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
                    string afterLineOfComIdx = orgLine.Remove(0, beginComIdx + BeginStrOfMulLineCom.Length);
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

        private string TrimMulLinesCom(string line, ref bool mulLinesCom)
        {
            int endIdx = GetEndIdxOfMulLinesCom(line, 0);
            if (endIdx >= 0)
            {
                mulLinesCom = false;
                return line.Remove(0, endIdx + 1);
            }
            return null;
        }

        private int GetBeginIdxOfComment(string line, ref bool mulLinesCom)
        {
            int beginIdxOfSglLineCom = GetBeginIdx(line, BeginStrOfSglLineCom);
            int beginIdxOfMulLineCom = GetBeginIdx(line, BeginStrOfMulLineCom);
            if (beginIdxOfSglLineCom >= 0 && beginIdxOfMulLineCom >= 0)
            {
                mulLinesCom = (beginIdxOfMulLineCom < beginIdxOfSglLineCom);
                return Math.Min(beginIdxOfSglLineCom, beginIdxOfMulLineCom);
            }
            else if (beginIdxOfSglLineCom >= 0)
            {
                return beginIdxOfSglLineCom;
            }
            else if (beginIdxOfMulLineCom >= 0)
            {
                mulLinesCom = true;
                return beginIdxOfMulLineCom;
            }
            return -1;
        }

        private int GetBeginIdx(string line, string beginStr)
        {
            var strFields = GetStringFields(line);
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

        private int GetEndIdxOfMulLinesCom(string line, int beginIdx)
        {
            for (int i = beginIdx; i < line.Length - 1; i++)
            {
                if (line.Substring(i, EndStrOfMulLineCom.Length) == EndStrOfMulLineCom)
                {
                    return i + 1;
                }
            }
            return -1;
        }

        // Key: begin index of string
        // Value: end index of string
        private Dictionary<int, int> GetStringFields(string line)
        {
            var dic = new Dictionary<int, int>();
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

        private int GetEndIdxOfString(string line, int beginIdx, bool isLiteral)
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

        private bool IsEscSeq(string line, int index, bool isLiteral)
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
