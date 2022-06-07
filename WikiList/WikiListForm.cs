using System.Collections;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text.RegularExpressions;

namespace WikiList
{
    public partial class WikiListForm : Form
    {
        public WikiListForm()
        {
            InitializeComponent();

            // DIWhy title bar
            setupTitleBar();

            // Load Categories from file. Add to Combobox
            comboBoxItems = Properties.Resources.Categories.Split('\n');

            // Setup Listview columns and change view.
            listView.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView.Columns.Add("Category", -2, HorizontalAlignment.Left);
            listView.View = View.Details;

            //Load default values from bin file
            // Convert byte array into stream and Deserialize
            using (MemoryStream ms = new MemoryStream(Properties.Resources.defaultValues))
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                Wiki = (List<Information>)bformatter.Deserialize(ms);
            }

            display();
        }

        private List<Information> Wiki = new List<Information>();
        private Information selectedItem;
        private string[] comboBoxItems;
        private byte state = 128;

        // Variables to allow window movement
        private bool drag = false;
        private Point startPoint = new Point(0, 0);

        private string runPath = Path.GetDirectoryName(Application.ExecutablePath);
        private string defName = "WikiList";

        private void WikiListForm_Load(object sender, EventArgs e)
        {
            comboBoxCat.DataSource = comboBoxItems;
            comboBoxCat.SelectedIndex = -1;
            state = 0;
            lockButtons(96);
        }



        #region Buttons

        // delete selected items
        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (askForConfirm("Delete " + listView.SelectedItems.Count + " item/s?", "Confirm Deletion"))
            {
                int count = 0;
                foreach (ListViewItem item in listView.SelectedItems)
                {
                    Wiki.Remove((Information)item.Tag);
                    count++;
                }
                if (count != 1)
                    StatusStripLabel.Text = "Removed " + count + " Items";
                else
                    StatusStripLabel.Text = "Removed " + selectedItem.gsName;

                clearCtl();
                display();
                lockButtons(state & 240);
            }
            else
            {
                StatusStripLabel.Text = "Did not delete item/s";
            }
        }

        private void buttonEdit_Click(object sender, EventArgs e)
        {
            int index = Wiki.FindIndex(i => i.gsName.Equals(selectedItem.gsName));
            // Could set values manually. Easier to assign.
            Information temp = getInfo();
            Wiki[index] = temp;
            display();
            listView.SelectedItems.Clear();
            listView.FindItemWithText(temp.gsName).Selected = true;
            StatusStripLabel.Text = "Edited " + selectedItem.gsName;
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            var temp = getInfo();
            bool confirm = true;
            if (string.IsNullOrWhiteSpace(temp.gsDescription))
            {
                confirm = askForConfirm("Add Item Without Description?", "Confirm Incomplete");
            }
            if (confirm)
            {
                Wiki.Add(temp);
                clearCtl();
                display();
                lockButtons(state & 240);
                StatusStripLabel.Text = "Added " + temp.gsName;
            }
            else
                StatusStripLabel.Text = "Did not add item";
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            int index = Wiki.BinarySearch(new Information(textBoxSearch.Text, null, null, null));
            if (index < 0)
            {
                StatusStripLabel.Text = "Could not find " + textBoxSearch.Text;
                textBoxSearch.Clear();
            }
            else
            {
                textBoxSearch.Clear();
                display();
                listView.SelectedItems.Clear();
                listView.Items[index].Selected = true;
                StatusStripLabel.Text = "Found " + textBoxSearch.Text;
            }
            textBoxSearch.Focus();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Bin files (*.bin)|*.bin";
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;
            openFile.InitialDirectory = runPath;
            openFile.FileName = defName + ".bin";
            DialogResult selectionResult = openFile.ShowDialog();
            if (selectionResult == DialogResult.OK)
            {
                try
                {
                    using (Stream stream = File.Open(openFile.FileName, FileMode.Open))
                    {
                        BinaryFormatter bformatter = new BinaryFormatter();
                        Wiki = (List<Information>)bformatter.Deserialize(stream);
                    }
                    StatusStripLabel.Text = "Opened: " + openFile.FileName;
                    display();
                    lockButtons(state | 16);
                }
                catch (Exception ex) when (ex is IOException || ex is SerializationException || ex is ArgumentNullException ||
                                           ex is SecurityException || ex is NotSupportedException)
                {
                    MessageBox.Show("Could not open" + openFile.FileName);
                }
            }
            else
            {
                StatusStripLabel.Text = "Did not open: Cancelled";
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Bin files (*.bin)|*.bin";
            saveFile.CheckPathExists = true;
            saveFile.InitialDirectory = runPath;
            saveFile.FileName = defName + ".bin";
            DialogResult result = saveFile.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    using (Stream stream = File.Open(saveFile.FileName, FileMode.Create))
                    {
                        BinaryFormatter bformatter = new BinaryFormatter();
                        bformatter.Serialize(stream, Wiki);
                    }
                    StatusStripLabel.Text = "Saved to:" + saveFile.FileName;
                    lockButtons(state & 239);
                }
                catch (Exception ex) when (ex is IOException || ex is SerializationException || ex is ArgumentNullException ||
                                           ex is SecurityException || ex is NotSupportedException)
                {
                    MessageBox.Show("Could not save " + saveFile.FileName);
                }
            }
            else
            {
                StatusStripLabel.Text = "Did not save: Cancelled";
            }
        }

        #endregion

        #region other events

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If only 1 item selected, allow editing
            if (listView.SelectedItems.Count == 1)
            {
                lockButtons((state | 2) & 242);
                selectedItem = (Information)listView.SelectedItems[0].Tag;
                setControls(selectedItem);

            }
            else if (listView.SelectedItems.Count != 0)
            {
                lockButtons(state | 10);
                selectedItem = null;
                clearCtl();
            }
            else
            {
                lockButtons(state & 240);
                selectedItem = null;
                clearCtl();
            }
        }

        private void textBoxName_DoubleClick(object sender, EventArgs e)
        {
            clearCtl();
            lockButtons(state & 240);
            StatusStripLabel.Text = "Cleared controls";
        }

        private void textBoxSearch_TextChanged(object sender, EventArgs e)
        {
            // display to filter items
            display();
            if (!string.IsNullOrEmpty(textBoxSearch.Text))
            {
                // Deselect in listview
                listView.SelectedItems.Clear();
                // Force update
                listView_SelectedIndexChanged(sender, e);
                // Enable search button
                lockButtons(state | 16);
            }
            else
            {   // Disable search button
                lockButtons(state & 239);
            }
        }
        #endregion

        #region Input Handling

        // Enable/Disable buttons depending on state
        private void lockButtons(int state)
        {
            if ((this.state & 128) == 0)
            {
                this.state = (byte)(state);
                BitArray bits = new BitArray(BitConverter.GetBytes(state).ToArray());
                buttonAdd.Enabled = bits[0];
                buttonDelete.Enabled = bits[1];
                buttonEdit.Enabled = bits[2] & !bits[3];
                buttonSearch.Enabled = bits[4];
                loadToolStripMenuItem.Enabled = bits[5];
                saveToolStripMenuItem.Enabled = bits[6];
            }
        }

        // Handle changing controls.
        private void ctrl_changed(object sender, EventArgs e)
        {
            // When Editing/deleting
            if (listView.SelectedItems.Count == 1 && (state & 8) == 0 && (state & 128) == 0)
            {

                if (!string.IsNullOrWhiteSpace(textBoxName.Text) &&
                    !string.IsNullOrWhiteSpace(comboBoxCat.Text) &&
                    getRadioValue() != null &&
                    !compareInfo(selectedItem, getInfo()) &&
                    ValidName((List<Information>)Wiki.Where(i => !i.gsName.Equals(selectedItem.gsName)).ToList(), textBoxName.Text.Trim())
                    )
                {
                    lockButtons((state | 4) & 253); // Enable edit if different
                }
                else if (!compareInfo(selectedItem, getInfo()))
                {
                    lockButtons(state & 240); // Disable edit if insufficent values
                    StatusStripLabel.Text = "Cannot Edit. Fix Required values";
                }
                    
                if (compareInfo(selectedItem, getInfo()))
                {
                    lockButtons((state | 2) & 251); // Only enable delete if no values changed
                }
            }
            // when adding
            else if ((state & 128) == 0)
            {
                // If name, category and structure defined
                if (!string.IsNullOrWhiteSpace(textBoxName.Text) &&
                    !string.IsNullOrWhiteSpace(comboBoxCat.Text) &&
                    getRadioValue() != null)
                {
                    if (ValidName(textBoxName.Text)) // Enable add if unique name
                        lockButtons(state | 1);
                    else
                    {
                        lockButtons(state & 240); // Disable add and warn user
                        StatusStripLabel.Text = "Cannot add duplicate names";
                    }
                }
            }
        }

        private void handleInput(object sender, KeyPressEventArgs e)
        {
            string allowedChars = "\b \".,\'!%()[]{}";
            // Handle everything in comboBox.
            if (sender == comboBoxCat)
                e.Handled = true;
            // Allow backspaces and spaces
            else if (sender == textBoxName || sender == textBoxSearch)
                if (!char.IsLetterOrDigit(e.KeyChar) && !allowedChars.Substring(0, 2).Contains(e.KeyChar))
                    e.Handled = true;
            else
                if (!char.IsLetterOrDigit(e.KeyChar) && !allowedChars.Contains(e.KeyChar))
                    e.Handled = true;
        }

        #endregion

        #region helpers

        // Compares values of two Information objects. If the same returns true
        private bool compareInfo(Information one, Information two)
        {
            return (one.gsName.Equals(two.gsName)
                && one.gsIsLinear.Equals(two.gsIsLinear)
                && one.gsCategory.Equals(two.gsCategory)
                && one.gsDescription.Equals(two.gsDescription));
        }

        //Removes carrige return and new line characters. Couldn't figure out how to get text without these characters.
        private string cleanText(string t)
        {
            return Regex.Replace(t, "['\r''\n']+", "").Trim();
        }

        private void display()
        {
            Wiki.Sort();
            listView.Items.Clear();
            Wiki.ForEach(i =>
            {
                var temp = new ListViewItem(new String[] { i.gsName, i.gsCategory });
                temp.Tag = i;
                if ((string.IsNullOrWhiteSpace(textBoxSearch.Text)) || (i.gsName.Contains(textBoxSearch.Text)))
                {
                    listView.Items.Add(temp);
                }
            });
        }


        private bool ValidName(string name)
        {
            return ValidName(Wiki, name);
        }
        private bool ValidName(List<Information> list, string name)
        {
            return !list.Exists(i => i.gsName.Equals(name.Trim()));
        }

        private bool askForConfirm(string message, string title)
        {
            var Result = MessageBox.Show(message, title, MessageBoxButtons.YesNo);
            if (Result == DialogResult.Yes)
                return true;
            else
                return false;
        }

        private Information getInfo()
        {
            return new Information(
                cleanText(textBoxName.Text),
                getRadioValue(),
                cleanText(comboBoxCat.Text),
                cleanText(textBoxDef.Text.Trim()));
        }

        private void setControls(Information i)
        {
            state |= 128;
            textBoxName.Text = i.gsName;
            comboBoxCat.Text = i.gsCategory;
            setRadioValue(convertRadioNameToIndex(i.gsIsLinear));
            textBoxDef.Text = i.gsDescription;
            state &= 127;

        }

        // Clear Controls
        private void clearCtl()
        {
            state |= 128;
            textBoxName.Clear();
            comboBoxCat.ResetText();
            setRadioValue(-1);
            textBoxDef.Clear();
            state &= 127;

        }

        // Useless requirement. 
        private string getRadioValue()
        {
            var s = groupBox.Controls.OfType<RadioButton>().ToArray();
            foreach (var radio in s)
            {
                if (radio.Checked == true)
                {
                    return radio.Text;
                }
            }
            return null;

        }

        private void setRadioValue(int index)
        {
            var radioButtons = groupBox.Controls.OfType<RadioButton>().ToArray();
            for (int i = 0; i < radioButtons.Length; i++)
            {
                if (i == index)
                {
                    radioButtons[i].Checked = true;
                }
                else
                {
                    radioButtons[i].Checked = false;
                }
            }
        }

        private int convertRadioNameToIndex(string n)
        {
            switch (n)
            {
                case "Linear":
                    return 0;
                case "Non-Linear":
                    return 1;
            }
            return -1;
        }

        #endregion

        #region custom TitleBar

        void setupTitleBar()
        {
            // Custom Titlebar events to make window movable
            titleBar.MouseDown += new MouseEventHandler(TitleMouseDown);
            titleBar.MouseUp += new MouseEventHandler(TitleMouseUp);
            titleBar.MouseMove += new MouseEventHandler(TitleMouseMove);

            label5.MouseDown += new MouseEventHandler(TitleMouseDown);
            label5.MouseUp += new MouseEventHandler(TitleMouseUp);
            label5.MouseMove += new MouseEventHandler(TitleMouseMove);

            // Events for close x. Changes image on hover and closes on click
            picClose.MouseEnter += new EventHandler(ExitMouseEnter);
            picClose.MouseLeave += new EventHandler(ExitMouseLeave);
            picClose.MouseClick += new MouseEventHandler(titleButtonMouseClick);
            // Events for maximise button. Changes image on hover and maximises/restores
            picMax.MouseEnter += new EventHandler(MaxMouseEnter);
            picMax.MouseLeave += new EventHandler(MaxMouseLeave);
            picMax.MouseClick += new MouseEventHandler(titleButtonMouseClick);
            // Events for minimise button. Changes image on hover and minimises on click
            picMin.MouseEnter += new EventHandler(MinMouseEnter);
            picMin.MouseLeave += new EventHandler(MinMouseLeave);
            picMin.MouseClick += new MouseEventHandler(titleButtonMouseClick);
        }

        void MaxMouseEnter(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                picMax.Image = Properties.Resources.maxReturnHover;
            }
            else
            {
                picMax.Image = Properties.Resources.maxHover;
            }
        }

        void MaxMouseLeave(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                picMax.Image = Properties.Resources.maxReturn;
            }
            else
            {
                picMax.Image = Properties.Resources.max;
            }
        }

        void ExitMouseEnter(object sender, EventArgs e)
        {
            picClose.Image = Properties.Resources.closeHover;
        }

        void ExitMouseLeave(object sender, EventArgs e)
        {
            picClose.Image = Properties.Resources.close;
        }

        void MinMouseEnter(object sender, EventArgs e)
        {
            picMin.Image = Properties.Resources.minHover;
        }

        void MinMouseLeave(object sender, EventArgs e)
        {
            picMin.Image = Properties.Resources.min;
        }

        void TitleMouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
            Cursor = Cursors.Default;

        }

        void TitleMouseDown(object sender, MouseEventArgs e)
        {
            startPoint = e.Location;
            drag = true;
        }

        private void titleButtonMouseClick(object sender, MouseEventArgs e)
        {
            if (sender.Equals(picClose))
                this.Close(); // close the form
            else if (sender.Equals(this.picMax))
            {
                if (WindowState == FormWindowState.Maximized)
                {
                    WindowState = FormWindowState.Normal;
                }
                else
                {
                    WindowState = FormWindowState.Maximized;
                }
            }
            else
                this.WindowState = FormWindowState.Minimized;
        }

        void TitleMouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
            {
                Cursor = Cursors.SizeAll;
                Point p1 = new Point(e.X, e.Y);
                Point p2 = PointToScreen(p1);
                Point p3 = new Point(p2.X - startPoint.X,
                                     p2.Y - startPoint.Y);
                Location = p3;
            }
        }

        // adds drop shadow
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;
                return cp;
            }

        }

        #endregion

        
    }
}