#region Using
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

#endregion

namespace HmiExample
{
    public class TagItem : INotifyPropertyChanged, IEquatable<TagItem>
    {

        private string plcName;
        public string PLCName
        {
            get { return this.plcName; }
            set
            {
                if (this.plcName != value)
                {
                    this.plcName = value;
                    this.NotifyPropertyChanged("PLCName");
                }
            }
        }

        private string name;
        public string Name
        {
            get { return this.name; }
            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    this.NotifyPropertyChanged("Name");
                }
            }
        }

        private string address;
        public string Address
        {
            get { return this.address; }
            set
            {
                if (this.address != value)
                {
                    this.address = value;
                    this.NotifyPropertyChanged("Address");
                }
            }
        }

        private string type;
        public string Type
        {
            get { return this.type; }
            set
            {
                if (this.type != value)
                {
                    this.type = value;
                    this.NotifyPropertyChanged("Type");
                }
            }
        }

        private string value;
        public string Value
        {
            get { return this.value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    this.NotifyPropertyChanged("Value");
                }
            }
        }

        public TagItem() { }

        void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null) 
            {
                // reentrant... approfondire
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
                }));
            }
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", plcName, address);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            return Equals(obj as TagItem);
        }

        public bool Equals(TagItem tag)
        {
            // If parameter is null return false:
            if (tag == null)
            {
                return false;
            }

            // Return true if either fields match:
            return ((PLCName == tag.PLCName && Address == tag.Address));
        }
        public override int GetHashCode()
        {
            return (this.PLCName+this.Address).GetHashCode();
        }

        public static bool operator == (TagItem tag1, TagItem tag2)
        {
            if (((object)tag1) == ((object)tag2)) return true;
            if (((object)tag1) == null || ((object)tag2) == null) return false;

            return tag1.Equals(tag2);
        }

        public static bool operator != (TagItem tag1, TagItem tag2)
        {
            return !(tag1 == tag2);
        }

    }
}
