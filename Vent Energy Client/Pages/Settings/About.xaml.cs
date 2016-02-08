using System.Windows.Controls;

namespace ventEnergy.Pages.Settings
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : UserControl
   {
        

        public About()
        {
            
            InitializeComponent();
            AssemblyInfo entryAssemblyInfo = new AssemblyInfo(System.Reflection.Assembly.GetEntryAssembly());
            Titletb.Text = entryAssemblyInfo.ProductTitleAndVersion;
            Descriptiontb.Text = entryAssemblyInfo.Description;
            Copyrightb.Text = entryAssemblyInfo.Copyright;
        }
    }
}
