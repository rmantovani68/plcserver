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
        SubscribePLCTag,        // SENDER   -> PLCSERVER : sottoscrizione di un tag                  PlcTagData                 X     ResponseData
        SubscribePLCTags,       // SENDER   -> PLCSERVER : sottoscrizione di un lista di tags        PlcTagsData                X     ResponseData
        RemovePLCTag,           // SENDER   -> PLCSERVER : rimozione tag                             PlcTagData                 X     ResponseData
        RemovePLCTags,          // SENDER   -> PLCSERVER : rimozione lista di tags                   PlcTagsData                X     ResponseData
        GetSubscribedPLCTags,   // SENDER   -> PLCSERVER : Richiede la lista dei tags sottoscritti   MsgData                    X     PlcTagsData
        StartCheckPLCTags,      // SENDER   -> PLCSERVER : start controllo plctags registrati        MsgData                    X     ResponseData
        StopCheckPLCTags,       // SENDER   -> PLCSERVER : stop controllo plctags registrati         MsgData                    X     ResponseData
        SetPLCTags,             // SENDER   -> PLCSERVER : set plctags                               PlcTagsData                X     ResponseData
        GetPLCTags,             // SENDER   -> PLCSERVER : get plctags                               PlcTagsData                X     PlcTagsData
        SetPLCTag,              // SENDER   -> PLCSERVER : set plctag                                PlcTagData                 X     ResponseData
        GetPLCTag,              // SENDER   -> PLCSERVER : get plctag                                PlcTagData                 X     PlcTagData
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


    /* ---------------------------- */


    [Serializable]
    public class PLCTag
    {
        public String PLCName { get; set; }
        public String Name { get; set; }
        public Object Value { get; set; }
    }

    [Serializable]
    public enum PLCConnectionStatus
    {
        Connected,
        NotConnected,
        InConnection,
    }

}

