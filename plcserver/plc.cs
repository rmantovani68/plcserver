#region Using
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using S7.Net;
using S7NetWrapper;
#endregion Using

namespace plcserver
{
    class Plc
    {
        /* gestione locking */
        static readonly object _lock = new object();

        #region Public properties

        public ConnectionStates ConnectionState { get { return plcDriver != null ? plcDriver.ConnectionState : ConnectionStates.Offline; } }

        public TimeSpan CycleReadTime { get; private set; }

        public int LoopTime
        {
            get { return _loopTime; }

            set
            {
                timer.Enabled = false;
                timer.Interval = value; // ms
                _loopTime = value;
                timer.Enabled = true;
            }
        }

        public string PlcName { get; private set; }
        public CpuType Cputype { get; private set; }
        public string IpAddress { get; private set; }
        public short Rack { get; private set; }
        public short Slot { get; private set; }
        public int Delay { get; private set; }

        #endregion

        #region Private fields


        private int _loopTime;

        private IPlcSyncDriver plcDriver;

        private System.Timers.Timer timer = new System.Timers.Timer();

        private DateTime lastReadTime;

        private List<Tag> m_TagsList = new List<Tag>();

        private Dictionary<String, Tag> m_Tags = new Dictionary<String, Tag>();

        #endregion

        #region Constructor

        public Plc(string plcName, CpuType cputype, string ipAddress, short rack, short slot, int delay)
        {
            PlcName = plcName;
            Cputype = cputype;
            IpAddress = ipAddress;
            Rack = rack;
            Slot = slot;
            Delay = delay;

            // connect to plc
            Connect();

            LoopTime = Delay;

            timer.Elapsed += timer_Elapsed;

            timer.Enabled = true;
            lastReadTime = DateTime.Now;


        }


        #endregion

        #region Event handlers

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Enabled = false;
            CycleReadTime = DateTime.Now - lastReadTime;

            if (plcDriver == null || plcDriver.ConnectionState != ConnectionStates.Online)
            {
                timer.Enabled = true;
                lastReadTime = DateTime.Now;
                return;
            }

            try
            {
                /* aggiornamento dei valori dei tag sottoscritti */
                RefreshTags();
            }
            finally
            {
                timer.Enabled = true;
                lastReadTime = DateTime.Now;
            }
        }

        #endregion

        #region Public methods

        public void Connect()
        {

            if (!IsValidIp(IpAddress))
            {
                throw new ArgumentException("Ip address is not valid");
            }
            //plcDriver = new S7NetPlcDriver(CpuType.S7300, ipAddress, 0, 2);
            plcDriver = new S7NetPlcDriver(Cputype, IpAddress, Rack, Slot);
            plcDriver.Connect();

        }

        public void Disconnect()
        {
            lock (_lock)
            {
                if (plcDriver == null || this.ConnectionState == ConnectionStates.Offline)
                {
                    return;
                }
                plcDriver.Disconnect();
            }
        }

        public void ClearTags()
        {
            m_Tags.Clear();
        }

        public bool AddTag(string name)
        {
            bool RetVal = true;

            if (m_Tags.ContainsKey(name))
            {
                return false;
            }

            /* controllo la validità del tag */
            var tag = new Tag(name);

            try 
            {
                plcDriver.ReadItem(tag);
            }
            catch (Exception exc) 
            {
                RetVal = false;
                throw exc;
            }

            if (RetVal)
            {
                m_Tags.Add(name, new Tag(name));
            }
            
            return RetVal;
        }

        public int AddTags(IEnumerable<string> tags)
        {
            int count = 0;

            foreach(var tagname in tags)
            {
                if (AddTag(tagname))
                {
                    count++;
                }
            }
            return count;
        }

        public bool RemoveTag(string name)
        {
            if (!m_Tags.ContainsKey(name))
            {
                return false;
            }
            m_Tags.Remove(name);

            return true;
        }

        public int RemoveTags(IEnumerable<string> tags)
        {
            int count = 0;

            foreach (var tag in tags)
            {
                if (RemoveTag(tag))
                {
                    count++;
                }
            }
            return count;
        }

        public void Write(string name, object value)
        {
            if (plcDriver == null || plcDriver.ConnectionState != ConnectionStates.Online)
            {
                return;
            }
            var tag = new Tag(name, value);

            Write(tag);
        }

        public void Write(Tag tag)
        {
            if (plcDriver == null || plcDriver.ConnectionState != ConnectionStates.Online)
            {
                return;
            }
        
            plcDriver.WriteItem(tag);
        }

        public void Write(List<Tag> tags)
        {
            if (plcDriver == null || plcDriver.ConnectionState != ConnectionStates.Online)
            {
                return;
            }
            plcDriver.WriteItems(tags);
        }

        public Tag Read(string name, object value)
        {
            if (plcDriver == null || plcDriver.ConnectionState != ConnectionStates.Online)
            {
                return null;
            }
            var tag = new Tag(name, value);

            return plcDriver.ReadItem(tag);
        }

        public Tag Read(Tag tag)
        {
            if (plcDriver == null || plcDriver.ConnectionState != ConnectionStates.Online)
            {
                return null;
            }

            return plcDriver.ReadItem(tag);
        }

        public List<Tag> Read(List<Tag> tags)
        {
            if (plcDriver == null || plcDriver.ConnectionState != ConnectionStates.Online)
            {
                return null;
            }

            return plcDriver.ReadItems(tags);
        }

        #endregion

        #region Private methods

        private bool IsValidIp(string addr)
        {
            IPAddress ip;
            bool valid = !string.IsNullOrEmpty(addr) && IPAddress.TryParse(addr, out ip);
            return valid;
        }

        private void RefreshTags()
        {
            lock(_lock)
            {
                if (this.ConnectionState != ConnectionStates.Online)
                    return;

                var old_TagsList = m_Tags.Values.ToList();

                var new_TagsList = plcDriver.ReadItems(old_TagsList);

                /*
                * ora occorre notificare i cambiamenti ai sottoscriventi
                */
                foreach (var Item in new_TagsList)
                {
                    if (m_Tags.ContainsKey(Item.ItemName))
                    {
                        if (!Item.ItemValue.Equals(m_Tags[Item.ItemName].ItemValue))
                        {
                            /* valore sottoscritto cambiato */
                            if (TagChangedValue != null)
                            {
                                //Raise event
                                Tag _Tag = new Tag(Item.ItemName);
                                _Tag.ItemValue = Item.ItemValue;

                                TagChangedValue(this, new TagChangedValueEventArgs { PLCName = this.PlcName, Tag = _Tag });
                            }
                            /* attualizzo */
                            m_Tags[Item.ItemName].ItemValue = Item.ItemValue;
                        }
                    }
                    else
                    {
                        /* tag non presente in lista */
                    }

                }

            }

        }

        #endregion

        /// <summary>
        /// This event is raised when a Tag Change his value
        /// </summary>
        public event TagChangedValueHandler TagChangedValue;


    }

    /// <summary>
    /// A delegate to create events when a tag value does change
    /// </summary>
    /// <param name="sender">The object which raises event</param>
    /// <param name="e">Event arguments</param>
    public delegate void TagChangedValueHandler(object sender, TagChangedValueEventArgs e);

    /// <summary>
    /// Stores tag informations.
    /// </summary>
    public class TagChangedValueEventArgs : EventArgs
    {
        /// <summary>
        /// Changed Tag
        /// </summary>
        public String PLCName { get; set; }
        public Tag Tag { get; set; }
    }
}