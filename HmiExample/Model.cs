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


        /*
        * roba da decidere ....
        * ---------------------
         
        private int LSX = 0;
        private int _minimumRange = 0;
        private int _maximumRange = 0;
        private int _loopTime;

        public int MinimumRange
        {
            get { return _minimumRange; }
            set
            {
                _minimumRange = value;
                this.NotifyPropertyChanged("MinimumRange");
            }
        }

        public int MaximumRange
        {
            get { return _maximumRange; }
            set
            {
                _maximumRange = value;
                this.NotifyPropertyChanged("MaximumRange");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public int LoopTime
        {
            get
            {
                return _loopTime;
            }

            set
            {
                _loopTime = value;
                timer.Interval = TimeSpan.FromMilliseconds(_loopTime);
            }
        }

        // disaccoppiare
        public int PlcLoopTime
        {
            get;
            set;
        }

        public int LoopTimeDisplayRange { get; set; }

        // andamento del tempo di ciclo
        public ObservableCollection<Point> LP { get; set; }
        * ---------------------

        */

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

            /*
            LoopTime = 100;
            LoopTimeDisplayRange = 20;

            LP = new ObservableCollection<Point>();


            for (var i = 0; i < LoopTimeDisplayRange; i++)
            {
                LSX++;
                LP.Add(new Point(LSX, LoopTime));
            }
            */

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
