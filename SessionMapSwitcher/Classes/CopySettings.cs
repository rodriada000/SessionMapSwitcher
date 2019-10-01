
using System.Collections.Generic;

class CopySettings
{
    public bool IsMovingFiles { get; set; }
    public bool CopySubFolders { get; set; }
    public List<string> ExcludeFolders { get; set; }
    public List<string> ExcludeFiles { get; set; }
}