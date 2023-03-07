using System;
using System.Device.Gpio;
using System.Device.Wifi;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Iot.Device.DhcpServer;
using nanoFramework.Networking;
using nanoFramework.Runtime.Native;


namespace DoorOpener
{
    public class Program
    {
        private static WebServer server = new WebServer();
        private static GpioController sGpioController;
        private static GpioController sUiGpioController;
        static readonly ThreadStart uiControlThreadStart = new(() => UiMain());
        static readonly Thread uiControlThread = new(uiControlThreadStart);

        

        private static bool IsConnected = false;

        //General Configuration options for our relays and other things
        const int setupPin = 4;
        const int sleepTimeMinutes = 60000;
        const int relayPin = 13;
        const int ledPin = 2;


        public static void Main()
        {
            Debug.WriteLine("Door opening system is booting... please wait!");
            sGpioController = new GpioController();
            Thread.Sleep(10_000);

            Debug.WriteLine("Starting Ui Thread...");
            uiControlThread.Start();
            InitWifi();

            // Sleep this thread indefinetly waiting for now.
            Thread.Sleep(Timeout.Infinite);
        }

        private static void RefreshNetworks(object obj)
        {
            if (obj is WifiAdapter adapter)
            {
                Debug.WriteLine("starting Wi-Fi scan");
                adapter.ScanAsync();
            }
        }

        public static void UiMain()
        {
            //Wifi timer which looks for new wifi networks and saves them in a public list
            WifiAdapter wifi = WifiAdapter.FindAllAdapters()[0];
            var aTimer = new Timer(RefreshNetworks, wifi, 30000, 30000);
            wifi.AvailableNetworksChanged += Wifi_AvailableNetworksChanged;


            sUiGpioController = new GpioController();
            var led = sUiGpioController.OpenPin(2, PinMode.Output);
            led.Write(PinValue.Low);

            do
            {
                WifiLightStatus(led);
            } while (true);
        }

        public static void WifiLightStatus(GpioPin led)
        {
            if (!Wireless80211.IsEnabled() || !IsConnected)
            {
                led.Toggle();
                Thread.Sleep(125);
                led.Toggle();
                Thread.Sleep(125);
                led.Toggle();
                Thread.Sleep(125);
                led.Toggle();
                Thread.Sleep(600);
            }
            else
            {
                //Constant glow the wifi cause we're connected now :)
                led.Write(PinValue.High);
            }
        }


        static bool InitWifi()
        {
            Debug.WriteLine("Program Started, attempting to connect to Wifi");

            GpioPin setupButton = sGpioController.OpenPin(setupPin, PinMode.InputPullUp);
            if (!Wireless80211.IsEnabled() || (setupButton.Read() == PinValue.High))
            {
                Wireless80211.Disable();

                if (WirelessAP.Setup() == false)
                {
                    Debug.WriteLine("Setting up Soft AP, rebooting");
                    Power.RebootDevice();
                }


                var dhcpServer = new DhcpServer
                {
                    CaptivePortalUrl = $"http://{WirelessAP.SoftApIP}"
                };

                var dhcpInitResult = dhcpServer.Start(IPAddress.Parse(WirelessAP.SoftApIP), new IPAddress(new byte[] { 255, 255, 255, 0 }));

                if (!dhcpInitResult)
                {
                    Debug.WriteLine("Error initializing DHCP Server.");
                }

                Debug.WriteLine("Running Soft AP, waiting for clients to connect");
                Debug.WriteLine($"AP access IP Address is: {WirelessAP.GetIP()}");


                server.Start();

                return false;
            }
            else
            {
                bool connected = false;


                do
                {
                    Debug.WriteLine("Using already existing Wifi credentials, attempting connection to access point.");
                    var conf = Wireless80211.GetConfiguration();



                    if (string.IsNullOrEmpty(conf.Password))
                    {
                        connected = WifiNetworkHelper.ConnectDhcp(conf.Ssid, conf.Password, requiresDateTime: true, token: new CancellationTokenSource(60000).Token);
                    }

                    if (connected)
                    {
                        Debug.WriteLine("Connection could be established");
                        IsConnected = true;
                        return true;
                    }
                    else
                    {
                        switch (WifiNetworkHelper.Status)
                        {
                            case NetworkHelperStatus.FailedNoNetworkInterface:
                                Console.WriteLine("Could not find a valid Network Interface to use to connect to wifi. Please check your configuration or reset");
                                Thread.Sleep(Timeout.Infinite);
                                break;
                            case NetworkHelperStatus.TokenExpiredWaitingIPAddress:
                                Console.WriteLine("Could not connect recieve an IP address from the network. Please ensure your DHCP is working correctly");
                                Thread.Sleep(360000);
                                Console.WriteLine("Attempting again :)");
                                break;
                            case NetworkHelperStatus.ErrorConnetingToWiFiWhileScanning:
                                Console.WriteLine("Could not find wifi. Please ensure it's config is valid");
                                Thread.Sleep(360000);
                                Console.WriteLine("Attemtping again :)");
                                break;
                        }
                    }
                } while (connected == false);
            }


            return false;
        }


        /// <summary>
        /// Event handler for when Wifi scan completes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Wifi_AvailableNetworksChanged(WifiAdapter sender, object e)
        {
            Debug.WriteLine("Wifi_AvailableNetworksChanged - get report");

            // Get Report of all scanned Wifi networks
            WifiNetworkReport report = sender.NetworkReport;

            // Enumerate though networks looking for our network
            foreach (WifiAvailableNetwork net in report.AvailableNetworks)
            {
                // Show all networks found
                Debug.WriteLine($"Found Network. Net SSID :{net.Ssid},  BSSID : {net.Bsid},  rssi : {net.NetworkRssiInDecibelMilliwatts.ToString()},  signal : {net.SignalBars.ToString()}");
            }
        }
    }
}
