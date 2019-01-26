using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommentRemover
{
    class CommentRemover
    {
        private const string BeginStrOfXmlTagsCom = "/// ";
        private const string BeginStrOfSglLineCom = "//";
        private const string BeginStrOfMulLineCom = "/*";
        private const string EndStrOfMulLineCom = "*/";

        private string[] targetExtensions = null;
        private bool removingXmlTags = false;
        private int rewrittenFiles = 0;
        private int rmLinesSum = 0;
        private int rmPartsSum = 0;

        public CommentRemover(bool removingXmlTags)
        {
            targetExtensions = new string[] { "*.cs" };
            this.removingXmlTags = removingXmlTags;
        }

        public void Remove(string path)
        {
            rewrittenFiles = 0;
            rmLinesSum = 0;
            rmPartsSum = 0;
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
            Console.WriteLine("  {0} lines removed.", rmLinesSum);
            Console.WriteLine("  {0} lines removed a part.", rmPartsSum);
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
            Console.WriteLine("  {0} lines removed.", rmLinesSum);
            Console.WriteLine("  {0} lines removed a part.", rmPartsSum);
        }

        private void RemoveForOneFile(string path)
        {
            int rmLines = 0;
            int rmParts = 0;
            string tmpFile = Path.GetTempFileName();
            using (StreamReader sr = new StreamReader(path))
            using (StreamWriter sw = new StreamWriter(tmpFile))
            {
                bool mulLineCom = false;
                while (sr.Peek() > -1)
                {
                    string orgLine = sr.ReadLine();
                    string newLine = TrimComment(orgLine, ref mulLineCom);
                    if (newLine == null)
                    {
                        rmLines++;
                    }
                    else
                    {
                        if (orgLine != newLine)
                        {
                            rmParts++;
                        }
                        sw.WriteLine(newLine);
                    }
                }
            }

            bool fileIsRewritten = (rmLines > 0 || rmParts > 0);
            if (fileIsRewritten)
            {
                rewrittenFiles++;
                File.Copy(tmpFile, path, true);
            }
            File.Delete(tmpFile);

            rmLinesSum += rmLines;
            rmPartsSum += rmParts;
        }

        private string TrimComment(string line, ref bool mulLineCom)
        {
            if (mulLineCom)
            {
                line = TrimMulLineCom(line, ref mulLineCom);
                if (line == null || line.Trim().Length == 0)
                {
                    // The line is comment only.
                    return null;
                }
            }
            int beginComIdx = 0;
            if (HasBeginIdxOfComment(line, ref mulLineCom, ref beginComIdx))
            {
                string orgLine = line;
                line = orgLine.Remove(beginComIdx);
                if (mulLineCom)
                {
                    string afterLineOfComIdx = orgLine.Remove(0, beginComIdx + BeginStrOfMulLineCom.Length);
                    line += TrimComment(afterLineOfComIdx, ref mulLineCom);
                }
                if (line.Trim().Length == 0)
                {
                    // If trimmed line is empty, the line won't be outputed.
                    return null;
                }
            }
            return line;
        }

        private string TrimMulLineCom(string line, ref bool mulLineCom)
        {
            int endIdx = 0;
            if (HasEndIdxOfMulLineCom(line, ref endIdx))
            {
                mulLineCom = false;
                return line.Remove(0, endIdx + 1);
            }
            return null;
        }

        private bool HasBeginIdxOfComment(string line, ref bool mulLineCom, ref int beginIdx)
        {
            var strFields = GetStringFields(line);
            for (int i = 0; i <= line.Length; i++)
            {
                // skip string field
                if (strFields.Keys.Contains(i))
                {
                    i = strFields[i];
                    continue;
                }

                // XML tags comments
                if (IsBeginIndex(line, i, BeginStrOfXmlTagsCom))
                {
                    if (removingXmlTags)
                    {
                        beginIdx = i;
                        return true;
                    }
                    break;
                }
                // Single line comments
                if (IsBeginIndex(line, i, BeginStrOfSglLineCom))
                {
                    beginIdx = i;
                    return true;
                }
                // Multiple line comments
                if (IsBeginIndex(line, i, BeginStrOfMulLineCom))
                {
                    mulLineCom = true;
                    beginIdx = i;
                    return true;
                }
            }
            return false;
        }

        private bool IsBeginIndex(string line, int index, string str)
        {
            return (index <= line.Length - str.Length) && (line.Substring(index, str.Length) == str);
        }

        private bool HasEndIdxOfMulLineCom(string line, ref int endIdx)
        {
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (line.Substring(i, EndStrOfMulLineCom.Length) == EndStrOfMulLineCom)
                {
                    endIdx = i + 1;
                    return true;
                }
            }
            return false;
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
