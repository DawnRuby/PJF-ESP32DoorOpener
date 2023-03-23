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
        private static readonly WebServer Server = new WebServer();
        private static GpioController _sGpioController;
        private static GpioController _sUiGpioController;
        static readonly ThreadStart UiControlThreadStart = new(() => UiMain());
        static readonly Thread UiControlThread = new(UiControlThreadStart);

        private static bool _isConnected = false;

        //General Configuration options for our relays and other things
        const int SetupPin = 4;


        public static void Main()
        {
            Debug.WriteLine("Door opening system is booting... please wait!");
            _sGpioController = new GpioController();

            Debug.WriteLine("Starting Ui Thread...");
            UiControlThread.Start();
            InitWifi();

            // Sleep this thread indefinetly waiting for now.
            Thread.Sleep(Timeout.Infinite);
        }


        public static void UiMain()
        {
            _sUiGpioController = new GpioController();
            var led = _sUiGpioController.OpenPin(2, PinMode.Output);
            led.Write(PinValue.Low);

            do
            {
                WifiLightStatus(led);
            } while (true);
        }

        public static void WifiLightStatus(GpioPin led)
        {
            if (!Wireless80211.IsEnabled() || !_isConnected)
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

            GpioPin setupButton = _sGpioController.OpenPin(SetupPin, PinMode.InputPullUp);
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


                Server.Start();

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
                        _isConnected = true;
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
    }
}
