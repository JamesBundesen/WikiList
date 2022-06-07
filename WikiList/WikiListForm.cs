using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text.RegularExpressions;


//      WikiList            08/06/2022         JB 30038531
//      ----------------------------------------------------
//      Uses List<Information> of items. Adds that list to a listView. 
//      File/IO to binary file (user defined). Saves to WikiList.bin 
//      automatically when closing.


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

        // 6.2
        private List<Information> Wiki = new List<Information>();
        // Currently selected item. Probably redundant. Should be replaced with listview.SelectedItems[0].Tag
        private Information selectedItem;
        // 6.4. Strings for combobox items
        private string[] comboBoxItems;
        // State of program used for locking buttons
        private byte state = 128;
        // Variables to allow window movement
        private bool drag = false;
        private Point startPoint = new Point(0, 0);
        // Values for file IO
        private string runPath = Path.GetDirectoryName(Application.ExecutablePath);
        private string defName = "WikiList";

        private void WikiListForm_Load(object sender, EventArgs e)
        {
            populateCategories();
            comboBoxCat.SelectedIndex = -1;
            state = 0;
            lockButtons(96);

            ToolTip toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;
            saveToolStripMenuItem.ToolTipText = "Saves entires to a .dat binary file";
            loadToolStripMenuItem.ToolTipText = "Loads entries from saved .dat binary file";
            toolTip.SetToolTip(buttonAdd, "Adds entry from textboxes to entries");
            toolTip.SetToolTip(buttonDelete, "Removes selected entry from entries");
            toolTip.SetToolTip(buttonEdit, "Updates selected entry with new values from textboxes");
            toolTip.SetToolTip(buttonSearch, "Searches for an entry with the same name specified in the search textbox");
            toolTip.SetToolTip(textBoxName, "Name of Data Structure. Double click to clear controls");
            toolTip.SetToolTip(comboBoxCat, "Category of Data Structure");
            toolTip.SetToolTip(groupBox, "Structure of Data Structure. (Linear/non-linear)");
            toolTip.SetToolTip(textBoxDef, "Definition of Data Structure.");
            toolTip.SetToolTip(textBoxSearch, "Search box to search entry names. As you type, Entries will be filtered.");
            toolTip.SetToolTip(listView, "Displays name and category of each data structure. Clicking on an item selects it and puts the information into relevent textboxes");
        }

        #region Buttons

        // 6.7 delete currently selected items
        private void buttonDelete_Click(object sender, EventArgs e)
        {
            // Ask for confirmation first
            if (askForConfirm("Delete " + listView.SelectedItems.Count + " item/s?", "Confirm Deletion"))
            {
                int count = 0;
                // Go through all selected items and remove from Wiki
                foreach (ListViewItem item in listView.SelectedItems)
                {
                    Wiki.Remove((Information)item.Tag);
                    count++;
                }
                if (count != 1) // Show correct feedback depending on how many items have been deleted
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

        // 6.8 Edit Button. Updates selected value with info from controls
        private void buttonEdit_Click(object sender, EventArgs e)
        {
            int index = Wiki.FindIndex(i => i.gsName.Equals(selectedItem.gsName));
            // Could set values manually. It's Easier to assign.
            Information temp = getInfo();
            Wiki[index] = temp; // set selected item to updated value
            display();
            listView.SelectedItems.Clear();
            // reselect
            listView.FindItemWithText(temp.gsName).Selected = true;
            StatusStripLabel.Text = "Edited " + selectedItem.gsName;
        }

        // 6.3 Adds Information from values in controls 
        private void buttonAdd_Click(object sender, EventArgs e)
        {
            var temp = getInfo();
            bool confirm = true;
            // If incomplete ask for confirmation
            if (string.IsNullOrWhiteSpace(temp.gsDescription))
            {
                confirm = askForConfirm("Add Item Without Description?", "Confirm Incomplete");
            }
            if (confirm)
            {
                // Add item and clear controls
                Wiki.Add(temp);
                clearCtl();
                display();
                lockButtons(state & 240);
                StatusStripLabel.Text = "Added " + temp.gsName;
            }
            else
                StatusStripLabel.Text = "Did not add item";
        }

        // 6.10 Binary search using built-in method
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
                // clear and select
                textBoxSearch.Clear();
                display();
                listView.SelectedItems.Clear();
                listView.Items[index].Selected = true;
                StatusStripLabel.Text = "Found " + textBoxSearch.Text;
            }
            textBoxSearch.Focus();
        }

        // 6.14 Loads file. Prompts user for bin file and loads Wiki
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Bin files (*.bin)|*.bin";
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;
            openFile.InitialDirectory = runPath;
            openFile.FileName = defName + ".bin";
            DialogResult selectionResult = openFile.ShowDialog();
            // If user did not cancel
            if (selectionResult == DialogResult.OK)
            {
                try
                {
                    // Try to write serialised Wiki to file
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

        // 6.14 Saves file. Prompts user for file unless form is closing
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            DialogResult result;
            // If this method has been called by a control (button)
            if (sender != this)
            {
                saveFile.Filter = "Bin files (*.bin)|*.bin";
                saveFile.CheckPathExists = true;
                saveFile.InitialDirectory = runPath;
                saveFile.FileName = defName + ".bin";
                result = saveFile.ShowDialog();
            }
            // If this method has been called by this form
            else
            {
                saveFile.FileName = runPath + '\\' + defName + ".bin";
                result = DialogResult.OK;
            }

            if (result == DialogResult.OK)
            {
                try
                {
                    // Save wiki as serialised bin file
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

        // 6.11 Updates locking when Listview selected index changes. Adds item to controls
        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If only 1 item selected, allow editing
            if (listView.SelectedItems.Count == 1)
            {
                lockButtons((state | 2) & 242);
                selectedItem = (Information)listView.SelectedItems[0].Tag;
                // 6.11
                setControls(selectedItem);
            }
            // Multiple selected, allow mass delete
            else if (listView.SelectedItems.Count != 0)
            {
                lockButtons(state | 10);
                selectedItem = null;
                clearCtl();
            }
            // Nothing selected. Lock buttons
            else
            {
                lockButtons(state & 240);
                selectedItem = null;
                clearCtl();
            }
        }

        // 6.13 Clear controls on TextBoxName double click
        private void textBoxName_DoubleClick(object sender, EventArgs e)
        {
            clearCtl();
            lockButtons(state & 240);
            StatusStripLabel.Text = "Cleared controls";
        }

        // Disable and enable search button If populated
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

        // 6.15 Automatically save data on close.
        private void WikiListForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Call save button event handler directly. Pass self
            saveToolStripMenuItem_Click(this, new EventArgs());
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

        // Handle changing controls. Enable/disable buttons depending on state of controls. Don't do anything if controls are being updated manually (state & 128)
        private void ctrl_changed(object sender, EventArgs e)
        {
            // When Editing/deleting
            if (listView.SelectedItems.Count == 1 && (state & 8) == 0 && (state & 128) == 0)
            {
                if (!string.IsNullOrWhiteSpace(textBoxName.Text) &&
                    !string.IsNullOrWhiteSpace(comboBoxCat.Text) &&
                    getRadioValue() != null &&
                    !getInfo().Equals(selectedItem) &&
                    ValidName((List<Information>)Wiki.Where(i => !i.gsName.Equals(selectedItem.gsName)).ToList(), textBoxName.Text.Trim())
                    )
                {
                    lockButtons((state | 4) & 253); // Enable edit if different
                }
                else if (!getInfo().Equals(selectedItem))
                {
                    lockButtons(state & 240); // Disable edit if insufficent values
                    StatusStripLabel.Text = "Cannot Edit. Fix Required values";
                }

                if (getInfo().Equals(selectedItem))
                    lockButtons((state | 2) & 251); // Only enable delete if no values changed
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

        // Handle input in controls. Handle everything in combobox. Could change dropdown type but code currently sets values by setting text. Changing dropdown type breaks this
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

        //Removes carrige return and new line characters. Couldn't figure out how to get text without these characters.
        private string cleanText(string t)
        {
            return Regex.Replace(t, "['\r''\n']+", "").Trim();
        }

        // 6.9 Sorts and Displays Wiki in Listview. When text in searchbox filter matches
        private void display()
        {
            Wiki.Sort();
            listView.Items.Clear();
            Wiki.ForEach(i =>
            {
                // If nothing in search box or name matches text in searchbox
                if ((string.IsNullOrWhiteSpace(textBoxSearch.Text)) || (i.gsName.Contains(textBoxSearch.Text)))
                {
                    var temp = new ListViewItem(new String[] { i.gsName, i.gsCategory });
                    temp.Tag = i;
                    listView.Items.Add(temp);
                }
            });
        }

        // 6.5 Checks duplicates names. Returns true if unique
        private bool ValidName(string name) { return ValidName(Wiki, name);}

        // Overload allows for specifying list. Used in edit to check for duplicates (other than selected)
        private bool ValidName(List<Information> list, string name) { return !list.Exists(i => i.gsName.Equals(name.Trim())); }

        // Does what it says on the tin :/
        private bool askForConfirm(string message, string title)
        {
            var Result = MessageBox.Show(message, title, MessageBoxButtons.YesNo);
            if (Result == DialogResult.Yes)
                return true;
            else
                return false;
        }

        // Make an Information Instance from controls 
        private Information getInfo()
        {
            return new Information(
                cleanText(textBoxName.Text),
                getRadioValue(),
                cleanText(comboBoxCat.Text),
                cleanText(textBoxDef.Text.Trim()));
        }

        // 6.11.2 Displays Information item in controls
        private void setControls(Information i)
        {
            state |= 128;
            textBoxName.Text = i.gsName;
            comboBoxCat.Text = i.gsCategory;
            setRadioValue(convertRadioNameToIndex(i.gsIsLinear));
            textBoxDef.Text = i.gsDescription;
            state &= 127;
        }

        // 6.12 Clear Controls
        private void clearCtl()
        {
            state |= 128;
            textBoxName.Clear();
            comboBoxCat.ResetText();
            setRadioValue(-1);
            textBoxDef.Clear();
            state &= 127;
        }

        // 6.6 Return name of active radiobox
        private string getRadioValue()
        {
            var s = groupBox.Controls.OfType<RadioButton>().ToArray();
            foreach (var radio in s)
                if (radio.Checked == true)
                    return radio.Text;
            return null;
        }

        // 6.6 set active radiobox using index
        private void setRadioValue(int index)
        {
            // get array of radioboxes in groupBox
            var radioButtons = groupBox.Controls.OfType<RadioButton>().ToArray();
            for (int i = 0; i < radioButtons.Length; i++)
                if (i == index)
                    radioButtons[i].Checked = true;
                else
                    radioButtons[i].Checked = false;
        }

        // Converts radiobox name to index
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

        // 6.4 Add comboBocItems to comboBox as source
        private void populateCategories() { comboBoxCat.DataSource = comboBoxItems; }

        #endregion

        // Custom hacky titlebar. There is no easy way to add menus to the titlebar.
        #region custom TitleBar

        // Assign titlebar control event handlers
        void setupTitleBar()
        {
            // Custom Titlebar events to make window movable
            titleBar.MouseDown += new MouseEventHandler(TitleMouseDown);
            titleBar.MouseUp += new MouseEventHandler(TitleMouseUp);
            titleBar.MouseMove += new MouseEventHandler(TitleMouseMove);

            // Allow Titlebar to be moved even if title is clicked
            labelTitle.MouseDown += new MouseEventHandler(TitleMouseDown);
            labelTitle.MouseUp += new MouseEventHandler(TitleMouseUp);
            labelTitle.MouseMove += new MouseEventHandler(TitleMouseMove);

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

        // Change image if mouse hovering. Change back if not
        #region max min close buttons

        // Bit more complicated for maximise. (restore/maximise) buttons
        void MaxMouseEnter(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
                picMax.Image = Properties.Resources.maxReturnHover;
            else
                picMax.Image = Properties.Resources.maxHover;
        }

        void MaxMouseLeave(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
                picMax.Image = Properties.Resources.maxReturn;
            else
                picMax.Image = Properties.Resources.max;
        }

        void ExitMouseEnter(object sender, EventArgs e) { picClose.Image = Properties.Resources.closeHover; }

        void ExitMouseLeave(object sender, EventArgs e) { picClose.Image = Properties.Resources.close; }

        void MinMouseEnter(object sender, EventArgs e) { picMin.Image = Properties.Resources.minHover; }

        void MinMouseLeave(object sender, EventArgs e) { picMin.Image = Properties.Resources.min; }
        #endregion

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

        // Make the custom buttons manipulate the form (close, max, min)
        private void titleButtonMouseClick(object sender, MouseEventArgs e)
        {
            if (sender.Equals(picClose))
                this.Close(); // close the form
            else if (sender.Equals(this.picMax))
                if (WindowState == FormWindowState.Maximized)
                    WindowState = FormWindowState.Normal; // restore
                else
                    WindowState = FormWindowState.Maximized; // max
            else
                this.WindowState = FormWindowState.Minimized; // min
        }

        // Move the window position if mouse is draging titlebar
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

        // override that adds some drop shadow
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