using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace AmiyaBotMaaAdapter.Helpers
{
    public class Logger
    {
        public class OnLogEventArgs:EventArgs
        {
            public OnLogEventArgs(string level, string message,  DateTime dateTime)
            {
                Message = message;
                Level = level;
                DateTime = dateTime;
            }

            public String Message { get;  }
            public String Level { get; }
            public DateTime DateTime { get; }
        }

        public static Logger Current { get;  } = new Logger();

        public event EventHandler<OnLogEventArgs> OnLog;

        public Logger()
        {
            OnLog += Logger_OnLog;
        }

        private void Logger_OnLog(object sender, OnLogEventArgs e)
        {
            lock (this)
            {
                using var fileStream = new FileStream("log.txt", FileMode.Append, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(fileStream);

                writer.WriteLine($"[{e.DateTime:s}][{e.Level,-10}]{e.Message}");
            }
        }

        public void Info(string message)
        {
            OnLog?.Invoke(this,new OnLogEventArgs("Info",message,DateTime.Now));
        }

        public void Report(string message)
        {
            OnLog?.Invoke(this, new OnLogEventArgs("Warning", message, DateTime.Now));
        }

        public void Critical(string message)
        {
            OnLog?.Invoke(this, new OnLogEventArgs("Critical", message, DateTime.Now));
        }

        public string FormatException(Exception exp, string prompt)
        {
            return prompt+exp.Message;
        }
    }
}
