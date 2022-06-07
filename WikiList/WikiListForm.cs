using System.Diagnostics;
using System.Drawing;
using System.Resources;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;

namespace WikiList
{
    public partial class WikiListForm : Form
    {
        public WikiListForm()
        {
            InitializeComponent();

            // Custom Titlebar events to make window movable
            titleBar.MouseDown += new MouseEventHandler(TitleMouseDown);
            titleBar.MouseUp += new MouseEventHandler(TitleMouseUp);
            titleBar.MouseMove += new MouseEventHandler(TitleMouseMove);
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

        // Variables to allow window movement
        private bool drag = false;
        private Point startPoint = new Point(0, 0);



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
            return Wiki.Exists(i => i.gsName.Equals(name));

        }

        #region custom TitleBar
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

        private void WikiListForm_Load(object sender, EventArgs e)
        {
            comboBoxCat.DataSource = comboBoxItems;
            comboBoxCat.SelectedIndex = -1;
        }

        private Information getInfo()
        {
            return new Information(textBoxName.Text, getRadioValue(), comboBoxCat.Text, textBoxDef.Text);
        }

        private void setText(Information i)
        {
            textBoxName.Text = i.gsName;
            comboBoxCat.SelectedIndex = comboBoxCat.FindString(i.gsCategory);
            setRadioValue(convertRadioNameToIndex(i.gsIsLinear));
            textBoxDef.Text = i.gsDescription;

        }

        private void clearCtl()
        {
            textBoxName.Text = "";
            comboBoxCat.SelectedIndex = -1;
            setRadioValue(-1);
            textBoxDef.Text = "";

        }

        // Useless requirement. 
        private string getRadioValue()
        {
            return groupBox.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked).Name;
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



        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If only 1 item selected, allow editing
            if (listView.SelectedItems.Count == 1) 
            {
                selectedItem = (Information)listView.SelectedItems[0].Tag;
                setText(selectedItem);

            }
            else
            {
                selectedItem = null;
                clearCtl();
            }
        }

        // delete selected items
        private void buttonDelete_Click(object sender, EventArgs e)
        {
            int count = 0;
            foreach (ListViewItem item in listView.SelectedItems)
            {
                Wiki.Remove((Information)item.Tag);
                count++;
            }
            if (count != 1)
                toolStripStatusLabel.Text = "Removed " + count + " Items";
            else
                toolStripStatusLabel.Text = "Removed " + selectedItem.gsName;

            clearCtl();
            display();
        }

        private void buttonEdit_Click(object sender, EventArgs e)
        {
            int index = Wiki.FindIndex(i => i == selectedItem);
            var temp = getInfo();
            Wiki[index] = temp;
            display();
            listView.SelectedItems.Clear();
            listView.FindItemWithText(temp.gsName).Selected = true;
            toolStripStatusLabel.Text = "Edited " + selectedItem.gsName;
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            var temp = getInfo();
            Wiki.Add(temp);
            clearCtl();
            display();
            toolStripStatusLabel.Text = "Added " + temp.gsName;
        }

        private void textBoxSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            display();
        }
    }
}