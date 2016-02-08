using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Microsoft.Win32;
using System.Management;


namespace ventEnergy
{
        public class NetworkManagement
        {
            /// <summary>
            /// Get an IP Address array
            /// </summary>
            /// <param name="complete">Показывает удачное выполнение метода</param>
            /// <param name="adapterDescription">Network adapter description</param>
            /// <returns>string[] Array of all of the IP addresses associated with the current network adapter. Starting with Windows Vista, this property can contain either IPv6 addresses or IPv4 addresses.</returns>
            public string[] GetIP(ref bool complete, string adapterDescription)
            {
                string[] ip_address = new string[] {""};
                complete = false;
                ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();
                foreach (ManagementObject objMO in objMOC)
                {
                    if (((string)objMO["Description"]).Equals(adapterDescription))
                    {
                    if((bool)objMO["IPEnabled"])
                    {
                           ip_address = (string[])objMO["IPAddress"];
                    }
                        if (ip_address != null)
                        { complete = true; return ip_address; }
                        else
                            return ip_address = new string[] { "" };
                    }
                }
                return ip_address;
            }
            /// <summary>
            /// Get an IP Gateway Address array
            /// </summary>
            /// <param name="complete">Показывает удачное выполнение метода</param>
            /// <param name="adapterDescription">Network adapter description</param>
            /// <returns>string[] Array of IP addresses of default gateways that the computer system uses.</returns>
            public string[] GetGateway(ref bool complete, string adapterDescription)
            {
                string[] gateway_address = new string[] { "" }; 
                complete = false;
                ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();
                foreach (ManagementObject objMO in objMOC)
                {
                    if (((string)objMO["Description"]).Equals(adapterDescription))
                    {
                        if ((bool)objMO["IPEnabled"])
                        {
                            gateway_address = (string[])objMO["DefaultIPGateway"];
                        }
                        if (gateway_address != null)
                        { complete = true; return gateway_address; }
                        else
                            return gateway_address = new string[] { "" };
                    }
                }
                return gateway_address;
            }
            /// <summary>
            /// Get an Subnet mask Address array
            /// </summary>
            /// <param name="complete">Показывает удачное выполнение метода</param>
            /// <param name="adapterDescription">Network adapter description</param>
            /// <returns>string[] Array of all of the subnet masks associated with the current network adapter.</returns>
            public string[] GetSubnetMask(ref bool complete, string adapterDescription)
            {
                string[] subnetMask = new string[] { "" };
                complete = false;
                ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();
                foreach (ManagementObject objMO in objMOC)
                {
                    if (((string)objMO["Description"]).Equals(adapterDescription))
                    {
                        if ((bool)objMO["IPEnabled"])
                        {
                            subnetMask = (string[])objMO["IPSubnet"];
                        }
                        if (subnetMask != null)
                        { complete = true; return subnetMask; }
                        else
                            return subnetMask = new string[] { "" };
                    }
                }
                return subnetMask;
            }
            /// <summary>
            /// Set's a new IP Address and it's Submask of the local machine
            /// </summary>
            /// <param name="ip_address">The IP Address</param>
            /// <param name="subnet_mask">The Submask IP Address</param>
            /// <param name="adapterDescription">Network adapter description</param>
            /// <remarks>Requires a reference to the System.Management namespace</remarks>
            public bool SetIP(string ip_address, string subnet_mask, string adapterDescription)
            {
                bool complete = false;
                if (!CheckIPv4(ip_address) || !CheckIPv4(subnet_mask))
                    throw new Exception();
                ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if (((string)objMO["Description"]).Equals(adapterDescription))
                    {
                        try
                        {
                            ManagementBaseObject setIP;
                            ManagementBaseObject newIP =
                                objMO.GetMethodParameters("EnableStatic");

                            newIP["IPAddress"] = new string[] { ip_address };
                            newIP["SubnetMask"] = new string[] { subnet_mask };

                            setIP = objMO.InvokeMethod("EnableStatic", newIP, null);
                            complete = true;
                            return complete;
                        }
                        catch (Exception ex)
                        {
                            //Обработчик ошибок "недопустимое значение маски и айпи адреса
                            throw;
                        }
                    }
                }
                return complete;
            }
            /// <summary>
            /// Set's a new Gateway address of the local machine
            /// </summary>
            /// <param name="gateway">The Gateway IP Address</param>
            /// <remarks>Requires a reference to the System.Management namespace</remarks>
            public bool SetGateway(string gateway, string adapterDescription)
            {
                bool complete = false;
                if (!CheckIPv4(gateway))
                    throw new Exception();
                ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if (((string)objMO["Description"]).Equals(adapterDescription))
                    {
                        try
                        {
                            ManagementBaseObject setGateway;
                            ManagementBaseObject newGateway =
                                objMO.GetMethodParameters("SetGateways");

                            newGateway["DefaultIPGateway"] = new string[] { gateway };
                            newGateway["GatewayCostMetric"] = new int[] { 1 };

                            setGateway = objMO.InvokeMethod("SetGateways", newGateway, null);
                            complete = true;
                            return complete;
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
                return complete;
            }
            /// <summary>
            /// Set's the DNS Server of the local machine
            /// </summary>
            /// <param name="adapterDescription">Name of adapter</param>
            /// <param name="DNS">DNS server addresses that are writed like "first adress, second adress, third adress, etc"</param>
            /// <remarks>Requires a reference to the System.Management namespace</remarks>
            public bool SetDNS(string DNS, string adapterDescription)
            {
                bool complete = false;
                if (!CheckIPv4(DNS))
                    throw new Exception();
                ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if (((string)objMO["Description"]).Equals(adapterDescription))
                    {
                        //  MessageBox.Show((string)objMO["caption"]);
                        //if (objMO["Caption"].Equals(NIC))
                        //{
                        try
                        {
                            ManagementBaseObject newDNS =
                                objMO.GetMethodParameters("SetDNSServerSearchOrder");
                            newDNS["DNSServerSearchOrder"] = DNS.Split(',');
                            ManagementBaseObject setDNS =
                                objMO.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                            complete = true;
                            return complete;
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                        //}
                    }
                }
                return complete;
            }
            /// <summary>
            /// Set's WINS of the local machine
            /// </summary>
            /// <param name="NIC">NIC Address</param>
            /// <param name="priWINS">Primary WINS server address</param>
            /// <param name="secWINS">Secondary WINS server address</param>
            /// <remarks>Requires a reference to the System.Management namespace</remarks>
            public bool SetWINS(string NIC, string priWINS, string secWINS, string adapterDescription)
            {
                bool complete = false;
                if (!CheckIPv4(NIC) || !CheckIPv4(priWINS) || !CheckIPv4(secWINS))
                    throw new Exception();
                ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if (((string)objMO["Description"]).Equals(adapterDescription))
                    {
                        if (objMO["Caption"].Equals(NIC))
                        {
                            try
                            {
                                ManagementBaseObject setWINS;
                                ManagementBaseObject wins =
                                objMO.GetMethodParameters("SetWINSServer");
                                wins.SetPropertyValue("WINSPrimaryServer", priWINS);
                                wins.SetPropertyValue("WINSSecondaryServer", secWINS);

                                setWINS = objMO.InvokeMethod("SetWINSServer", wins, null);
                                complete = true;
                                return complete;
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }
                }
                return complete;
            }
            /// <summary>
            /// Изменяет имя рабочей группы, только если не является чатью домена
            /// </summary>
            /// <param name="workgroup"></param>
            /// <returns></returns>
            public bool SetWorkgroup(string workgroup)
            {
                bool complete = false;
                try
                {
                    ManagementObject objMO = new ManagementObject(string.Format("Win32_ComputerSystem.Name='{0}'",
                    Environment.MachineName));
                    if (!(bool)objMO.GetPropertyValue("PartOfDomain"))
                    {
                        ManagementBaseObject wGroup = objMO.GetMethodParameters("JoinDomainOrWorkgroup");
                        wGroup.SetPropertyValue("Name", workgroup);

                        ManagementBaseObject setwGroup = objMO.InvokeMethod("JoinDomainOrWorkgroup", wGroup, null);
                        complete = true;
                        return complete;
                    }

                }
                catch (Exception) { throw; }
                return complete;
            }
            /// <summary>
            /// Возвращает название рабочей группы данной машины или домент, к которому принадлежит компьютер
            /// </summary>
            /// <returns></returns>
            public string GetWorkgroupOrDomain()
            {
                ManagementObject objMO = new ManagementObject(string.Format("Win32_ComputerSystem.Name='{0}'",
                    Environment.MachineName));

                object result = objMO["Domain"];
                return result.ToString();
            }
            /// <summary>
            /// Изменяет имя компьютера
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public bool ChangeComputerName(string name)
            {
                bool complete = false;
                try
                {
                    ManagementObject objMO = new ManagementObject(string.Format("Win32_ComputerSystem.Name='{0}'",
                    Environment.MachineName));
                    ManagementBaseObject wGroup = objMO.GetMethodParameters("Rename");
                    wGroup.SetPropertyValue("Name", name);
                    ManagementBaseObject setwGroup = objMO.InvokeMethod("Rename", wGroup, null);
                    complete = true;
                }
                catch (Exception) { throw; }
                return complete;
            }
            public bool SetDefaultPrinter(string printerName)
            {
                bool complete = false;
                try
                {
                    ManagementObject objMO = new ManagementObject(string.Format("Win32_Printer.Name='{0}'", printerName));
                }
                catch (Exception) { throw; }
                return complete;
            }
            /// <summary>
            /// method to set a specified printer as the system's default printer
            /// </summary>
            /// <param name="printerName">name of the printer we want to be default/// <returns>Returns true if successfull</returns>
            /// <exception cref="Exception">Throws exception if printer not installed</exception>
            public bool SetPrinterToDefault(string printerName)
            {
                //path we need for WMI
                string queryPath = "win32_printer.DeviceId='" + printerName + "'";

                try
                {
                    //ManagementObject for doing the retrieval
                    ManagementObject managementObj = new ManagementObject(queryPath);

                    //ManagementBaseObject which will hold the results of InvokeMethod
                    ManagementBaseObject obj = managementObj.InvokeMethod("SetDefaultPrinter", null, null);

                    //if we're null the something went wrong
                    if (obj == null)
                        throw new Exception("101");

                    //now get the return value and make our decision based on that
                    int result = (int)obj.Properties["ReturnValue"].Value;

                    if (result == 0)
                        return true;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    // вызов обработчика ошибок
                    return false;
                }
            }

            private bool CheckIPv4(string address)
            {
                try
                {

                    for(int i =0; i<4; i++)
                    {
                        if (int.Parse(address.Split('.')[i]) > 255 && int.Parse(address.Split('.')[i]) < 0)
                            return false;
                    }
                    return true;
                }
                catch { return false; }

            }
            /// <summary>
            /// Возвращает состояние NEtBIOS TCP/IP для заданного адаптера
            /// </summary>
            /// <param name="adapterDescription">Network adapter description</param>
            /// <returns>int32 : -1 - ошибка, 0 - Netbios enabled via DHCP, 1 - Netbios enabled, 2 - Netbios disabled</returns>
            public int GetTcpipNetbiosState(string adapterDescription)
            {
                int result = -1;
                ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if (((string)objMO["Description"]).Equals(adapterDescription))
                    {
                        try
                        {
                            result = Int32.Parse(objMO["TcpipNetbiosOptions"].ToString());
                          //  result = (int)objMO["TcpipNetbiosOptions"];
                        }
                        catch (Exception ex)
                        {
                            //Обработчик ошибок "недопустимое значение маски и айпи адреса
                            throw;
                        }
                    }
                }
                return result;
            }
            /// <summary>
            /// Устанавлиевает TCP/IP Netbios в состояние определенное "state"
            /// </summary>
            /// <param name="state">int32: - Enable Netbios via DHCP, 1 - Enable Netbios, 2 - Disable Netbios</param>
            /// <param name="adapterDescription">Network adapter description</param>
            /// <returns></returns>
            public bool SetTcpipNetbios(int state, string adapterDescription)
            {
                bool complete = false;


                ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if (((string)objMO["Description"]).Equals(adapterDescription))
                    {
                        try
                        {
                                ManagementBaseObject netBios = objMO.GetMethodParameters("SetTcpipNetbios");
                                netBios.SetPropertyValue("TcpipNetbiosOptions", state);

                                ManagementBaseObject setwGroup = objMO.InvokeMethod("SetTcpipNetbios", netBios, null);
                              // string t = netBios.
                               //Console.WriteLine(t); 
                                complete = true;
                                return complete;
                        }
                        catch (Exception ex)
                        {
                            //Обработчик ошибок "недопустимое значение маски и айпи адреса
                            throw;
                        }
                    }
                }
                return complete;
            }
        } 
}
