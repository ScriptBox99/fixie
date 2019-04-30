﻿namespace Fixie.TestAdapter
{
    public class SourceLocation
    {
        public SourceLocation(string codeFilePath, int lineNumber)
        {
            CodeFilePath = codeFilePath;
            LineNumber = lineNumber;
        }

        public string CodeFilePath { get; }
        public int LineNumber { get; }
    }
}