#region Using
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
#endregion 

namespace HmiExample
{
    /// <summary>
    /// Interaction logic for AddPLC.xaml
    /// </summary>
    public partial class AddPLC : Window
    {
        public AddPLC()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void AddPLCWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult == true)
            {
                /* controllo le conizioni per validare il plc da aggiungere */
                
                if (Controller.Instance.PLCNameExists(this.tbPLCName.Text.Trim()))
                {
                    MessageBox.Show(string.Format("PLC {0} già presente", this.tbPLCName.Text.Trim())); 
                    e.Cancel = true;
                }
                else
                {
                    if (Controller.Instance.IPAddressExists(this.tbIPAddress.Text.Trim()))
                    {
                        MessageBox.Show(string.Format("Indirizzo IP [{0}] già utilizzato dal plc [{1}]", this.tbIPAddress.Text.Trim(), this.tbPLCName.Text.Trim()));
                        e.Cancel = true;
                    }
                }
            }
        }
    }
}
