#region Using
using System;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;

using log4net;
using MDS;
using MDS.Client;
using MDS.Communication.Messages;

using OMS.Core.Communication;
using System.Collections.Specialized;
using System.ComponentModel;
#endregion

namespace HmiExample
{
    public class Model
    {
        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // lista dei tags sottoscritti
        public ObservableUniqueCollection<TagItem> ListTagItems { get; set; }

        // lista dei plc connessi
        public ObservableUniqueCollection<PLCItem> ListPLCItems { get; set; }

        // timer 
        DispatcherTimer timer = new DispatcherTimer();

        public bool ConnectionState = false;

        #region Constructor
        public Model()
        {
            ListTagItems = new ObservableUniqueCollection<TagItem>();

            ListPLCItems = new ObservableUniqueCollection<PLCItem>();
        }
        #endregion Constructor
    }

    
    public class ObservableUniqueCollection<T> : ObservableCollection<T>
    {
        protected override void InsertItem(int index, T item)
        {
            var exists = false;

            foreach (var myItem in Items.Where(myItem => myItem.Equals(item)))
                exists = true;

            if (!exists)
                base.InsertItem(index, item);
        }
    }
}
