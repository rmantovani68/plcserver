#region Using

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;

using log4net;
using MDS;
using MDS.Client;
using MDS.Communication.Messages;

using OMS.Core.Communication;

using System.Diagnostics;


#endregion

namespace HmiExample
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public MainWindow()
        {
            Logger.InfoFormat("{0} application ready",Controller.Instance.ApplicationName);

            /* necessario per il binding in xaml */
            this.DataContext = Controller.Instance.model;    

            InitializeComponent();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var AddPLCWnd = new AddPLC();

            AddPLCWnd.tbPLCName.Text = Controller.Instance.GetDefaultPLCName();
            AddPLCWnd.tbIPAddress.Text = Controller.Instance.GetDefaultIpAddress();

            if (AddPLCWnd.ShowDialog() == true)
            {

                var plcName = AddPLCWnd.tbPLCName.Text; plcName.Trim();
                var ipAddress = AddPLCWnd.tbIPAddress.Text; ipAddress.Trim();

                /* aggiungere il tipo di plc s7300 .. 1200 */

                Controller.Instance.PLCAdd(plcName, ipAddress);

                SetPLCButtonsState(listviewPLCs.SelectedItem as PLCItem);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listviewPLCs.SelectedItem != null)
            {
                var plc = listviewPLCs.SelectedItem as PLCItem;

                try
                {
                    Controller.Instance.PLCRemove(plc.Name, plc.IPAddress);
                    
                    SetPLCButtonsState(listviewPLCs.SelectedItem as PLCItem);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                }
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (listviewPLCs.SelectedItem != null)
            {
                var plc = listviewPLCs.SelectedItem as PLCItem;

                try
                {
                    Controller.Instance.PLCConnect(plc);

                    SetPLCButtonsState(listviewPLCs.SelectedItem as PLCItem);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                }
            }
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (listviewPLCs.SelectedItem != null)
            {
                var plc = listviewPLCs.SelectedItem as PLCItem;
                try
                {
                    Controller.Instance.PLCDisconnect(plc);

                    SetPLCButtonsState(listviewPLCs.SelectedItem as PLCItem);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                }
            }
        }

        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
            // Create the startup window
            var AddVarWnd = new AddVar();
            // Show the window
            if (AddVarWnd.ShowDialog() == true)
            {
                // split plcname e varname (es : plc4/db86.dbd58:Bool)
                string[] var1 = AddVarWnd.tbVarName.Text.Split('/');
                if(var1.Count()==2){

                    // split varname e var type (es : db86.dbd58:Bool)
                    string[] var2 = var1[1].Split(':');
                    if (var2.Count() == 2)
                    {
                        var tag = new TagItem() { PLCName = var1[0], Name = var2[0], Type=var2[1] };

                        // controlla che plcname sia un plc connesso
                        // ...

                        Controller.Instance.PLCAddTag(tag);


                    }
                    else
                    {
                        // errore in nome var
                    }

                }
                else 
                {
                    // errore in nome var
                }
            }
        }

        private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
        {
            if (listviewVars.SelectedItem != null)
            {
                var tag = listviewVars.SelectedItem as TagItem;

                Controller.Instance.PLCRemoveTag(tag);
            }
            /*
            if (listviewVars.SelectedItem != null)
            {
                var tag = listviewVars.SelectedItem as TagItem;

                var rnd = new Random();

                tag.Value = rnd.Next(100).ToString();
                tag.Type = rnd.Next(100).ToString();
                tag.Name = rnd.Next(100).ToString();
            }
            */
        }

        // Method to handle the Window.Closing event.
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // disconnettere tutti i plc
            Controller.Instance.Close();
        }

        private void listviewPLCs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetPLCButtonsState(listviewPLCs.SelectedItem as PLCItem);
        }

        private void SetPLCButtonsState(PLCItem plc)
        {
            if (plc == null)
            {
                btnDelete.IsEnabled = false;
                btnDisconnect.IsEnabled = false;
                btnConnect.IsEnabled = false;
            }
            else
            {
                btnDelete.IsEnabled = true;
                if (plc.ConnectionStatus == PLCConnectionStatus.Connected)
                {
                    btnConnect.IsEnabled = false;
                    btnDisconnect.IsEnabled = true;
                }
                else
                {
                    btnConnect.IsEnabled = true;
                    btnDisconnect.IsEnabled = false;
                }
            }
        }

        private void listviewVars_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetListButtonsState(listviewVars.SelectedItem as TagItem);
        }

        private void SetListButtonsState(TagItem tag)
        {
            if (tag == null)
            {
                btnDeleteTag.IsEnabled = false;
            }
            else
            {
                btnDeleteTag.IsEnabled = true;
            }
        }
    }
}


