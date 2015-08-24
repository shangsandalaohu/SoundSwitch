﻿/********************************************************************
* Copyright (C) 2015 Jeroen Pelgrims
* Copyright (C) 2015 Antoine Aflalo
* 
* This program is free software; you can redistribute it and/or
* modify it under the terms of the GNU General Public License
* as published by the Free Software Foundation; either version 2
* of the License, or (at your option) any later version.
* 
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
********************************************************************/

using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using SoundSwitch.Framework;
using SoundSwitch.Util;

namespace SoundSwitch
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (new Mutex(true, Application.ProductName, out createdNew))
            {
                if (!createdNew) return;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += Application_ThreadException;
                WindowsAPIAdapter.Start();
                //Manage the Closing events send by Windows
                //Since this app don't use a Form as "main window" the app doesn't close 
                //when it should without this.
                WindowsAPIAdapter.RestartManagerTriggered += (sender, @event) =>
                {
                    switch (@event.Type)
                    {
                        case WindowsAPIAdapter.RestartManagerEventType.Query:
                            @event.Result = new IntPtr(1);
                            break;
                        case WindowsAPIAdapter.RestartManagerEventType.EndSession:
                        case WindowsAPIAdapter.RestartManagerEventType.ForceClose:
                            Application.Exit();
                            break;
                    }
                };
                var config = ConfigurationManager.LoadConfiguration<SoundSwitchConfiguration>();
                WindowsAPIAdapter.AddThreadExceptionHandler(Application_ThreadException);
                using (var icon = new TrayIcon(new Main(config)))
                {
                    if (config.FirstRun)
                    {
                        icon.ShowSettings();
                        config.FirstRun = false;
                    }
                    Application.Run();
                    WindowsAPIAdapter.Stop();
                }
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            var message =
                $"It seems {Application.ProductName} has crashed.\nDo you want to save a log of the error that ocurred?\nThis could be useful to fix bugs. Please post this file in the issues section.";
            var result = MessageBox.Show(message, $"{Application.ProductName} crashed...", MessageBoxButtons.YesNo,
                MessageBoxIcon.Error);

            if (result == DialogResult.Yes)
            {
                var textToWrite =
                    $"{DateTime.Now}\nException:\n{e.Exception}\n\nInner Exception:\n{e.Exception.InnerException}\n\n\n\n";
                var dialog = new SaveFileDialog
                {
                    Title = @"Crash Log",
                    AddExtension = true,
                    Filter = @"*.log"
                };
                dialog.ShowDialog();
                using (var sw = new StreamWriter(dialog.OpenFile()))
                {
                    sw.Write(textToWrite);
                }
            }
        }
    }
}