#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml;

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

        public string ApplicationName { get; private set; }

        /* associazione plcname / plctags */
        private Dictionary<string, Plc> _PLCs = new Dictionary<string, Plc>();

        /* associazione sender / subscriptions */
        private Dictionary<string, HashSet<Subscription>> _Subs = new Dictionary<string, HashSet<Subscription>>();
        #endregion Private Fields

        #region Constructor
        public plcserver()
        {
            ApplicationName = "PLCServer";

            Logger.InfoFormat("{0} application ready", ApplicationName);

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

        }
        #endregion Constructor

        #region Public Methods
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
            try
            {
                // Get message 
                var Message = e.Message;
                // Get message data
                var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as MsgData;

                // Acknowledge that message is properly handled and processed. So, it will be deleted from queue.
                e.Message.Acknowledge();

                switch (MsgData.MsgCode)
                {
                    case MsgCodes.ConnectSubscriber:
                        ConnectSubscriber(Message);
                        break;

                    case MsgCodes.DisconnectSubscriber:
                        DisconnectSubscriber(Message);
                        break;

                    case MsgCodes.SubscribePLCTag:
                        SubscribePLCTag(Message);
                        break;

                    case MsgCodes.SubscribePLCTags:
                        SubscribePLCTags(Message);
                        break;

                    case MsgCodes.RemovePLCTag:
                        RemovePLCTag(Message);
                        break;

                    case MsgCodes.RemovePLCTags:
                        RemovePLCTags(Message);
                        break;

                    case MsgCodes.GetSubscribedPLCTags:
                        GetSubscribedPLCTags(Message);
                        break;

                    case MsgCodes.SetPLCTags:
                        SetPLCTags(Message);
                        break;

                    case MsgCodes.GetPLCTags:
                        GetPLCTags(Message);
                        break;

                    case MsgCodes.SetPLCTag:
                        SetPLCTag(Message);
                        break;

                    case MsgCodes.GetPLCTag:
                        GetPLCTag(Message);
                        break;

                    case MsgCodes.StartCheckPLCTags:
                        StartCheckPLCTags(Message);
                        break;

                    case MsgCodes.StopCheckPLCTags:
                        StopCheckPLCTags(Message);
                        break;

                    case MsgCodes.GetPLCStatus:
                        GetPLCStatus(Message);
                        break;

                    case MsgCodes.ConnectPLC:
                        ConnectPLC(Message);
                        break;

                    case MsgCodes.DisconnectPLC:
                        DisconnectPLC(Message);
                        break;

                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message, ex);
            }

        }

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
#if eliminato
            /* invio messaggio di risposta */
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }

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

#if eliminato
            /* invio messaggio di risposta */
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }

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

            if (RetValue == true)
            {
                if (_PLCs.ContainsKey(MsgData.PLCName))
                {
                    // log
                    Logger.WarnFormat("PLC [{0}] already connected", MsgData.PLCName);
                    RetValue = false;
                }
                else
                {
                    try
                    {

                        var plc = new Plc(MsgData.PLCName, MsgData.Cputype, MsgData.IpAddress, MsgData.Rack, MsgData.Slot, MsgData.Delay);

                        _PLCs.Add(MsgData.PLCName, plc);

                        // associo l'evento di changed value sui tags sottoscritti
                        plc.TagChangedValue += plc_TagChangedValue;

                    }
                    catch (Exception exc)
                    {
                        // log
                        Logger.WarnFormat("PLC [{0}] error in connection : {1}", MsgData.PLCName, exc.Message);

                        RetValue = false;
                    }
                }
            }

#if eliminato
            /* invio messaggio di risposta */
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }

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

            if (RetValue == true)
            {
                if (!_PLCs.ContainsKey(MsgData.PLCName))
                {
                    // log
                    Logger.WarnFormat("PLC [{0}] not connected", MsgData.PLCName);
                    RetValue = false;
                }
                else
                {
                    try
                    {
                        /* Disconnetto */
                        var plc = _PLCs[MsgData.PLCName];

                        plc.Disconnect();

                        /* elimino dalla lista */
                        _PLCs.Remove(MsgData.PLCName);

                        /* elimino le sottoscrizioni associate */


                    }
                    catch (Exception exc)
                    {
                        // log
                        Logger.WarnFormat("PLC [{0}] error in disconnection : {1}", MsgData.PLCName, exc.Message);
                        RetValue = false;
                    }
                }
            }
#if eliminato
            /* invio messaggio di risposta */
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }

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
                /* verifico esistenza PLC interessato */
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
                        /* aggiungo tag */
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


                /* verifico esistenza PLC interessato */
                if (!_PLCs.ContainsKey(tag.PLCName))
                {
                    Logger.WarnFormat("PLC [{0}] not present", tag.PLCName);

                    RetValue = false;
                }
                else
                {
                    try
                    {
                        /* rimuovo tag */
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


        private bool SubscribePLCTag(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;
            var tag = MsgData.Tag;

            Logger.InfoFormat("{1}/{2} da {0}", Message.SourceApplicationName, tag.PLCName, tag.Address);

            RetValue = SubscribePLCTag(Message.SourceApplicationName, tag);

#if eliminato
            /* invio messaggio di risposta */
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }
        
        private bool RemovePLCTag(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;
            var tag = MsgData.Tag;

            RetValue = RemovePLCTag(Message.SourceApplicationName, tag);

#if eliminato
            /* invio messaggio di risposta */
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }


        private bool SubscribePLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagsData;
            var tagslist = MsgData.Tags;

            Logger.InfoFormat("{0}", Message.SourceApplicationName);

            foreach (var tag in tagslist)
            {
                if (!SubscribePLCTag(Message.SourceApplicationName, tag)) { RetValue = false; }
            }

#if eliminato
            /* invio messaggio di risposta */
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }

        private bool RemovePLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var msgdata = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagsData;
            var tagslist = msgdata.Tags;

            Logger.InfoFormat("{0}", Message.SourceApplicationName);

            foreach (var tag in tagslist)
            {
                if (!RemovePLCTag(Message.SourceApplicationName, tag)) { RetValue = false; }
            }

#if eliminato
            /* invio messaggio di risposta */
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }

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
            }
            else
            {
                /* costruisco la lista da inviare come risposta */
                foreach (var sub in _Subs[Message.SourceApplicationName].ToList())
                {
                    var tag = new PLCTag() { PLCName = sub.PLCName, Address = sub.TagAddress };
                    tagslist.Add(tag);
                }
            }

#if eliminato
            /* invio messaggio di risposta */
            //Create a DotNetMQ Message to respond
            var ResponseMessage = Message.CreateResponseMessage();

            //Create a message
            var ResponseData = new PLCTagsData
            {
                Tags = tagslist
            };

            //Set message data
            ResponseMessage.MessageData = GeneralHelper.SerializeObject(ResponseData);
            ResponseMessage.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send message
                ResponseMessage.Send();

                Logger.InfoFormat("Inviata Risposta a {0}", ResponseMessage.DestinationApplicationName);
            }
            catch (Exception exc)
            {
                // non sono riuscito a inviare il messaggio
                Logger.WarnFormat("Risposta non inviata - {0}", exc.Message);
                RetValue = false;
            }
#endif
            return RetValue;
        }

        // non implementata - occorre definire una classe subscriptions contenente lista plctag sottoscritti e stato (start/stop) ...
        private bool StartCheckPLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;
            return RetValue;
        }

        // non implementata - occorre definire una classe subscriptions contenente lista plctag sottoscritti e stato (start/stop) ...
        private bool StopCheckPLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;
            return RetValue;
        }

        private bool SetPLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagsData;
            var tagslist = MsgData.Tags;

            Logger.InfoFormat("{0}", Message.SourceApplicationName);

            foreach (var tag in tagslist)
            {
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
            }

            /* NON invio messaggio di risposta */
#if false
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }

        private bool GetPLCTags(IIncomingMessage Message)
        {
            bool RetValue = true;
            

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagsData;
            var tagslist = MsgData.Tags;

            Logger.InfoFormat("{0}", Message.SourceApplicationName);

            foreach (var tag in tagslist)
            {
                /* verifico connessione/esistenza PLC interessato */
                if (!_PLCs.ContainsKey(tag.PLCName))
                {
                    // log
                    Logger.WarnFormat("PLC [{1}] not connected", tag.PLCName);

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
                        }
                        tagslist.Add(tag);
                    }
                    catch (Exception exc)
                    {
                        // log
                        Logger.WarnFormat("PLC [{0}] error reading tag {1} : {2}", tag.PLCName, tag.Address, exc.Message);
                        RetValue = false;
                    }
                }
            }

#if eliminato
            /* invio messaggio di risposta */
            //Create a DotNetMQ Message to respond
            var ResponseMessage = Message.CreateResponseMessage();

            //Create a message
            var ResponseData = new PLCTagsData
            {
                Tags = tagslist
            };

            //Set message data
            ResponseMessage.MessageData = GeneralHelper.SerializeObject(ResponseData);
            ResponseMessage.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send message
                ResponseMessage.Send();

                Logger.InfoFormat("Inviata Risposta a {0}", ResponseMessage.DestinationApplicationName);
            }
            catch (Exception exc)
            {
                // non sono riuscito a inviare il messaggio
                Logger.WarnFormat("Risposta non inviata - {0}", exc.Message);
                RetValue = false;
            }
#endif // eliminato
            return RetValue;
        }

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

        private bool SetPLCTag(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;
            var tag = MsgData.Tag;

            Logger.InfoFormat("{0} {1}/{2} : {3}", Message.SourceApplicationName,tag.PLCName,tag.Address,tag.Value);

            RetValue=SetPLCTag(tag);

#if false
            /* NON invio messaggio di risposta */
            RetValue = SendResponse(Message, RetValue);
#endif
            return RetValue;
        }

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


        private bool GetPLCTag(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;
            var tag = MsgData.Tag;

            Logger.InfoFormat("{0}/{1} da {2}", tag.PLCName, tag.Address, Message.SourceApplicationName);

            RetValue = GetPLCTag(ref tag);

#if eliminato

            /* invio messaggio di risposta */

            //Create a DotNetMQ Message to respond
            var ResponseMessage = Message.CreateResponseMessage();

            //Create a message
            var ResponseData = new PLCTagData
            {
                Tag = tag
            };

            //Set message data
            ResponseMessage.MessageData = GeneralHelper.SerializeObject(ResponseData);
            ResponseMessage.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send message
                ResponseMessage.Send();

                Logger.InfoFormat("Inviata Risposta a {0}", ResponseMessage.DestinationApplicationName);
            }
            catch (Exception exc)
            {
                // non sono riuscito a inviare il messaggio
                Logger.WarnFormat("Risposta non inviata - {0}", exc.Message);
                RetValue = false;
            }
#endif
            return RetValue;
        }

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

#if eliminato

            //Create a DotNetMQ Message to send 
            var ResponseMessage = Message.CreateResponseMessage();

            //Create a message
            var MsgStatus = new PLCStatusData
            {
                PLCName = MsgData.PLCName,
                Status = ConnectionStatus
            };

            //Set message data
            ResponseMessage.MessageData = GeneralHelper.SerializeObject(MsgStatus);
            ResponseMessage.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send status message in answer
                ResponseMessage.Send();

                Logger.InfoFormat("Inviata Risposta [{0}] a {1}", ConnectionStatus, ResponseMessage.DestinationApplicationName);
            }
            catch (Exception exc)
            {
                // non sono riuscito a inviare il messaggio
                Logger.WarnFormat("Risposta non inviata - {0}", exc.Message);
                ConnectionStatus = PLCConnectionStatus.NotConnected;
                RetValue = false;
            }
#endif
            return RetValue;
        }

        // invio messaggio di tag changed value 
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
                        Logger.InfoFormat("{1}/{2}-{3} : Inviata Risposta a {0}", message.DestinationApplicationName, tag.PLCName, tag.Address, tag.Value.ToString());
                    }
                    catch (Exception exc)
                    {
                        // non sono riuscito a inviare il messaggio
                        Logger.WarnFormat("Risposta non inviata - {0}", exc.Message);
                    }
                }
            }
        }

        // invia risposta generica (bool)
        private bool SendResponse(IIncomingMessage Message, bool bOK)
        {

            //Create a DotNetMQ Message to respond
            var ResponseMessage = Message.CreateResponseMessage();
            var ResponseData = new ResponseData
            {
                Response = bOK
            };

            //Set message data
            ResponseMessage.MessageData = GeneralHelper.SerializeObject(ResponseData);
            ResponseMessage.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send message
                ResponseMessage.Send();

                Logger.InfoFormat("Inviata Risposta a {0}", ResponseMessage.DestinationApplicationName);
            }
            catch (Exception exc)
            {
                // non sono riuscito a inviare il messaggio
                Logger.WarnFormat("Risposta non inviata - {0}", exc.Message);

                bOK = false;
            }
            return bOK;
        }
        #endregion Private Methods
    }
}

