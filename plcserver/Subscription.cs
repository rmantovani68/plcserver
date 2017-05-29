using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plcserver
{

    class Subscription : IComparable,IComparable<Subscription>
    {
        public string PLCName { get; private set; }
        public string TagAddress { get; private set; }

        public Subscription(string plcName, string tagAddress)
        {
            PLCName = plcName;
            TagAddress = tagAddress;
        }

        public override bool Equals(object obj)
        {
            // throws exception if type is wrong
            var sub = obj as Subscription;

            if(sub==null) return false;

            return (PLCName == sub.PLCName && TagAddress == sub.TagAddress);
        }

        public override int GetHashCode()
        {
            return (PLCName+TagAddress).GetHashCode();
        }

        public override string ToString()
        {
            return PLCName + "/" + TagAddress;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            // throws exception if type is wrong
            Subscription sub = (Subscription)obj;

            return CompareTo(sub);
        }

        public int CompareTo(Subscription sub)
        {
            var cmpPLCName = PLCName.CompareTo(sub.PLCName);
            if (cmpPLCName == 0) 
                return TagAddress.CompareTo(sub.TagAddress);
            return cmpPLCName;            
        }

        #endregion

    }

}
