using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S7.Net;

namespace OMS.Core.Communication
{
    /*
    * Messaggi scambiati tra applicazioni
    */
    public enum MsgCodes
    {
//      Messaggio                  Mittente    Dest.       Descrizione                               MsgData                 Risposta MsgData Risp.
//      ---------                  --------    ---------   ----------------------------------------- ----------------------- -------- -----------------
        ConnectSubscriber,      // SENDER   -> RECEIVER  : apertura sottoscrizione                   MsgData                    X     ResponseData
        DisconnectSubscriber,   // SENDER   -> RECEIVER  : chiusura sottoscrizione                   MsgData                    X     ResponseData
        SubscribePLCTag,        // SENDER   -> PLCSERVER : sottoscrizione di un tag                  PlcTagData                 X     ResponseData
        SubscribePLCTags,       // SENDER   -> PLCSERVER : sottoscrizione di un lista di tags        PlcTagsData                X     ResponseData
        RemovePLCTag,           // SENDER   -> PLCSERVER : rimozione tag                             PlcTagData                 X     ResponseData
        RemovePLCTags,          // SENDER   -> PLCSERVER : rimozione lista di tags                   PlcTagsData                X     ResponseData
        GetSubscribedPLCTags,   // SENDER   -> PLCSERVER : Richiede la lista dei tags sottoscritti   MsgData                    X     PlcTagsData
        StartCheckPLCTags,      // SENDER   -> PLCSERVER : start controllo plctags registrati        MsgData                    X     ResponseData
        StopCheckPLCTags,       // SENDER   -> PLCSERVER : stop controllo plctags registrati         MsgData                    X     ResponseData
        SetPLCTag,              // SENDER   -> PLCSERVER : set plctag                                PlcTagData                 
        SetPLCTags,             // SENDER   -> PLCSERVER : set plctags                               PlcTagsData                
        GetPLCTag,              // SENDER   -> PLCSERVER : get plctag                                PlcTagData                 X     PlcTagData
        GetPLCTags,             // SENDER   -> PLCSERVER : get plctags                               PlcTagsData                X     PlcTagsData
        GetPLCStatus,           // SENDER   -> PLCSERVER : get plc status                            PLCData                    X     PLCStatusData
        GetStatus,              // SENDER   -> PLCSERVER : get status                                MsgData                    X     PLCServerStatus
        ConnectPLC,             // SENDER   -> PLCSERVER : connect plc                               PLCConnectionData          X     PLCStatusData
        DisconnectPLC,          // SENDER   -> PLCSERVER : disconnect plc                            PLCData                    X     PLCStatusData
//      ---------                  --------    ---------   ----------------------------------------- ----------------------- -------- -----------------
        PLCTagChanged,          // PLCSERVER-> SENDER    : tag cambiato                              PlcTaData 
        PLCTagsChanged,         // PLCSERVER-> SENDER    : lista tags cambiati                       PlcTagsData 
        PLCStatusChanged,       // PLCSERVER-> SENDER    : PLC Status                                PLCStatusData 
        PLCStatus,              // PLCSERVER-> SENDER    : PLC Status                                PLCStatusData 
//      ---------                  --------    ---------   ----------------------------------------- ----------------------- -------- -----------------
        SubscribeProperty,      // SENDER   -> MANAGER   : sottoscrizione di una property            PropertyData               X     ResponseData
        SubscribeProperties,    // SENDER   -> MANAGER   : sottoscrizione di un lista di properties  PropertiesData             X     ResponseData
        RemoveProperty,         // SENDER   -> MANAGER   : rimozione di una property                 PropertyData               X     ResponseData
        RemoveProperties,       // SENDER   -> MANAGER   : rimozione di un lista di properties       PropertiesData             X     ResponseData
        //      ---------                  --------    ---------   ----------------------------------------- ----------------------- -------- -----------------
        PropertyChanged,        // MANAGER-> SENDER      : prop cambiata                             PropertyData 
        PropertiesChanged,      // MANAGER-> SENDER      : lista prop cambiate                       PropertiesData 
        
    };

    //
    // Generic Message class
    //
    [Serializable]
    public class MsgData
    {
        public MsgCodes MsgCode { get; set; }
    }

    [Serializable]
    public class ResponseData : MsgData
    {
        public bool Response { get; set; }
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


    /* ---------------------------- */


    [Serializable]
    public class PLCTag
    {
        public String PLCName { get; set; }
        public String Address { get; set; }
        public Object Value { get; set; }

    }

    [Serializable]
    public class Property
    {
        public String ObjID { get; set; }
        public String ObjPath { get; set; }
        public Object Value { get; set; }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

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
        public override int GetHashCode()
        {
            return this.ObjPath.GetHashCode();
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

