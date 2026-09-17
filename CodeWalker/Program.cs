using CodeWalker.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shell;

namespace CodeWalker
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SR_File_crash.log"),
                        "UnhandledException: " + e.ExceptionObject?.ToString());
                }
                catch { }
            };
            Application.ThreadException += (s, e) =>
            {
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SR_File_crash.log"),
                        "ThreadException: " + e.Exception?.ToString());
                }
                catch { }
            };
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            bool menumode = false;
            bool explorermode = false;
            bool projectmode = false;
            bool vehiclesmode = false;
            bool pedsmode = false;
            if ((args != null) && (args.Length > 0))
            {
                foreach (string arg in args)
                {
                    string argl = arg.ToLowerInvariant();
                    if (argl == "menu")
                    {
                        menumode = true;
                    }
                    if (argl == "explorer")
                    {
                        explorermode = true;
                    }
                    if (argl == "project")
                    {
                        projectmode = true;
                    }
                    if (argl == "vehicles")
                    {
                        vehiclesmode = true;
                    }
                    if (argl == "peds")
                    {
                        pedsmode = true;
                    }
                }
            }

            EnsureJumpList();

            //Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            // Check the GTA folder if launching directly into 3D World
            if (!menumode && !explorermode && !projectmode && !vehiclesmode && !pedsmode)
            {
                bool gtaOk = GTAFolder.UpdateGTAFolder(Properties.Settings.Default.RememberGTAFolder);
                if (!gtaOk)
                {
                    menumode = true;
                }
            }
#if !DEBUG
            try
            {
#endif
                if (menumode)
                {
                    Application.Run(new MenuForm());
                }
                else if (explorermode)
                {
                    Application.Run(new ExploreForm());
                }
                else if (projectmode)
                {
                    Application.Run(new Project.ProjectForm());
                }
                else if (vehiclesmode)
                {
                    Application.Run(new VehicleForm());
                }
                else if (pedsmode)
                {
                    Application.Run(new PedsForm());
                }
                else
                {
                    Application.Run(new WorldForm());
                }
#if !DEBUG
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error was encountered!\n" + ex.ToString());
                //this can happen if folder wasn't chosen, or in some other catastrophic error. meh.
            }
#endif
        }


        static void EnsureJumpList()
        {
            if (Settings.Default.JumpListInitialised) return;

            try
            {
                var cwpath = Assembly.GetEntryAssembly().Location;
                var cwdir = Path.GetDirectoryName(cwpath);

                var jtWorld = new JumpTask();
                jtWorld.ApplicationPath = cwpath;
                jtWorld.IconResourcePath = cwpath;
                jtWorld.WorkingDirectory = cwdir;
                jtWorld.Arguments = "";
                jtWorld.Title = "World View";
                jtWorld.Description = "Display the GTAV World";
                jtWorld.CustomCategory = "Launch Options";

                var jtExplorer = new JumpTask();
                jtExplorer.ApplicationPath = cwpath;
                jtExplorer.IconResourcePath = Path.Combine(cwdir, "SR File Explorer.exe");
                jtExplorer.WorkingDirectory = cwdir;
                jtExplorer.Arguments = "explorer";
                jtExplorer.Title = "RPF Explorer";
                jtExplorer.Description = "Open RPF Explorer";
                jtExplorer.CustomCategory = "Launch Options";

                var jtVehicles = new JumpTask();
                jtVehicles.ApplicationPath = cwpath;
                jtVehicles.IconResourcePath = Path.Combine(cwdir, "SR File Vehicle Viewer.exe");
                jtVehicles.WorkingDirectory = cwdir;
                jtVehicles.Arguments = "vehicles";
                jtVehicles.Title = "Vehicle Viewer";
                jtVehicles.Description = "Open Vehicle Viewer";
                jtVehicles.CustomCategory = "Launch Options";

                var jtPeds = new JumpTask();
                jtPeds.ApplicationPath = cwpath;
                jtPeds.IconResourcePath = Path.Combine(cwdir, "SR File Ped Viewer.exe");
                jtPeds.WorkingDirectory = cwdir;
                jtPeds.Arguments = "peds";
                jtPeds.Title = "Ped Viewer";
                jtPeds.Description = "Open Ped Viewer";
                jtPeds.CustomCategory = "Launch Options";

                var jumpList = new JumpList();

                jumpList.JumpItems.Add(jtWorld);
                jumpList.JumpItems.Add(jtExplorer);
                jumpList.JumpItems.Add(jtVehicles);
                jumpList.JumpItems.Add(jtPeds);

                jumpList.Apply();

                Settings.Default.JumpListInitialised = true;
                Settings.Default.Save();
            }
            catch
            { }
        }
    }
}
