using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Management;
using System.Diagnostics;

namespace CommandSpy
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		ManagementEventWatcher startWatch;
		public List<string> filters;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// From https://lolbas-project.github.io/
			filters = new List<string>() {
				"appinstaller.exe",
				"aspnet_compiler.exe",
				"at.exe",
				"atbroker.exe",
				"bash.exe",
				"bitsadmin.exe",
				"certoc.exe",
				"certreq.exe",
				"certutil.exe",
				"cmd.exe",
				"cmdkey.exe",
				"cmdl32.exe",
				"cmstp.exe",
				"configsecuritypolicy.exe",
				"control.exe",
				"csc.exe",
				"cscript.exe",
				"datasvcutil.exe",
				"desktopimgdownldr.exe",
				"dfsvc.exe",
				"diantz.exe",
				"diskshadow.exe",
				"dllhost.exe",
				"dnscmd.exe",
				"esentutl.exe",
				"eventvwr.exe",
				"expand.exe",
				"extexport.exe",
				"extrac32.exe",
				"findstr.exe",
				"finger.exe",
				"fltmc.exe",
				"forfiles.exe",
				"ftp.exe",
				"gfxdownloadwrapper.exe",
				"gpscript.exe",
				"hh.exe",
				"imewdbld.exe",
				"ie4uinit.exe",
				"ieexec.exe",
				"ilasm.exe",
				"infdefaultinstall.exe",
				"installutil.exe",
				"jsc.exe",
				"makecab.exe",
				"mavinject.exe",
				"microsoft.workflow.compiler.exe",
				"mpcmdrun.exe",
				"msbuild.exe",
				"msconfig.exe",
				"msdt.exe",
				"mshta.exe",
				"msiexec.exe",
				"netsh.exe",
				"odbcconf.exe",
				"offlinescannershell.exe",
				"onedrivestandaloneupdater.exe",
				"pcalua.exe",
				"pcwrun.exe",
				"pktmon.exe",
				"pnputil.exe",
				"powershell.exe",
				"presentationhost.exe",
				"print.exe",
				"printbrm.exe",
				"psr.exe",
				"rasautou.exe",
				"reg.exe",
				"regasm.exe",
				"regedit.exe",
				"regini.exe",
				"register-cimprovider.exe",
				"regsvcs.exe",
				"regsvr32.exe",
				"replace.exe",
				"rpcping.exe",
				"rundll32.exe",
				"runonce.exe",
				"runscripthelper.exe",
				"sc.exe",
				"schtasks.exe",
				"scriptrunner.exe",
				"settingsynchost.exe",
				"stordiag.exe",
				"syncappvpublishingserver.exe",
				"ttdinject.exe",
				"tttracer.exe",
				"vbc.exe",
				"verclsid.exe",
				"wab.exe",
				"wlrmdr.exe",
				"wmic.exe",
				"workfolders.exe",
				"wscript.exe",
				"wsreset.exe",
				"wuauclt.exe",
				"xwizard.exe",
				"adplus.exe",
				"agentexecutor.exe",
				"appvlp.exe",
				"bginfo.exe",
				"cdb.exe",
				"coregen.exe",
				"csi.exe",
				"defaultpack.exe",
				"devtoolslauncher.exe",
				"dnx.exe",
				"dotnet.exe",
				"dxcap.exe",
				"fsi.exe",
				"fsianycpu.exe",
				"mftrace.exe",
				"msdeploy.exe",
				"msxsl.exe",
				"ntdsutil.exe",
				"procdump(64).exe",
				"rcsi.exe",
				"remote.exe",
				"sqldumper.exe",
				"sqlps.exe",
				"sqltoolsps.exe",
				"squirrel.exe",
				"te.exe",
				"tracker.exe",
				"update.exe",
				"vsiisexelauncher.exe",
				"visualuiaverifynative.exe",
				"vsjitdebugger.exe",
				"wfc.exe",
				"wsl.exe",
			};
			Subscribe();
		}

		public void Subscribe()
		{
			if (startWatch != null)
			{
				startWatch.Stop();
				startWatch.Dispose();
			}
			startWatch = new ManagementEventWatcher(
				new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 0.1 WHERE TargetInstance ISA 'Win32_Process'"));
			startWatch.EventArrived += new EventArrivedEventHandler(startWatch_EventArrived);
			startWatch.Start();
			FindScrollViewer(listView).ScrollChanged += scrollViewer_ScrollChanged;
		}

		public void ChangeFilter(ItemCollection items)
		{
			filters.Clear();

			foreach (var item in items)
			{
				filters.Add((string)item);
			}
		}

		private void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
		{
			var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

			if (filters.Count != 0)
			{
				if (!filters.Contains(targetInstance["Name"]?.ToString().ToLower()))
				{
					return;
				}
			}

			if (KillNewProcesses)
			{
				try
				{
					KillProcess(Convert.ToInt32(targetInstance["ProcessID"]));
				}
				catch (System.ArgumentException) { }
				catch (System.ComponentModel.Win32Exception) { }
			}

			listView.Dispatcher.Invoke(new Action(() =>
			{
				var newItemNum = listView.Items.Add(new Finding()
				{
					Timestamp = DateTime.Now.ToString(),
					Process = targetInstance["Name"]?.ToString(),
					Command = targetInstance["CommandLine"]?.ToString(),
					PID = Convert.ToInt32(targetInstance["ProcessID"])
				});

				GridView gv = listView.View as GridView;
				if (gv != null)
				{
					foreach (var c in gv.Columns)
					{
						// Code below was found in GridViewColumnHeader.OnGripperDoubleClicked() event handler (using Reflector)
						// i.e. it is the same code that is executed when the gripper is double clicked
						if (double.IsNaN(c.Width))
						{
							c.Width = c.ActualWidth;
						}
						c.Width = double.NaN;
					}
				}

				DoAutoscroll();
			}));

			foreach (var prop in e.NewEvent.Properties)
			{
				Debug.WriteLine(prop.Name + ": " + prop.Value);
			}
		}

		public class Finding
		{
			public string Timestamp { get; set; }
			public int PID { get; set; }
			public string Process { get; set; }
			public string Command { get; set; }
		}

		private bool killNewProcesses;
		public bool KillNewProcesses
		{
			get { return killNewProcesses; }
			set { killNewProcesses = value; }
		}

		private void KillSelectedProcess(object sender, RoutedEventArgs e)
		{

			var pid = ((Finding)listView.SelectedItem).PID;
			try
			{
				KillProcess(pid);
			}
			catch (System.ArgumentException)
			{
				string messageBoxText = "The selected process is not found. It has probably already exited.";
				string caption = "Process Not Found";
				MessageBoxButton button = MessageBoxButton.OK;
				MessageBoxImage icon = MessageBoxImage.Warning;

				MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.OK);
			}
			catch (System.ComponentModel.Win32Exception)
			{
				string messageBoxText = "Access denied.";
				string caption = "Access Denied";
				MessageBoxButton button = MessageBoxButton.OK;
				MessageBoxImage icon = MessageBoxImage.Error;

				MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.OK);
			}
		}

		private void KillProcess(int PID)
		{
			Process.GetProcessById(PID).Kill();
		}

		private void CopySelectedProcessTimestamp(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(((Finding)listView.SelectedItem).Timestamp);
		}

		private void CopySelectedProcessPID(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(((Finding)listView.SelectedItem).PID.ToString());
		}

		private void CopySelectedProcessName(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(((Finding)listView.SelectedItem).Process);
		}

		private void CopySelectedProcessCommand(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(((Finding)listView.SelectedItem).Command);
		}

		private ScrollViewer FindScrollViewer(DependencyObject d)
		{
			if (d is ScrollViewer)
				return d as ScrollViewer;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
			{
				var sw = FindScrollViewer(VisualTreeHelper.GetChild(d, i));
				if (sw != null) return sw;
			}
			return null;
		}

		// Autoscrolling unless interrupted by user
		// https://stackoverflow.com/questions/28587131/detect-user-originated-scroll-in-wpf
		private volatile bool isUserScroll = true;
		public bool IsAutoScrollEnabled { get; set; } = true;
		private void DoAutoscroll()
		{
			if (!IsAutoScrollEnabled)
				return;
			var lastItem = listView.Items.GetItemAt(listView.Items.Count - 1);
			if (lastItem != null)
			{
				isUserScroll = false;
				listView.ScrollIntoView(lastItem);
			}
		}

		private void scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (e.VerticalChange == 0.0)
				return;

			if (isUserScroll)
			{
				if (e.VerticalChange > 0.0)
				{
					double scrollerOffset = e.VerticalOffset + e.ViewportHeight;
					if (Math.Abs(scrollerOffset - e.ExtentHeight) < 5.0)
					{
						// The user has tried to move the scroll to the bottom, activate autoscroll.
						IsAutoScrollEnabled = true;
					}
				}
				else
				{
					// The user has moved the scroll up, deactivate autoscroll.
					IsAutoScrollEnabled = false;
				}
			}
			isUserScroll = true;
		}

		private void ClearLog_Click(object sender, RoutedEventArgs e)
		{
			listView.Items.Clear();
			IsAutoScrollEnabled = true;
		}

		private void Options_Click(object sender, RoutedEventArgs e)
		{
			OptionsWindow options = new OptionsWindow();
			options.ShowDialog();
		}
	}
}
