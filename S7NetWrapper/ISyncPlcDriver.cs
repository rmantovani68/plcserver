#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 
#endregion

namespace S7NetWrapper
{
    public interface IPlcSyncDriver
    {        
        ConnectionStates ConnectionState { get; }
        
        void Connect();

        void Disconnect();        

        List<Tag> ReadItems(List<Tag> itemList);

        Tag ReadItem(Tag item);

        void WriteItems(List<Tag> itemList);

        void WriteItem(Tag item); 

    }   
}
