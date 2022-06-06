using System.Diagnostics;
using System.Drawing;
using System.Resources;

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
        }

        private List<Information> Wiki;
        private bool drag = false;
        private Point startPoint = new Point(0, 0);



        //private void display()
        //{
        //    Wiki.ForEach(i => {
        //        var temp = new ListViewItem();
        //        temp.SubItems.Add(i.gsName);
        //        temp.SubItems.Add(i.gsCategory);
        //        temp.SubItems.Add(i.gsIsLinear);
        //        temp.SubItems.Add(i.gsDescription);
        //        listView.Items.Add(temp);
        //    });
        //}



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

        }
    }
}