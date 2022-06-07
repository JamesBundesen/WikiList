
//      Information.cs        08/06/2022         JB 30038531
//      ----------------------------------------------------.

namespace WikiList
{
    [Serializable]
    internal class Information : IComparable<Information>
    {
        // 6.1
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

        // get/set Name
        public string gsName
        {
            get { return name; }
            set { name = value; }
        }

        // get/set isLinear returning string from bool/setting bool from string
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

        // get/set Category
        public string gsCategory
        {
            get { return category; }
            set { category = value; }
        }

        // get/set Description
        public string gsDescription
        {
            get { return description; }
            set { description = value; }
        }

        // CompareTo Override
        public int CompareTo(Information? other)
        {
            return this.name.CompareTo(other.gsName);
        }

        // Compares values of two Information objects. If the same returns true
        public bool Equals(Information? other)
        {
            return this.gsName.Equals(other.gsName)
                && this.gsIsLinear.Equals(other.gsIsLinear)
                && this.gsCategory.Equals(other.gsCategory)
                && this.gsDescription.Equals(other.gsDescription);
        }
    }
}
