namespace WikiList
{
    public partial class WikiListForm : Form
    {
        public WikiListForm()
        {
            InitializeComponent();
        }

        private List<Information> Wiki;


        private bool ValidName(string name)
        {
            return Wiki.Exists(i => i.gsName.Equals(name));
        }
    }
}