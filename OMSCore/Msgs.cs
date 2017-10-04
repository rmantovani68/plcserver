using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S7.Net;

namespace OMS.Core.Communication
{

    /// <summary>
    /// Messaggi scambiati tra applicazioni
    /// </summary>
    public enum MsgCodes
    {
//      Messaggio                        Mittente    Dest.       Descrizione                               MsgData                 Risposta 
//      ---------                        --------    ---------   ----------------------------------------- ----------------------- -------- 
        ConnectSubscriber,            // SENDER   -> RECEIVER  : apertura sottoscrizione (sessione)        MsgData                    X     
        DisconnectSubscriber,         // SENDER   -> RECEIVER  : chiusura sottoscrizione (sessione)        MsgData                    X     
//      ---------------                  --------    ---------   ----------------------------------------- ----------------------- -------- 
        ResultConnectSubscriber,      // RECEIVER -> SENDER    : risposta a ...                            MsgData                    X     
        ResultDisconnectSubscriber,   // RECEIVER -> SENDER    : risposta a ...                            MsgData                    X     
//      ---------------                  --------    ---------   ----------------------------------------- ----------------------- -------- 
        SubscribePLCTag,              // SENDER   -> PLCSERVER : sottoscrizione di un tag                  PlcTagData                 X     
        SubscribePLCTags,             // SENDER   -> PLCSERVER : sottoscrizione di un lista di tags        PlcTagsData                X     
        RemovePLCTag,                 // SENDER   -> PLCSERVER : rimozione tag                             PlcTagData                 X     
        RemovePLCTags,                // SENDER   -> PLCSERVER : rimozione lista di tags                   PlcTagsData                X     
        GetSubscribedPLCTags,         // SENDER   -> PLCSERVER : Richiede la lista dei tags sottoscritti   MsgData                    X     
        StartCheckPLCTags,            // SENDER   -> PLCSERVER : start controllo plctags registrati        MsgData                    X     
        StopCheckPLCTags,             // SENDER   -> PLCSERVER : stop controllo plctags registrati         MsgData                    X     
        SetPLCTag,                    // SENDER   -> PLCSERVER : set plctag                                PlcTagData                 X     
        SetPLCTags,                   // SENDER   -> PLCSERVER : set plctags                               PlcTagsData                X     
        GetPLCTag,                    // SENDER   -> PLCSERVER : get plctag                                PlcTagData                 X     
        GetPLCTags,                   // SENDER   -> PLCSERVER : get plctags                               PlcTagsData                X     
        GetPLCStatus,                 // SENDER   -> PLCSERVER : get plc status                            PLCData                    X
        GetStatus,                    // SENDER   -> PLCSERVER : get status                                MsgData                         
        ConnectPLC,                   // SENDER   -> PLCSERVER : connect plc                               PLCConnectionData          X     
        DisconnectPLC,                // SENDER   -> PLCSERVER : disconnect plc                            PLCData                    X     
//      ---------------                  --------    ---------   ----------------------------------------- ----------------------- -------- 
        ResultSubscribePLCTag,        // PLCSERVER-> SENDER    : risposta a ...                            PlcTagData                 X     
        ResultSubscribePLCTags,       // PLCSERVER-> SENDER    : risposta a ...                            PlcTagsData                X     
        ResultRemovePLCTag,           // PLCSERVER-> SENDER    : risposta a ...                            PlcTagData                 X     
        ResultRemovePLCTags,          // PLCSERVER-> SENDER    : risposta a ...                            PlcTagsData                X     
        ResultStartCheckPLCTags,      // PLCSERVER-> SENDER    : risposta a ...                            MsgData                    X     
        ResultStopCheckPLCTags,       // PLCSERVER-> SENDER    : risposta a ...                            MsgData                    X     
        ResultSetPLCTag,              // PLCSERVER-> SENDER    : risposta a ...                            PlcTagData                 X     
        ResultSetPLCTags,             // PLCSERVER-> SENDER    : risposta a ...                            PlcTagsData                X     
        ResultGetPLCTag,              // PLCSERVER-> SENDER    : risposta a ...                            PlcTagData                 X     
        ResultGetPLCTags,             // PLCSERVER-> SENDER    : risposta a ...                            PlcTagsData                X     
        ResultConnectPLC,             // PLCSERVER-> SENDER    : risposta a ...                            PLCConnectionData          X     
        ResultDisconnectPLC,          // PLCSERVER-> SENDER    : risposta a ...                            PLCData                    X     
//      ---------------                  --------    ---------   ----------------------------------------- ----------------------- -------- 
        PLCTagChanged,                // PLCSERVER-> SUBSCRIBER: tag cambiato                              PlcTaData 
        PLCTagsChanged,               // PLCSERVER-> SUBSCRIBER: lista tags cambiati                       PlcTagsData 
        PLCStatusChanged,             // PLCSERVER-> SUBSCRIBER: PLC Status                                PLCStatusData 
        SubscribedPLCTags,            // PLCSERVER-> SUBSCRIBER: lista tags sottoscritti                   PlcTagsData 

        PLCStatus,                    // PLCSERVER-> SENDER    : PLC Status                                PLCStatusData 
//      ---------------                  --------    ---------   ----------------------------------------- ----------------------- -------- 
        SubscribeProperty,            // SENDER   -> MANAGER   : sottoscrizione di una property            PropertyData               X
        SubscribeProperties,          // SENDER   -> MANAGER   : sottoscrizione di un lista di properties  PropertiesData             X
        RemoveProperty,               // SENDER   -> MANAGER   : rimozione di una property                 PropertyData               X
        RemoveProperties,             // SENDER   -> MANAGER   : rimozione di un lista di properties       PropertiesData             X
        GetSubscribedProperties,      // SENDER   -> MANAGER   : Richiede lista delle props sottoscritte   MsgData                    X     
//      ---------------                  --------    ---------   ----------------------------------------- ----------------------- -------- 
        ResultSubscribeProperty,      // MANAGER  -> SENDER    : risposta a ...                            PropertyData               
        ResultSubscribeProperties,    // MANAGER  -> SENDER    : risposta a ...                            PropertiesData             
        ResultRemoveProperty,         // MANAGER  -> SENDER    : risposta a ...                            PropertyData               
        ResultRemoveProperties,       // MANAGER  -> SENDER    : risposta a ...                            PropertiesData             
//      ---------------                  --------    ---------   ----------------------------------------- ----------------------- -------- 
        PropertyChanged,              // MANAGER  -> SUBSCRIBER: prop cambiata                             PropertyData
        PropertiesChanged,            // MANAGER  -> SUBSCRIBER: lista prop cambiate                       PropertiesData
        SubscribedProperties,         // MANAGER  -> SUBSCRIBER: lista props sottoscritte                  PropertiesData
    };

    //
    // Generic Message class
    //
    [Serializable]
    public class MsgData
    {
        public bool validation { get; set; }
        public MsgCodes MsgCode { get; set; }
    }

    [Serializable]
    public class PLCData : MsgData
    {
        public string PLCName { get; set; }
    }

    [Serializable]
    public class PLCTagData : MsgData
    {
        public PLCTag Tag { get; set; }
    }

    [Serializable]
    public class PLCTagsData : MsgData
    {
        public List<PLCTag> Tags { get; set; }
    }

    [Serializable]
    public class PLCStatusData : PLCData
    {
        public PLCConnectionStatus Status { get; set; }
    }

    [Serializable]
    public class PLCConnectionData : PLCData
    {
        public CpuType Cputype { get; set; }
        public string IpAddress { get; set; }
        public short Rack { get; set; }
        public short Slot { get; set; }
        public int Delay { get; set; }
    }

    [Serializable]
    public class PropertyData : MsgData
    {
        public Property Prop { get; set; }
    }

    [Serializable]
    public class PropertiesData : MsgData
    {
        public List<Property> Props { get; set; }
    }

    [Serializable]
    public class ResponseData : MsgData
    {
        public bool Response { get; set; }
    }


    /* ---------------------------- */


    [Serializable]
    public class PLCTag
    {public bool Validation { get; set; }
        public String PLCName { get; set; }
        public String Address { get; set; }
        public Object Value { get; set; }

        public override bool Equals(Object obj)
        {
            return Equals(obj as PLCTag);
        }

        public bool Equals(PLCTag tag)
        {
            // If parameter is null return false:
            if (tag == null)
            {
                return false;
            }

            // Return true if either fields match:
            return (this.PLCName == tag.PLCName);
        }

        public override string ToString()
        {
            return (this.PLCName + "/" + this.Address);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public static bool operator ==(PLCTag t1, PLCTag t2)
        {
            if (((object)t1) == ((object)t2)) return true;
            if (((object)t1) == null || ((object)t2) == null) return false;

            return t1.Equals(t2);
        }

        public static bool operator !=(PLCTag t1, PLCTag t2)
        {
            return !(t1 == t2);
        }
    }

    [Serializable]
    public class Property
    {
        public bool Validation { get; set; }
        public String ObjID { get; set; }
        public String ObjPath { get; set; }
        public Object Value { get; set; }

        public override bool Equals(Object obj)
        {
            return Equals(obj as Property);
        }

        public bool Equals(Property prop)
        {
            // If parameter is null return false:
            if (prop == null)
            {
                return false;
            }

            // Return true if either fields match:
            return (ObjPath == prop.ObjPath);
        }

        public override string ToString()
        {
            return this.ObjPath;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public static bool operator ==(Property prop1, Property prop2)
        {
            if (((object)prop1) == ((object)prop2)) return true;
            if (((object)prop1) == null || ((object)prop2) == null) return false;

            return prop1.Equals(prop2);
        }

        public static bool operator !=(Property prop1, Property prop2)
        {
            return !(prop1 == prop2);
        }
    }

    [Serializable]
    public enum PLCConnectionStatus
    {
        Connected,
        NotConnected,
        InConnection,
    }
}

