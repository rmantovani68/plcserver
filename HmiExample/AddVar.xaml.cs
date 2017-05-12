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
    /// Interaction logic for AddVar.xaml
    /// </summary>
    public partial class AddVar : Window
    {
        public AddVar()
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

        private void AddVarWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult == true)
            {
                /* controllo le condizioni per validare il plc da aggiungere */

                if (!Controller.Instance.PLCTagIsCorrect(this.tbVarName.Text.Trim()))
                {
                    MessageBox.Show(string.Format("Tag {0} non corretto", this.tbVarName.Text.Trim()));
                    e.Cancel = true;
                }
            }
        }
    }
}
