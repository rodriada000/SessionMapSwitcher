
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class CopySettings
{
    public bool IsMovingFiles { get; set; }
    public bool CopySubFolders { get; set; }
    public List<string> ExcludeFolders { get; set; }
    public List<string> ExcludeFiles { get; set; }
    public bool ContainsSearchForFiles { get; set; }

    internal bool ExcludeFile(FileInfo file)
    {
        if (ContainsSearchForFiles)
        {
            return ExcludeFiles.Any(f => file.Name.Contains(f));
        }
        else
        {
            return ExcludeFiles.Contains(file.Name);
        }
    }
}