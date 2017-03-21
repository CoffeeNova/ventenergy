using System.Linq;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;

namespace ventEnergy
{
    public class NetworkAdapters
    {
        public int Id { get; set; }
        public string ConnectionName { get; set; }
        public string GUID { get; set; }
        public string AdapterDescription { get; set; }
        public string AdapterDescriptionFull { get; set; }
        public string DescrForUser { get; set; }
        public ObservableCollection<NetworkAdapters> GetAdapters()
        {
            int id_count = 1;
            ObservableCollection<NetworkAdapters> ListAdapters = new ObservableCollection<NetworkAdapters>();
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            if(adapters!=null)
            {

                foreach (NetworkInterface adapter in adapters)
                {
                    // OperationalStatus adapterStatus = adapter.OperationalStatus; // текущее состояние адаптера
                    string[] descrToParts;
                    string descr;
                    descr = adapter.Description;
                    if (descr.Contains(" - "))
                    {
                        descrToParts = descr.Split('-'); //расщепляет на 2 части (чтобы откинуть часть с "минипорт планировщика бла бла")
                        //descrToParts[1] = descr; // забираем только первую часть
                        foreach (string s in descrToParts)
                        {
                            descr = s;   // забираем только первую часть
                            break;
                        }
                    }
                    // descr = descrToParts.Skip(1).ToString();     
                    ListAdapters.Add(new NetworkAdapters { DescrForUser = descr + "  (" + adapter.Name + ")", ConnectionName = adapter.Name, AdapterDescription = descr, AdapterDescriptionFull = adapter.Description, GUID = adapter.Id, Id = id_count });

                    id_count++;


                    //MessageBox.Show(""+adapter.Name);
                }
                ListAdapters.Remove(ListAdapters.Last());

            return ListAdapters;
            }
            return null;
        }
    }
}
