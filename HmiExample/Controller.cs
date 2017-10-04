#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;
using MDS;
using MDS.Client;
using MDS.Communication.Messages;

using OMS.Core.Communication;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Threading;
using HmiExample.Properties;
using System.Threading;
using System.Windows;
using System.Collections.ObjectModel;
#endregion

namespace HmiExample
{
    public class Controller
    {
        #region Singleton
        private static Controller _instance;
        public static Controller Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Controller();
                }
                return _instance;
            }
        }
        #endregion Singleton

        #region Public Properties

        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MDSClient mdsClient { get; private set; }

        public string ApplicationName { get; private set; }

        public string PLCServerApplicationName { get; private set; }

        public Model model { get; private set; }

        #endregion Public Properties

        #region Private Properties
        private DispatcherTimer timer { get; set; }

        private short LoopTime { get; set; }

        #endregion Private Properties

        #region Constructor
        private Controller()
        {
            // Name of this application: HMIClient
            ApplicationName = "Interface";
            // Name of the plc server application: PLCServer
            PLCServerApplicationName = "PLCServer";

            model = new Model();

            timer = new DispatcherTimer();

            // Create MDSClient object to connect to DotNetMQ
            mdsClient = new MDSClient(ApplicationName);

            // Connect to DotNetMQ server
            try
            {
                mdsClient.Connect();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message, ex);
            }

            // connetto a plcserver
            PLCServerConnect();

            // Register to MessageReceived event to get messages.
            mdsClient.MessageReceived += hmi_MessageReceived;

            // timer 
            timer.Interval = TimeSpan.FromMilliseconds(LoopTime);
            timer.Tick += timer_Tick;
            timer.IsEnabled = true;
        }
        #endregion


        #region Public Methods

        /// <summary>
        /// Connette a plcserver
        /// </summary>
        /// <returns>true if connected </returns>
        /// <returns>false if not connected</returns>
        private bool PLCServerConnect()
        {
            bool RetValue = true;

            //Create a DotNetMQ Message to send 
            var message = mdsClient.CreateMessage();

            //Set destination application name
            message.DestinationApplicationName = PLCServerApplicationName;

            //Create a message
            var MsgData = new MsgData
            {
                MsgCode = MsgCodes.ConnectSubscriber,
            };

            //Set message data
            message.MessageData = GeneralHelper.SerializeObject(MsgData);

            // message.MessageData = Encoding.UTF8.GetBytes(messageText);
            message.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send message
                message.Send();
            }
            catch
            {
                // non sono riuscito a inviare il messaggio
                Logger.InfoFormat("Messaggio non inviato");
                RetValue = false;
            }

            if (RetValue)
            {
                Logger.InfoFormat("Connesso");
            }

            return RetValue;
        }

        /// <summary>
        /// Disconnette da plcserver
        /// </summary>
        /// <returns>true if all ok</returns>
        /// <returns>false if not</returns>
        private bool PLCServerDisconnect()
        {
            bool RetValue = true;

            //Create a DotNetMQ Message to send 
            var message = mdsClient.CreateMessage();

            //Set destination application name
            message.DestinationApplicationName = PLCServerApplicationName;

            //Create a message
            var MsgData = new MsgData
            {
                MsgCode = MsgCodes.DisconnectSubscriber,
            };

            //Set message data
            message.MessageData = GeneralHelper.SerializeObject(MsgData);

            // message.MessageData = Encoding.UTF8.GetBytes(messageText);
            message.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send message
                message.Send();
            }
            catch
            {
                // non sono riuscito a inviare il messaggio
                Logger.InfoFormat("Messaggio non inviato");
                RetValue = false;
            }

            if (RetValue)
            {
                Logger.InfoFormat("Disconnesso");
            }

            return RetValue;
        }




        public void PLCAdd(string plcName, string ipAddress)
        {
            var plc = new PLCItem(plcName, ipAddress, mdsClient);

            if (plc.Connection(ApplicationName, PLCServerApplicationName))
            {
                model.ListPLCItems.Add(plc);
            }
        }

        public void PLCRemove(string plcName, string ipAddress)
        {
            var plc = new PLCItem(plcName, ipAddress, mdsClient);

            plc.Disconnection(ApplicationName, PLCServerApplicationName);

            model.ListPLCItems.Remove(plc);
        }

        public void PLCConnect(PLCItem plc)
        {
            plc.Connection(ApplicationName, PLCServerApplicationName);
        }

        public void PLCDisconnect(PLCItem plc)
        {
            plc.Disconnection(ApplicationName, PLCServerApplicationName);
        }


        public bool PLCAddTag(TagItem tag)
        {
            bool RetValue = true;

            // se già presente non lo aggiungo
            if (model.ListTagItems.Contains(tag))
            {
                Logger.InfoFormat("tag {0}/{1} già presente", tag.PLCName, tag.Address);
                return false;
            }


            //Create a DotNetMQ Message to send 
            var message = mdsClient.CreateMessage();

            //Set destination application name
            message.DestinationApplicationName = PLCServerApplicationName;

            //Create a message
            var MsgData = new PLCTagData
            {
                MsgCode = MsgCodes.SubscribePLCTag,
                Tag = new PLCTag() { PLCName = tag.PLCName, Address = tag.Address }
            };

            //Set message data
            message.MessageData = GeneralHelper.SerializeObject(MsgData);

            // message.MessageData = Encoding.UTF8.GetBytes(messageText);
            message.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send message
                message.Send();

                Logger.InfoFormat("Inviato Messaggio a {0}", message.DestinationApplicationName);

            }
            catch
            {
                // non sono riuscito a inviare il messaggio
                Logger.InfoFormat("Messaggio non inviato");
                RetValue = false;
            }

            return RetValue;
        }

        public bool PLCRemoveTag(TagItem tag)
        {
            bool RetValue = true;

            //Create a DotNetMQ Message to send 
            var message = mdsClient.CreateMessage();

            //Set destination application name
            message.DestinationApplicationName = PLCServerApplicationName;

            //Create a message
            var MsgData = new PLCTagData
            {
                MsgCode = MsgCodes.RemovePLCTag,
                Tag = new PLCTag() { PLCName = tag.PLCName, Address = tag.Address }
            };

            //Set message data
            message.MessageData = GeneralHelper.SerializeObject(MsgData);

            // message.MessageData = Encoding.UTF8.GetBytes(messageText);
            message.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send message
                message.Send();

                Logger.InfoFormat("Inviato Messaggio a {0}", message.DestinationApplicationName);
            }
            catch
            {
                // non sono riuscito a inviare il messaggio
                Logger.InfoFormat("Messaggio non inviato");
                RetValue = false;
            }

            if (RetValue)
            {
                model.ListTagItems.Remove(tag);
            }
            return RetValue;
        }

        public void Close()
        {
            foreach (var plc in model.ListPLCItems)
            {
                if (plc.ConnectionStatus == PLCConnectionStatus.Connected)
                {
                    plc.Disconnection(ApplicationName, PLCServerApplicationName);
                }
            }
            // disconnetto da plcserver
            PLCServerDisconnect();

            // prima di chiudere con mds devo pulire la coda messaggi...
            if (model.ConnectionState)
            {
                mdsClient.Disconnect();
            }
            // aspetto che venga elaborato il messaggio di risopsta alla disconnection
            while (Controller.Instance.model.ConnectionState == true)
            {
                System.Threading.Thread.Sleep(50);
            }

            Logger.InfoFormat("Stopped");
        }

        // verifica presenza di plcname nelle connessioni plc presenti 
        public bool PLCNameExists(string plcName)
        {
            bool RetVal = false;

            foreach (var plc in model.ListPLCItems)
            {
                if (plc.Name == plcName)
                {
                    RetVal = true;
                    break;
                }
            }
            return RetVal;
        }

        // verifica presenza di ipaddress nelle connessioni plc presenti 
        public bool IPAddressExists(string ipAddress)
        {
            bool RetVal = false;
            foreach (var plc in model.ListPLCItems)
            {
                if (plc.IPAddress == ipAddress)
                {
                    RetVal = true;
                    break;
                }
            }
            return RetVal;
        }

        // verifica correttezza formale nome tag ( <plcname>/<nometag>:<tipotag> )
        public bool PLCTagIsCorrect(string tagName)
        {
            bool RetVal = true;

            // split plcname e varname (es : plc4/db86.dbd58:Bool)
            string[] var1 = tagName.Split('/');
            if (var1.Count() == 2)
            {
                // split varname e var type (es : db86.dbd58:Bool)
                string[] var2 = var1[1].Split(':');
                if (var2.Count() == 2)
                {
                    var tag = new TagItem() { PLCName = var1[0], Address = var2[0], Type = var2[1] };
                    // controlla che plcname sia un plc connesso
                    if (!PLCNameExists(tag.PLCName))
                    {
                        RetVal = false;
                    }
                    else
                    {
                        // controllo tipo tag

                    }
                    // controllo esistenza tag ?
                }
                else
                {
                    RetVal = false;
                }
            }
            else
            {
                RetVal = false;
            }

            return RetVal;
        }

        // reperisce il default ip address dai settings
        public string GetDefaultIpAddress()
        {
            return Settings.Default.IpAddress;
        }

        // reperisce il default plc name dai settings
        public string GetDefaultPLCName()
        {
            return Settings.Default.PLCName;
        }

        #endregion Public Methods

        #region Private Methods

        private void timer_Tick(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// This method handles received messages from other applications via DotNetMQ.
        /// </summary>
        /// <param name="sender">Sender of the message</param>
        /// <param name="e">Message parameters</param>
        private void hmi_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                // Get message 
                var Message = e.Message;


                // Acknowledge that message is properly handled and processed. So, it will be deleted from queue.
                e.Message.Acknowledge();

                // Get message data
                var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as MsgData;

                Logger.InfoFormat("Ricevuto Messaggio {0}", MsgData.MsgCode);


                switch (MsgData.MsgCode)
                {
                    case MsgCodes.PLCTagsChanged:          /* gestione da fare */                  break;
                    case MsgCodes.PLCTagChanged:              PLCTagChanged(Message);              break;
                    case MsgCodes.PLCStatus:                  PLCStatus(Message);                  break;

                    case MsgCodes.ResultSubscribePLCTag:      ResultSubscribePLCTag(Message);      break;
                    case MsgCodes.ResultSubscribePLCTags:                                          break;
                    case MsgCodes.ResultSetPLCTag:                                                 break;
                    case MsgCodes.ResultSetPLCTags:                                                break;
                    case MsgCodes.ResultConnectSubscriber:    ResultConnectSubscriber(Message);    break;
                    case MsgCodes.ResultDisconnectSubscriber: ResultDisconnectSubscriber(Message); break;
                    case MsgCodes.ResultConnectPLC:           ResultConnectPLC(Message);           break;
                    case MsgCodes.ResultDisconnectPLC:        ResultDisconnectPLC(Message);        break;



                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message, ex);
            }

        }

        private bool RequestPLCStatus(string plcName)
        {
            bool RetValue = true;

            //Create a DotNetMQ Message to send 
            var message = mdsClient.CreateMessage();

            //Set destination application name
            message.DestinationApplicationName = PLCServerApplicationName;

            //Create a message
            var MsgData = new PLCData
            {
                MsgCode = MsgCodes.GetPLCStatus,
                PLCName = plcName
            };

            //Set message data
            message.MessageData = GeneralHelper.SerializeObject(MsgData);

            // message.MessageData = Encoding.UTF8.GetBytes(messageText);
            message.TransmitRule = MessageTransmitRules.NonPersistent;

            try
            {
                //Send message
                message.Send();
            }
            catch
            {
                // non sono riuscito a inviare il messaggio
                Logger.InfoFormat("Messaggio non inviato");
                RetValue = false;
            }

            return RetValue;
        }


        private bool PLCStatus(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCStatusData;

            Logger.InfoFormat("Ricevuto Messaggio {1}:{2} da {0}", Message.SourceApplicationName, MsgData.PLCName, MsgData.Status, MsgData.validation);

            RetValue = MsgData.validation;

            if (MsgData.validation)
            {
                var plc = model.ListPLCItems.FirstOrDefault(item => item.Name == MsgData.PLCName);
                if (plc != null)
                {
                    plc.ConnectionStatus = MsgData.Status;
                }
                else
                {
                    RetValue = false;
                }
            }

            return RetValue;
        }

        private bool ResultDisconnectSubscriber(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as MsgData;

            Logger.InfoFormat("Ricevuto Messaggio {1} da {0}", Message.SourceApplicationName, MsgData.validation);

            RetValue = MsgData.validation;

            model.ConnectionState = false;

            return RetValue;
        }

        private bool ResultConnectSubscriber(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as MsgData;

            Logger.InfoFormat("Ricevuto Messaggio {1} da {0}", Message.SourceApplicationName, MsgData.validation);

            RetValue = MsgData.validation;

            model.ConnectionState = true;

            return RetValue;
        }

        private bool ResultConnectPLC(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCConnectionData;

            Logger.InfoFormat("Ricevuto Messaggio {1}/{2}:{3} da {0}", Message.SourceApplicationName, MsgData.PLCName, MsgData.IpAddress, MsgData.validation);

            RetValue=MsgData.validation;
            
            if (MsgData.validation)
            {
                var plc = model.ListPLCItems.FirstOrDefault(item => item.Name == MsgData.PLCName );
                if (plc != null)
                {
                    RetValue=RequestPLCStatus(plc.Name);
                }
            }

            return RetValue;
        }

        private bool ResultDisconnectPLC(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCData;

            Logger.InfoFormat("Ricevuto Messaggio {1} da {0}", Message.SourceApplicationName, MsgData.PLCName);

            RetValue = MsgData.validation;

            if (MsgData.validation)
            {
                var plc = model.ListPLCItems.FirstOrDefault(item => item.Name == MsgData.PLCName);
                if (plc != null)
                {
                    // reentrant... approfondire
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        model.ListPLCItems.Remove(plc);
                    }));
                }
            }

            return RetValue;
        }



        private bool ResultSubscribePLCTag(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;

            Logger.InfoFormat("Ricevuto Messaggio {1}/{2}:{3} da {0}", Message.SourceApplicationName, MsgData.Tag.PLCName, MsgData.Tag.Address, MsgData.Tag.Value);

            TagItem tag = new TagItem() { PLCName = MsgData.Tag.PLCName, Name = MsgData.Tag.Address };
            if (tag != null)
            {
                Logger.InfoFormat("Aggiunto {0}/{1}:{2}", tag.PLCName, tag.Address, tag.Type);

                /* verifica il nome del plc tag */
                model.ListTagItems.Add(tag);
            }
            return RetValue;
        }


        private bool PLCTagChanged(IIncomingMessage Message)
        {
            bool RetValue = true;

            // get msg application data
            var MsgData = GeneralHelper.DeserializeObject(Message.MessageData) as PLCTagData;

            Logger.InfoFormat("Ricevuto Messaggio {1}/{2}:{3} da {0}", Message.SourceApplicationName, MsgData.Tag.PLCName, MsgData.Tag.Address, MsgData.Tag.Value);

            var tag = model.ListTagItems.FirstOrDefault(item => item.Address == MsgData.Tag.Address);
            if (tag != null)
            {
                // funzionano entrambe, la Invoke esegue in modo bloccante, la BeginInvoke esegue in parallelo

                //Application.Current.Dispatcher.Invoke(new Action(() =>
                //{
                //    tag.Value = MsgData.Tag.Value.ToString();
                //}));

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    tag.Value = MsgData.Tag.Value.ToString();
                }));

            }
            else
            {
                Logger.InfoFormat("Tag {0}/{1} non trovato", tag.PLCName, tag.Address);
                RetValue = false;
            }

            return RetValue;
        }


        #endregion Private Methods

    }
}
