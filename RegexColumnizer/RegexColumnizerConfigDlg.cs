namespace LogExpert
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Text.RegularExpressions;

    public partial class RegexColumnizerConfigDlg : Form
    {
        private readonly RegexColumnizerConfig config;
        private string timestampField;
        private string defaultMessageField;

        public RegexColumnizerConfigDlg(RegexColumnizerConfig config)
        {
            this.config = config;
            this.InitializeComponent();
            this.regexText.Text = this.config.Regex.ToString();
            this.localTimeCheckBox.Checked = this.config.LocalTimestamps;
            this.timestampField = this.config.TimestampField;
            this.formatComboBox.Text = this.config.TimestampFormat;
            this.defaultMessageField = this.config.DefaultMessageField;
        }

        internal void Apply(RegexColumnizerConfig configuration)
        {
            configuration.Regex = new Regex(this.regexText.Text, RegexOptions.IgnoreCase);
            configuration.LocalTimestamps = this.localTimeCheckBox.Checked;
            configuration.TimestampField = this.timestampField;
            configuration.TimestampFormat = this.formatComboBox.Text;
            configuration.DefaultMessageField = this.defaultMessageField;

            var selectedFields = new string[this.listView1.CheckedItems.Count];

            for (var i = 0; i < selectedFields.Length; i++)
            {
                selectedFields[i] = this.listView1.CheckedItems[i].Text;
            }

            configuration.SelectedFields = selectedFields;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Apply(this.config);
        }

        private readonly Dictionary<string, ListViewItem> oldFields = new Dictionary<string, ListViewItem>();

        private void regexText_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                var regex = new Regex(this.regexText.Text, RegexOptions.IgnoreCase);

                foreach (ListViewItem item in listView1.Items)
                {
                    this.oldFields[item.Text] = item;
                }

                this.fieldComboBox.Items.Clear();
                this.fieldComboBox.Items.Add("<Select Timestamp>");

                this.defaultMessageFieldComboBox.Items.Clear();
                this.defaultMessageFieldComboBox.Items.Add("<Select Default Message Field>");

                foreach (var name in regex.GetGroupNames())
                {
                    int i;
                    if (int.TryParse(name, out i))
                    {
                        continue;
                    }

                    if (this.oldFields.ContainsKey(name))
                    {
                        var item = this.oldFields[name];

                        if (!this.listView1.Items.Contains(item))
                        {
                            this.listView1.Items.Add(item);
                        }
                    }
                    else
                    {
                        var item = new ListViewItem(name);
                        item.Checked = true;
                        item.Name = name;
                        this.listView1.Items.Add(item);
                    }

                    this.fieldComboBox.Items.Add(name);
                    this.defaultMessageFieldComboBox.Items.Add(name);

                    if (name.Equals(this.timestampField))
                    {
                        this.fieldComboBox.SelectedIndex = this.fieldComboBox.Items.Count - 1;
                    }

                    if (name.Equals(this.defaultMessageField))
                    {
                        this.defaultMessageFieldComboBox.SelectedIndex = this.defaultMessageFieldComboBox.Items.Count - 1;
                    }
                }

                if (this.fieldComboBox.SelectedIndex < 0)
                {
                    this.fieldComboBox.SelectedIndex = 0;
                }

                if (this.defaultMessageFieldComboBox.SelectedIndex < 0)
                {
                    this.defaultMessageFieldComboBox.SelectedIndex = 0;
                }

                foreach (ListViewItem item in this.listView1.Items)
                {
                    var groupNumber = regex.GroupNumberFromName(item.Text);

                    if (groupNumber < 0)
                    {
                        this.listView1.Items.Remove(item);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid Regular Expression");
                e.Cancel = true;
            }
        }

        private void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            //Begins a drag-and-drop operation in the ListView control.
            this.listView1.DoDragDrop(this.listView1.SelectedItems, DragDropEffects.Move);
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            var len = e.Data.GetFormats().Length - 1;
            
            for (var i = 0; i <= len; i++)
            {
                if (e.Data.GetFormats()[i].Equals("System.Windows.Forms.ListView+SelectedListViewItemCollection"))
                {
                    //The data from the drag source is moved to the target.	
                    e.Effect = DragDropEffects.Move;
                }
            }
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            //Return if the items are not selected in the ListView control.
            if (this.listView1.SelectedItems.Count == 0)
            {
                return;
            }

            //Returns the location of the mouse pointer in the ListView control.
            var cp = this.listView1.PointToClient(new Point(e.X, e.Y));
            
            //Obtain the item that is located at the specified location of the mouse pointer.
            var dragToItem = this.listView1.GetItemAt(cp.X, cp.Y);

            if (dragToItem == null)
            {
                return;
            }
            
            //Obtain the index of the item at the mouse pointer.
            var dragIndex = dragToItem.Index;
            var sel = new ListViewItem[this.listView1.SelectedItems.Count];

            for (var i = 0; i <= this.listView1.SelectedItems.Count - 1; i++)
            {
                sel[i] = this.listView1.SelectedItems[i];
            }

            for (var i = 0; i < sel.GetLength(0); i++)
            {
                //Obtain the ListViewItem to be dragged to the target location.
                var dragItem = sel[i];
                var itemIndex = dragIndex;

                if (itemIndex == dragItem.Index)
                {
                    return;
                }

                if (dragItem.Index < itemIndex)
                {
                    itemIndex++;
                }
                else
                {
                    itemIndex = dragIndex + i;
                }

                //Insert the item at the mouse pointer.
                var insertItem = (ListViewItem)dragItem.Clone();
                this.listView1.Items.Insert(itemIndex, insertItem);
                //Removes the item from the initial location while 
                //the item is moved to the new location.
                this.listView1.Items.Remove(dragItem);
            }
        }

        private void RegexColumnizerConfigDlg_Load(object sender, EventArgs e)
        {
            this.ValidateChildren();
        }

        private void fieldComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            this.formatComboBox.Enabled = this.localTimeCheckBox.Enabled = this.fieldComboBox.SelectedIndex > 0;
        }

        private void fieldComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.timestampField = this.fieldComboBox.SelectedIndex == 0
                ? string.Empty
                : this.fieldComboBox.SelectedItem.ToString();
        }

        private void defaultMessageFieldComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.defaultMessageField = this.defaultMessageFieldComboBox.SelectedIndex == 0
                ? string.Empty
                : this.defaultMessageFieldComboBox.SelectedItem.ToString();
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            this.Apply(this.config);
        }
    }
}
