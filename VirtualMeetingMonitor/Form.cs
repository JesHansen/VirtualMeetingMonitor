﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VirtualMeetingMonitor
{
    public partial class Form : System.Windows.Forms.Form
    {
        private readonly OnAirSign onAirSign = new OnAirSign();
        private readonly Network network = new Network();
        private readonly VirtualMeeting meeting = new VirtualMeeting();
        readonly Timer timer = new Timer();
        private const string LogFileName = "meetings.txt";
        private Task networkListener;


        public Form()
        {
            InitializeComponent();

            notifyIcon.Text = Text;
            notifyIcon.ContextMenuStrip = contextMenuStrip;

            timer.Interval = 1000;
            timer.Enabled = true;
            timer.Tick += OnTimerEvent;

            network.OutsideUDPTafficeReceived += Network_OutsideUDPTafficeReceived;
            networkListener = network.StartListening();

            meeting.OnMeetingStarted += Meeting_OnMeetingStarted;
            meeting.OnMeetingEnded += Meeting_OnMeetingEnded;

            // init the UI text
            meetingTxt.Text = "";
            startedTxt.Text = "";
            IpTxt.Text = "";
            EnedTxt.Text = "";
            InboundTxt.Text = "";
            OutboundTxt.Text = "";
            TotalTxt.Text = "";
            BackColor = Color.DarkGray;
        }

        private void Network_OutsideUDPTafficeReceived(IPHeader ipHeader)
        {
            meeting.ReceivedUDP(ipHeader);
        }

        private void Meeting_OnMeetingStarted()
        {
            byte red = 0;
            byte green = 255;
            byte blue = 255;

            if (meeting.IsTeamsMeeting())
            {
                red = 0;
                green = 0;
                blue = 255;
            }
            else if (meeting.IsWebExMeeting())
            {
                red = 0;
                green = 255;
                blue = 0;
            }
            else if (meeting.IsZoomMeeting())
            {
                red = 255;
                green = 0;
                blue = 0;
            }

            onAirSign.TurnOn(red, green, blue);
            LogMeeting("Started");
            BackColor = Color.Green;

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form));
            notifyIcon.Icon = ((Icon)(resources.GetObject("$this.Icon")));

            meetingTxt.Text = meeting.GetMeetingType();
            startedTxt.Text = DateTime.Now.ToString("MM/dd H:mm:ss");
            IpTxt.Text = meeting.GetIP();
            EnedTxt.Text = "";
        }

        private void Meeting_OnMeetingEnded()
        {
            onAirSign.TurnOff();
            LogMeeting("Ended  ");
            BackColor = Color.DarkGray;

            EnedTxt.Text = DateTime.Now.ToString("MM/dd H:mm:ss");

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form));
            notifyIcon.Icon = ((Icon)(resources.GetObject("notifyIcon.Icon")));
        }

        private void LogMeeting(string Msg)
        {
            string logEntry = $"{DateTime.Now:MM/dd H:mm:ss}: {Msg} - {meeting.GetIP()} {meeting.GetMeetingType()}";
            using (StreamWriter w = File.AppendText( LogFileName ))
            {
                w.WriteLine(logEntry);
            }
        }


        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
            Activate();
        }

        private void onAirOnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            onAirSign.TurnOn(0, 255, 255);
        }

        private void onAirOffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            onAirSign.TurnOff();
        }

        private void OnTimerEvent(object sender, EventArgs e)
        {
            InboundTxt.Text = meeting.GetUdpInbound().ToString();
            OutboundTxt.Text = meeting.GetUdpOutbound().ToString();
            TotalTxt.Text = meeting.GetUdpTotal().ToString();
            meeting.CheckMeetingStatus();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            network.Stop();
            networkListener.Dispose();
        }

        private void openLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessStartInfo pi = new ProcessStartInfo(LogFileName);
            pi.Arguments = Path.GetFileName(LogFileName);
            pi.UseShellExecute = true;
            pi.WorkingDirectory = Path.GetDirectoryName(LogFileName);
            pi.FileName = LogFileName;
            pi.Verb = "EDIT";

            Process.Start(pi);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                //notifyIcon.Visible = true;
            }
        }
    }
}
