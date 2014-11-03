using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Microsoft.Xna.Framework.Input;

namespace KOSC
{
    public class OSCInfoObject
    {
        private int portNumber;
        private IPAddress oscIpAddress;
        private int startChannel;
        private String startString;

        /// <summary>
        /// Holds the relevant information about the osc parameters of which to send messages to
        /// ,also handles the parsing of those values from the GUI
        /// </summary>
        public OSCInfoObject()
        {
            this.portNumber = 9001;
            this.oscIpAddress = IPAddress.Loopback;
            this.startChannel = 1;
            this.startString = "/lj/osc/";
        }

        public int getPortNumber()
        {
            return this.portNumber;
        }

        public IPAddress getOscIpAddress()
        {
            return this.oscIpAddress;
        }

        public int getStartChannel()
        {
            return this.startChannel;
        }

        public String getStartString()
        {
            return this.startString;
        }

        public void setPortNumber(String _portNumber)
        {
            try
            {
                this.portNumber = int.Parse(_portNumber);
            }
            catch (Exception)
            {
                Console.Out.WriteLine("The Port you entered is a non numerical format, try again");
                this.portNumber = 9001;
            }
        }

        public void setOSCIPAddress(String _ipAddress)
        {
            try
            {
                this.oscIpAddress = IPAddress.Parse(_ipAddress);
            }
            catch (Exception)
            {
                Console.Out.WriteLine("The IP Addresss you entered is not in the correct format, try again");
                this.oscIpAddress = IPAddress.Loopback;
            }          
        }

        public void setOSCIPAddressIP(IPAddress _ipAddress)
        {
                this.oscIpAddress = _ipAddress;
        }

        public void setStartChannel(String _startChannel)
        {
            try
            {
                this.startChannel = int.Parse(_startChannel);
            }
            catch (Exception)
            {
                Console.Out.WriteLine("The Start Channel you entered is a non numerical format, try again");
                this.startChannel = 1;
                this.startString = "/lj/osc/";
                Console.Out.WriteLine("Because your settings are wrong, the settings have been defaulted back to, Port: 9001, " + "\r\n" 
                    + "IP: 127.0.0.1 (This Computer), Channel: 1, Message: /lj/osc/");
            }            
        }

        public void setStartChannelInt(int _startChannel)
        {
            this.startChannel = _startChannel;
        }

        public void setStartString(String _startString)
        {
            this.startString = _startString;
        }
    }
}
