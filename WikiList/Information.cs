namespace WikiList
{
    [Serializable]
    internal class Information : IComparable<Information>
    {
        private string name;
        private bool isLinear;
        private string category;
        private string description;

        public Information(string name, string gsIsLinear, string category, string description)
        {
            this.name = name;
            this.gsIsLinear = gsIsLinear;
            this.category = category;
            this.description = description;

        }

        public string gsName
        {
            get { return name; }
            set { name = value; }
        }

        public string gsIsLinear
        {
            get
            {
                if (isLinear)
                {
                    return "Linear";
                }
                else
                {
                    return "Non-Linear";
                }
            }
            set
            {
                if (value == null)
                    value = "l";
                char temp = value[0];
                switch (temp)
                {
                    case 'l':
                    case 'L':
                        isLinear = true;
                        break;
                    case 'N':
                    case 'n':
                        isLinear = false;
                        break;
                }
            }
        }

        public string gsCategory
        {
            get { return category; }
            set { category = value; }
        }

        public string gsDescription
        {
            get { return description; }
            set { description = value; }
        }

        public int CompareTo(Information? other)
        {
            return this.name.CompareTo(other.gsName);
        }
    }
}
