#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml;
using System.Timers;

using log4net;
using MDS;
using MDS.Client;
using MDS.Communication.Messages;

using OMS.Core.Communication;
#endregion Using

namespace plcserver
{
    class plcserver
    {

        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
                
        #region Private Fields
        private MDSClient mdsClient;
        private System.Timers.Timer timer = new System.Timers.Timer();
        
        // default loop time = 100
        static private int _defaultLoopTime = 100;
        // default application Name
        static private string _defaultApplicationName = "PLCServer";

        private int _loopTime;
        private DateTime lastReadTime;

        /* associazione plcname / plctags */
        private Dictionary<string, Plc> _PLCs = new Dictionary<string, Plc>();

        /* associazione sender / subscriptions */
        private Dictionary<string, HashSet<Subscription>> _Subs = new Dictionary<string, HashSet<Subscription>>();
        #endregion Private Fields
        
        #region Properties
        public string ApplicationName { get; private set; }
        
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
        #endregion Properties

        #region Constructor
        
        public plcserver(int loopTime, string applicationName)
        {
            ApplicationName = applicationName;

            // Create MDSClient object to connect to DotNetMQ
            // Name of this application: PLCServer
            mdsClient = new MDSClient(ApplicationName);

            // Connect to DotNetMQ server
            try
            {
                mdsClient.Connect();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
                // esco
                this.Exit();
            }
            // Register to MessageReceived event to get messages.
            mdsClient.MessageReceived += PLCServer_MessageReceived;

            LoopTime = loopTime;
            timer.Elapsed += timer_Elapsed;
            timer.Enabled = true;

            Logger.InfoFormat("{0} application ready", ApplicationName);
        }

        public plcserver()
            : this(_defaultLoopTime, _defaultApplicationName)
        {
        }
        #endregion Constructor

        #region Event Handlers
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Enabled = false;
            CycleReadTime = DateTime.Now - lastReadTime;

            timer.Enabled = true;
            lastReadTime = DateTime.Now;
        }
        #endregion Event Handlers



        #region Public Methods

        /// <summary>
        /// Uscita dall'applicazione
        /// </summary>
        public void Exit()
        {
            //Disconnect from DotNetMQ server
            Logger.InfoFormat("{0} Exit Application", ApplicationName);
            mdsClient.Disconnect();
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// This method handles received messages from other applications via DotNetMQ.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Message parameters</param>
        private void PLCServer_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            // Get message 
            var Message = e.Message;
            
            // Acknowledge that message is properly handled and processed. So, it will be deleted from queue.
            e.Message.Acknowledge();

            // Get message data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as MsgData;

            try
            {

                switch (MsgData.MsgCode)
                {
                    case MsgCodes.ConnectSubscriber:      ConnectSubscriber(Message);       break;
                    case MsgCodes.DisconnectSubscriber:   DisconnectSubscriber(Message);    break;
                    case MsgCodes.SubscribePLCTag:        SubscribePLCTag(Message);         break;
                    case MsgCodes.SubscribePLCTags:       SubscribePLCTags(Message);        break;
                    case MsgCodes.RemovePLCTag:           RemovePLCTag(Message);            break;
                    case MsgCodes.RemovePLCTags:          RemovePLCTags(Message);           break;
                    case MsgCodes.GetSubscribedPLCTags:   GetSubscribedPLCTags(Message);    break;
                    case MsgCodes.SetPLCTags:             SetPLCTags(Message);              break;
                    case MsgCodes.GetPLCTags:             GetPLCTags(Message);              break;
                    case MsgCodes.SetPLCTag:              SetPLCTag(Message);               break;
                    case MsgCodes.GetPLCTag:              GetPLCTag(Message);               break;
                    case MsgCodes.StartCheckPLCTags:      StartCheckPLCTags(Message);       break;
                    case MsgCodes.StopCheckPLCTags:       StopCheckPLCTags(Message);        break;
                    case MsgCodes.GetPLCStatus:           GetPLCStatus(Message);            break;
                    case MsgCodes.ConnectPLC:             ConnectPLC(Message);              break;
                    case MsgCodes.DisconnectPLC:          DisconnectPLC(Message);           break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message, ex);
            }
        }

        /// <summary>
        /// Implementazione esecuzione comando di connessione di un client sottoscrittore
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool ConnectSubscriber(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as MsgData;

            Logger.InfoFormat("{1} da {0}", Message.SourceApplicationName, MsgData.MsgCode);
            
            // gestione subscriptions
            if (!_Subs.ContainsKey(Message.SourceApplicationName))
            {
                _Subs.Add(Message.SourceApplicationName, new HashSet<Subscription>());
            }
            else
            {
                foreach (var sub in _Subs[Message.SourceApplicationName].ToList())
                {
                    RemovePLCTag(Message.SourceApplicationName, new PLCTag() { PLCName = sub.PLCName, Address = sub.TagAddress });
                }
            }

            /* invio messaggio di risposta generica */
            return SendResponse(Message, MsgCodes.ResultConnectSubscriber, MsgData, RetValue);
        }

        /// <summary>
        /// Implementazione esecuzione comando di disconnessione di un client sottoscrittore
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool DisconnectSubscriber(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as MsgData;

            Logger.InfoFormat("{1} da {0}", Message.SourceApplicationName, MsgData.MsgCode);

            // gestione subscriptions
            if (_Subs.ContainsKey(Message.SourceApplicationName))
            {
                foreach (var sub in _Subs[Message.SourceApplicationName].ToList())
                {
                    RemovePLCTag(Message.SourceApplicationName, new PLCTag() { PLCName = sub.PLCName, Address = sub.TagAddress });
                }
                _Subs.Remove(Message.SourceApplicationName);
            }
            else
            {
                // non esiste !
                Logger.WarnFormat("{0} non sottoscritto!", Message.SourceApplicationName);
                RetValue = false;
            }

            /* invio messaggio di risposta generica */
            return SendResponse(Message, MsgCodes.ResultDisconnectSubscriber, MsgData, RetValue);
        }

        /// <summary>
        /// Connette un PLC 
        /// </summary>
        /// <param name="ConnectionData">Dati di connessione</param>
        /// <returns>true set tutto ok, altrimenti false</returns>
        private bool ConnectPLC(PLCConnectionData ConnectionData)
        {
            bool RetValue = true;

            if (_PLCs.ContainsKey(ConnectionData.PLCName))
            {
                // log
                Logger.WarnFormat("PLC [{0}] already connected", ConnectionData.PLCName);
                RetValue = false;
            }
            else
            {
                try
                {

                    var plc = new Plc(ConnectionData.PLCName, ConnectionData.Cputype, ConnectionData.IpAddress, ConnectionData.Rack, ConnectionData.Slot, ConnectionData.Delay);

                    _PLCs.Add(ConnectionData.PLCName, plc);

                    // associo l'evento di changed value sui tags sottoscritti
                    plc.TagChangedValue += plc_TagChangedValue;

                }
                catch (Exception exc)
                {
                    // log
                    Logger.WarnFormat("PLC [{0}] error in connection : {1}", ConnectionData.PLCName, exc.Message);

                    RetValue = false;
                }
            }
            return RetValue;
        }



        /// <summary>
        /// Implementazione esecuzione comando di connessione di un plc
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool ConnectPLC(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCConnectionData;

            Logger.InfoFormat("{1}-{2}-{3}-{4}-{5}-{6} da {0}", Message.SourceApplicationName, MsgData.PLCName, MsgData.Cputype.ToString(), MsgData.IpAddress, MsgData.Rack, MsgData.Slot, MsgData.Delay);

            if (!_Subs.ContainsKey(Message.SourceApplicationName))
            {
                Logger.WarnFormat("[{0}] not subscribed", Message.SourceApplicationName);
                RetValue = false;
            }
            else
            {
                RetValue = ConnectPLC(MsgData);
            }

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultConnectPLC, MsgData, RetValue);
        }

        /// <summary>
        /// Disconnette un PLC 
        /// </summary>
        /// <param name="PLC">Dati del PLC da disconnettere</param>
        /// <returns>true set tutto ok, altrimenti false</returns>
        private bool DisconnectPLC(PLCData PLC)
        {
            bool RetValue = true;

            if (!_PLCs.ContainsKey(PLC.PLCName))
            {
                // log
                Logger.WarnFormat("PLC [{0}] not connected", PLC.PLCName);
                RetValue = false;
            }
            else
            {
                try
                {
                    var plc = _PLCs[PLC.PLCName];

                    // Disconnetto 
                    plc.Disconnect();

                    // elimino dalla lista
                    _PLCs.Remove(PLC.PLCName);

                }
                catch (Exception exc)
                {
                    // log
                    Logger.WarnFormat("PLC [{0}] error in disconnection : {1}", PLC.PLCName, exc.Message);
                    RetValue = false;
                }
            }
            return RetValue;
        }



        /// <summary>
        /// Implementazione esecuzione comando di disconnessione di un plc
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool DisconnectPLC(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCData;

            Logger.InfoFormat("{1} da {0}", Message.SourceApplicationName, MsgData.PLCName);

            if (!_Subs.ContainsKey(Message.SourceApplicationName))
            {
                Logger.WarnFormat("[{0}] not subscribed", Message.SourceApplicationName);
                RetValue = false;
            }
            else
            {
                RetValue = DisconnectPLC(MsgData);
            }

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultDisconnectPLC, MsgData, RetValue);
        }

        /// <summary>
        /// Implementazione esecuzione comando di sottoscrizione di un plctag
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool SubscribePLCTag(string subscriber, PLCTag tag)
        {
            bool RetValue = true;

            if (!_Subs.ContainsKey(subscriber))
            {
                Logger.WarnFormat("[{0}] not subscribed", subscriber);
                RetValue = false;
            }

            if (RetValue == true)
            {
                // verifico esistenza PLC interessato
                if (!_PLCs.ContainsKey(tag.PLCName))
                {
                    // log
                    Logger.WarnFormat("PLC [{0}] non presente", tag.PLCName);

                    RetValue = false;
                }
                else
                {
                    try
                    {
                        // aggiungo tag
                        _PLCs[tag.PLCName].AddTag(tag.Address);

                        // gestione subscriptions
                        if (!_Subs.ContainsKey(subscriber))
                        {
                            _Subs.Add(subscriber, new HashSet<Subscription>());
                        }

                        _Subs[subscriber].Add(new Subscription(tag.PLCName, tag.Address));
                    }
                    catch (Exception exc)
                    {
                        // log
                        Logger.WarnFormat("PLC [{0}] error adding tag {1} : {2}", tag.PLCName, tag.Address, exc.Message);
                        RetValue = false;
                    }
                }
            }

            return RetValue;
        }

        /// <summary>
        /// Implementazione esecuzione comando di rimozione di un plctag
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool RemovePLCTag(string subscriber, PLCTag tag)
        {
            bool RetValue = true;

            Logger.InfoFormat("{1}/{2} da {0}", subscriber, tag.PLCName, tag.Address);

            if (!_Subs.ContainsKey(subscriber))
            {
                Logger.WarnFormat("[{0}] not subscribed", subscriber);
                RetValue = false;
            }

            if (RetValue == true)
            {


                // verifico esistenza PLC interessato
                if (!_PLCs.ContainsKey(tag.PLCName))
                {
                    Logger.WarnFormat("PLC [{0}] not present", tag.PLCName);

                    RetValue = false;
                }
                else
                {
                    try
                    {
                        // rimuovo tag
                        _PLCs[tag.PLCName].RemoveTag(tag.Address);

                        // gestione subscriptions
                        if (_Subs.ContainsKey(subscriber))
                        {
                            _Subs[subscriber].Remove(new Subscription(tag.PLCName, tag.Address));
                        }
                    }
                    catch (Exception exc)
                    {
                        // log
                        Logger.WarnFormat("PLC [{0}] error removing tag {1} : {2}", tag.PLCName, tag.Address, exc.Message);
                        RetValue = false;
                    }
                }
            }
            return RetValue;
        }


        /// <summary>
        /// Implementazione esecuzione comando di sottoscrizione di un plctags
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool SubscribePLCTag(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;
            var tag = MsgData.Tag;

            Logger.InfoFormat("{1}/{2} da {0}", Message.SourceApplicationName, tag.PLCName, tag.Address);

            RetValue = SubscribePLCTag(Message.SourceApplicationName, tag);

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultSubscribePLCTag, MsgData, RetValue);
        }

        /// <summary>
        /// Implementazione esecuzione comando di rimozione di un plctags
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool RemovePLCTag(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;
            var tag = MsgData.Tag;

            RetValue = RemovePLCTag(Message.SourceApplicationName, tag);

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultRemovePLCTag, MsgData, RetValue);
        }


        /// <summary>
        /// Implementazione esecuzione comando di sottoscrizione di una lista di plctags
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool SubscribePLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagsData;
            var tagslist = MsgData.Tags;

            Logger.InfoFormat("{0}", Message.SourceApplicationName);

            for (int i = 0; i < tagslist.Count; i++)
            {
                var tag = tagslist[i];

                if (!SubscribePLCTag(Message.SourceApplicationName, tag)) 
                {
                    RetValue = false;
                    tag.Validation = false;
                }
                else
                {
                    tag.Validation = true;
                }
            }

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultSubscribePLCTags, MsgData, RetValue);
        }

        /// <summary>
        /// Implementazione esecuzione comando di rimozione della sottoscrizione di una lista di plctags
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool RemovePLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagsData;
            var tagslist = MsgData.Tags;

            Logger.InfoFormat("{0}", Message.SourceApplicationName);

            for (int i = 0; i < tagslist.Count; i++)
            {
                var tag = tagslist[i];

                if (!RemovePLCTag(Message.SourceApplicationName, tag))
                {
                    tag.Validation = false;
                    RetValue = false;
                }
                else
                {
                    tag.Validation = true;
                }
            }

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultRemovePLCTags, MsgData, RetValue);
        }

        /// <summary>
        /// Implementazione esecuzione comando di richiesta lista plctags sotoscritti
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool GetSubscribedPLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;
            var tagslist = new List<PLCTag>();

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as MsgData;

            Logger.InfoFormat("{0}", Message.SourceApplicationName);

            // gestione subscriptions
            if (!_Subs.ContainsKey(Message.SourceApplicationName))
            {
                Logger.WarnFormat("[{0}] has no subscriptions", Message.SourceApplicationName);
                RetValue = false;
            }
            else
            {
                // costruisco la lista da inviare come risposta
                foreach (var sub in _Subs[Message.SourceApplicationName].ToList())
                {
                    var tag = new PLCTag() { PLCName = sub.PLCName, Address = sub.TagAddress, Validation=true };
                    tagslist.Add(tag);
                }
            }

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.SubscribedPLCTags, new PLCTagsData(){MsgCode = MsgCodes.SubscribedPLCTags,Tags = tagslist}, RetValue);
        }

        /// <summary>
        /// non implementata - occorre definire una classe subscriptions contenente lista plctag sottoscritti e stato (start/stop) ...
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        private bool StartCheckPLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;
            return RetValue;
        }

        /// <summary>
        /// non implementata - occorre definire una classe subscriptions contenente lista plctag sottoscritti e stato (start/stop) ...
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        private bool StopCheckPLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;
            return RetValue;
        }

        /// <summary>
        /// Implementazione esecuzione comando di set valore lista plctags
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool SetPLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagsData;
            var tagslist = MsgData.Tags;

            Logger.InfoFormat("{0}", Message.SourceApplicationName);

            for (int i = 0; i < tagslist.Count; i++)
            {
                var tag = tagslist[i];

                // verifico connessione/esistenza PLC interessato
                if (!_PLCs.ContainsKey(tag.PLCName))
                {
                    // log
                    Logger.WarnFormat("PLC [{0}] not connected", tag.PLCName);
                    tag.Validation = false;
                    RetValue = false;
                }
                else
                {
                    try
                    {
                        // scrivo tag
                        var plctag = new S7NetWrapper.Tag(tag.Address, tag.Value);
                        _PLCs[tag.PLCName].Write(plctag);
                        tag.Validation = true;
                    }
                    catch (Exception exc)
                    {
                        // log
                        Logger.WarnFormat("PLC [{0}] error writing tag {1} : {2}", tag.PLCName, tag.Address, exc.Message);
                        tag.Validation = false;
                        RetValue = false;
                    }
                }
            }

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultSetPLCTags, MsgData, RetValue);
        }

        /// <summary>
        /// Implementazione esecuzione comando di richiesta valore lista plctags
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool GetPLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;
            

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagsData;
            var tagslist = MsgData.Tags;

            Logger.InfoFormat("{0}", Message.SourceApplicationName);

            for (int i = 0; i < tagslist.Count; i++)
            {
                var tag = tagslist[i];

                // verifico connessione/esistenza PLC interessato
                if (!_PLCs.ContainsKey(tag.PLCName))
                {
                    // log
                    Logger.WarnFormat("PLC [{1}] not connected", tag.PLCName);

                    tag.Validation = true;
                    RetValue = false;
                }
                else
                {
                    try
                    {
                        /* leggo tag value */
                        var plctag = _PLCs[tag.PLCName].Read(new S7NetWrapper.Tag(tag.Address));
                        if(plctag!=null){
                            tag.Value = plctag.ItemValue;
                            tag.Validation = true;
                        }
                    }
                    catch (Exception exc)
                    {
                        // log
                        Logger.WarnFormat("PLC [{0}] error reading tag {1} : {2}", tag.PLCName, tag.Address, exc.Message);
                        tag.Validation = true;
                        RetValue = false;
                    }
                }
            }

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultGetPLCTags, new PLCTagsData() { MsgCode = MsgCodes.ResultGetPLCTags, Tags = tagslist }, RetValue);
        }

        /// <summary>
        /// Impostazione valore plctag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool SetPLCTag(PLCTag tag)
        {
            bool RetValue = true;

            /* verifico connessione/esistenza PLC interessato */
            if (!_PLCs.ContainsKey(tag.PLCName))
            {
                // log
                Logger.WarnFormat("PLC [{0}] not connected", tag.PLCName);

                RetValue = false;
            }
            else
            {
                try
                {
                    /* scrivo tag */
                    var plctag = new S7NetWrapper.Tag(tag.Address, tag.Value);
                    _PLCs[tag.PLCName].Write(plctag);
                }
                catch (Exception exc)
                {
                    // log
                    Logger.WarnFormat("PLC [{0}] error writing tag {1} : {2}", tag.PLCName, tag.Address, exc.Message);
                    RetValue = false;
                }
            }
            return RetValue;
        }

        /// <summary>
        /// Implementazione esecuzione comando di set valore plctag
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool SetPLCTag(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;
            var tag = MsgData.Tag;

            Logger.InfoFormat("{0} {1}/{2} : {3}", Message.SourceApplicationName,tag.PLCName,tag.Address,tag.Value);

            RetValue=SetPLCTag(tag);

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultSetPLCTag, MsgData, RetValue);
        }

        /// <summary>
        /// Lettura valore plctag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool GetPLCTag(ref PLCTag tag)
        {
            bool RetValue = true;

            /* verifico connessione/esistenza PLC interessato */
            if (!_PLCs.ContainsKey(tag.PLCName))
            {
                // log
                Logger.WarnFormat("PLC [{0}] not connected", tag.PLCName);

                RetValue = false;
            }
            else
            {
                try
                {
                    /* leggo tag value*/
                    var plctag = _PLCs[tag.PLCName].Read(new S7NetWrapper.Tag(tag.Address));
                    if (plctag != null)
                    {
                        tag.Value = plctag.ItemValue;
                    }
                }
                catch (Exception exc)
                {
                    // log
                    Logger.WarnFormat("PLC [{0}] error reading tag {1} : {2}", tag.PLCName, tag.Address, exc.Message);
                    RetValue = false;
                }
            }

            return RetValue;
        }


        /// <summary>
        /// Implementazione esecuzione comando di richiesta valore plctag
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool GetPLCTag(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;
            var tag = MsgData.Tag;

            Logger.InfoFormat("{0}/{1} da {2}", tag.PLCName, tag.Address, Message.SourceApplicationName);

            RetValue = GetPLCTag(ref tag);

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.ResultGetPLCTag, MsgData, RetValue);
        }

        /// <summary>
        /// Implementazione esecuzione comando di richiesta plc status
        /// </summary>
        /// <param name="Message">Dati messaggio ricevuto</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool GetPLCStatus(IIncomingMessage Message)
        {
            bool RetValue = true;
            PLCConnectionStatus ConnectionStatus;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCData;

            Logger.InfoFormat("{0} da {1}", MsgData.PLCName, Message.SourceApplicationName);

            if (!_PLCs.ContainsKey(MsgData.PLCName))
            {
                // log
                Logger.WarnFormat("PLC [{0}] not connected", MsgData.PLCName);
                ConnectionStatus = PLCConnectionStatus.NotConnected;
                RetValue = false;
            }
            else
            {
                var plc = _PLCs[MsgData.PLCName];

                switch (plc.ConnectionState)
                {
                    case S7NetWrapper.ConnectionStates.Connecting:
                        ConnectionStatus = PLCConnectionStatus.InConnection;
                        break;
                    default:
                    case S7NetWrapper.ConnectionStates.Offline:
                        ConnectionStatus = PLCConnectionStatus.NotConnected;
                        break;
                    case S7NetWrapper.ConnectionStates.Online:
                        ConnectionStatus = PLCConnectionStatus.Connected;
                        break;
                }
            }

            // invio messaggio di risposta generica
            return SendResponse(Message, MsgCodes.PLCStatus, new PLCStatusData { PLCName = MsgData.PLCName, Status = ConnectionStatus }, RetValue);
        }

        /// <summary>
        /// invio messaggio di plctag changed value a tutti i susbscibers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">dati plctag</param>
        private void plc_TagChangedValue(object sender, TagChangedValueEventArgs e)
        {
            var tag = new PLCTag();

            /* cerco il subscriber associato al tag */
            foreach (var subscriber in _Subs.ToList())
            {
                /* lista di subscrictions del subscriber */
                var list = subscriber.Value;

                if (list.Contains(new Subscription(e.PLCName, e.Tag.ItemName)))
                {
                    // trovato il subscriber, invio messaggio di tag changed
                    tag.PLCName = e.PLCName;
                    tag.Address = e.Tag.ItemName;
                    tag.Value = e.Tag.ItemValue;

                    //Create a DotNetMQ Message to send 
                    var message = mdsClient.CreateMessage();

                    //Set destination application name
                    message.DestinationApplicationName = subscriber.Key;

                    //Create a message
                    var MsgData = new PLCTagData
                    {
                        MsgCode = MsgCodes.PLCTagChanged,
                        Tag = tag
                    };

                    //Set message data
                    message.MessageData = GeneralHelper.SerializeObject(MsgData);
                    message.TransmitRule = MessageTransmitRules.NonPersistent;

                    try
                    {
                        //Send message
                        message.Send();
                        Logger.InfoFormat("{1}/{2}-{3} : Inviato a {0}", message.DestinationApplicationName, tag.PLCName, tag.Address, tag.Value.ToString());
                    }
                    catch (Exception exc)
                    {
                        // non sono riuscito a inviare il messaggio
                        Logger.WarnFormat("Messaggio non inviato - {0}", exc.Message);
                    }
                }
            }
        }


        /// <summary>
        /// Invio risposta a messaggi aventi messagedata di tipo <typeparamref name="MsgData"/>
        /// </summary>
        /// <param name="receivedMsg">Messaggio a cui viene inviata risposta</param>
        /// <param name="MsgCode">Codice messaggio di risposta</param>
        /// <param name="MessageData">Dato da inviare (contiene Validation)</param>
        /// <param name="Result">Risultato dell'operazione richiesta</param>
        /// <returns>true se tutto bene, altrimenti false</returns>
        private bool SendResponse(IIncomingMessage receivedMsg, MsgCodes MsgCode, MsgData MessageData, bool Result)
        {
            bool bOK = true;
            var message = mdsClient.CreateMessage();

            // Set message data
            MessageData.MsgCode = MsgCode;
            MessageData.validation = Result;

            // Set message params
            message.MessageData = GeneralHelper.SerializeObject(MessageData);
            message.DestinationApplicationName = receivedMsg.SourceApplicationName;
            message.TransmitRule = MessageTransmitRules.NonPersistent;


            try
            {
                //Send message
                message.Send();
                Logger.InfoFormat("Inviato msg a {0}", message.DestinationApplicationName);
            }
            catch (Exception exc)
            {
                // non sono riuscito a inviare il messaggio
                Logger.WarnFormat("Messaggio non inviato - {0}", exc.Message);
                bOK = false;
            }

            return bOK;
        }
        #endregion Private Methods
    }
}

