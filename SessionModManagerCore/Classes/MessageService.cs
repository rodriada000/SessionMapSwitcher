using System;
using System.Collections.Generic;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public class MessageService
    {
        private static MessageService _instance;

        public delegate void ReceiveMessageDelegate(string message);

        public event ReceiveMessageDelegate MessageReceived;

        public static MessageService Instance 
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MessageService();
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public void ShowMessage(string message)
        {
            MessageReceived?.Invoke(message);
        }
    }
}
