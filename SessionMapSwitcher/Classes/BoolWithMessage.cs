using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes
{
    /// <summary>
    /// Used to return true or false and a string like an exception message.
    /// </summary>
    class BoolWithMessage
    {
        public bool Result { get; set; }
        public string Message { get; set; }

        public BoolWithMessage(bool result, string message)
        {
            Result = result;
            Message = message;
        }

        public BoolWithMessage(bool result)
        {
            Result = result;
            Message = "";
        }
    }
}
