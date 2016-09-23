namespace LogExpert
{
    using System;
    using System.Text;
    using System.Globalization;
    using System.Windows.Forms;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;
    using System.Security.Cryptography;

    public class RegexColumnizer : IColumnizerConfigurator, ILogLineColumnizer, IInitColumnizer
    {
        ///// <summary>
        ///// Implement this property to let LogExpert display the name of the Columnizer
        ///// in its Colummnizer selection dialog.
        ///// </summary>
        public string Text
        {
            get { return this.GetName(); }
        }

        private RegexColumnizerConfig config = new RegexColumnizerConfig();
        private string name = string.Empty;

        protected int TimeOffset;
        protected string ConfigDir;

        #region IColumnizerConfigurator Members

        public void Configure(ILogLineColumnizerCallback callback, string configDir)
        {
            var configPath = configDir + @"\Regexcolumnizer-" + this.name + "." + ".dat";

            var configDialog = new RegexColumnizerConfigDlg(this.config);

            if (configDialog.ShowDialog() == DialogResult.OK)
            {
                configDialog.Apply(this.config);
                using (var fs = new FileStream(configPath, FileMode.Create, FileAccess.Write))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fs, this.config);
                }
            }
        }

        public void LoadConfig(string configDir)
        {
            this.ConfigDir = configDir;
        }

        #endregion

        #region ILogLineColumnizer Members

        public int GetColumnCount()
        {
            return this.config.SelectedFields.Length;
        }

        public string[] GetColumnNames()
        {
            return this.config.SelectedFields;
        }

        public string GetDescription()
        {
            return "Improved Regex Columnizer";
        }

        public string GetName()
        {
            return "ImprovedRegexColumnizer";
        }

        public int GetTimeOffset()
        {
            return this.TimeOffset;
        }

        public DateTime GetTimestamp(ILogLineColumnizerCallback callback, string line)
        {
            var match = this.config.Regex.Match(line);

            DateTime timestamp;

            if (this.config.TimestampField.Length == 0 
                || this.config.TimestampFormat.Length == 0 
                || !match.Success 
                || !match.Groups[this.config.TimestampField].Success 
                || !DateTime.TryParseExact(
                        match.Groups[this.config.TimestampField].Value, 
                        this.config.TimestampFormat, 
                        CultureInfo.InvariantCulture, 
                        DateTimeStyles.None, out timestamp))
            {
                return DateTime.MinValue;
            }

            if (this.config.LocalTimestamps)
            { 
                timestamp = timestamp.ToLocalTime();
            }

            timestamp = timestamp.AddMilliseconds(this.TimeOffset);
            
            return timestamp;
        }

        public bool IsTimeshiftImplemented()
        {
            return true;
        }

        public void PushValue(ILogLineColumnizerCallback callback, int column, string value, string oldValue)
        {
            if (column >= 0 
                && column < this.GetColumnCount() 
                && this.GetColumnNames()[column].Equals(this.config.TimestampField))
            {
                try
                {
                    var newDateTime = DateTime.ParseExact(value, this.config.TimestampFormat, CultureInfo.InvariantCulture);
                    var oldDateTime = DateTime.ParseExact(oldValue, this.config.TimestampFormat, CultureInfo.InvariantCulture);

                    var oldSeconds = oldDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                    var newSeconds = newDateTime.Ticks / TimeSpan.TicksPerMillisecond;

                    this.TimeOffset = (int)(newSeconds - oldSeconds);
                }
                catch (FormatException)
                { }
            }
        }

        public void SetTimeOffset(int offsetSeconds)
        {
            this.TimeOffset = offsetSeconds;
        }

        public string[] SplitLine(ILogLineColumnizerCallback callback, string line)
        {
            var match = this.config.Regex.Match(line);

            return match.Success 
                ? this.SplitLinesToColumns(callback, line, match) 
                : this.LineToDefaultColumn(line);
        }

        private string[] SplitLinesToColumns(ILogLineColumnizerCallback callback, string line, Match match)
        {
            var timeStamp = this.GetTimestamp(callback, line);

            var columns = this.GetColumnNames()
                .Select(columnName =>
                {
                    var matchGroup = match.Groups[columnName];

                    if (this.config.TimestampField.Equals(columnName)
                        && timeStamp != DateTime.MinValue)
                    {
                        return timeStamp.ToString(this.config.TimestampFormat);
                    }

                    return matchGroup.Success 
                        ? matchGroup.Value 
                        : string.Empty;
                })
                .ToArray();

            return columns;
        }

        private string[] LineToDefaultColumn(string line)
        {
            var columns = new string[this.GetColumnCount()];

            var defaultColumnIndex = Array.IndexOf(this.GetColumnNames(), this.config.DefaultMessageField);

            if (defaultColumnIndex < 0 
                || defaultColumnIndex >= columns.Length)
            {
                return columns;
            }

            columns[defaultColumnIndex] = line;

            return columns;
        }

        #endregion

        #region IInitColumnizer Members

        public void DeSelected(ILogLineColumnizerCallback callback)
        {
            //throw new NotImplementedException();
        }

        public void Selected(ILogLineColumnizerCallback callback)
        {
            var fileInfo = new FileInfo(callback.GetFileName());

            this.name = BitConverter.ToString(new MD5CryptoServiceProvider()
                .ComputeHash(Encoding.Unicode.GetBytes(fileInfo.FullName)))
                .Replace("-", "")
                .ToLower();

            var configPath = this.ConfigDir + @"\Regexcolumnizer-" + this.name + "." + ".dat";

            if (!File.Exists(configPath))
            {
                this.config = new RegexColumnizerConfig();
            }
            else
            {
                using (var fs = File.OpenRead(configPath))
                {
                    try
                    {
                        var formatter = new BinaryFormatter();

                        var configuration = (RegexColumnizerConfig)formatter.Deserialize(fs);

                        if (configuration.IsValid())
                        {
                            this.config = configuration;
                        }
                    }
                    catch (SerializationException e)
                    {
                        MessageBox.Show(e.Message, "Deserialize");
                        this.config = new RegexColumnizerConfig();
                    }
                }
            }
        }

        #endregion
    }
}