namespace LogExpert
{
    using System;
    using System.Text.RegularExpressions;
    using System.ComponentModel;

    [Serializable]
    public class RegexColumnizerConfig
    {
        [DefaultValue("true")]
        public bool LocalTimestamps { get; set; }

        public Regex Regex { get; set; }

        [DefaultValue("")]
        public string TimestampField { get; set; }

        [DefaultValue("Test")]
        public string TimestampFormat { get; set; }

        public string[] SelectedFields { get; set; }

        public string DefaultMessageField { get; set; }

        public RegexColumnizerConfig()
        {
            this.Regex = new Regex("(?<one>)(?<two>)(?<three>)(?<four>)", RegexOptions.IgnoreCase);
            this.SelectedFields = new string[0];
            this.TimestampField = "";
            this.TimestampFormat = "";
            this.DefaultMessageField = "";
        }

        internal bool IsValid()
        {
            return
                this.Regex != null
                && this.SelectedFields != null
                && this.TimestampField != null
                && this.TimestampFormat != null
                && this.DefaultMessageField != null;
        }
    }
}
