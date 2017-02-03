using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Seq.App.DeadMansSwitch
{
    [SeqApp("Dead Mans Switch", Description = "Ensures an event happens at least once, writing an event back to the stream when it doesn't.")]
    public class DeadMansSwitchReactor : Reactor, ISubscribeTo<LogEventData>
    {
        [SeqAppSetting(
            DisplayName = "Timeout (seconds)",
            HelpText = "The number of seconds within which the events must occur to disarm the switch.")]
        public int Timeout { get; set; }

        [SeqAppSetting(DisplayName = "Log level for blown switches",
            HelpText = "Verbose, Debug, Information, Warning, Error, Fatal",
            IsOptional = true)]
        public string BlownSwitchLogLevel { get; set; }

        [SeqAppSetting(DisplayName = "Log level for armed switches",
            HelpText = "Verbose, Debug, Information, Warning, Error, Fatal",
            IsOptional = true)]
        public string ArmedSwitchLogLevel { get; set; }

        [SeqAppSetting(DisplayName = "Disable logging of the arming of a switch",
            HelpText = "If selected the arming of the switch won't be logged, only when it blows")]
        public bool DisableLogArmingOfSwitch { get; set; }

        [SeqAppSetting(
            DisplayName = "Repeat",
            HelpText = "Whether or not the timeout should repeat if there are no events. Otherwise, it will only trigger once and wait until next event. Will need each event at least once.")]
        public bool Repeat { get; set; }

        private Timer _timer;
        private Dictionary<string, EventSwitch> Switches { get; set; } = new Dictionary<string, EventSwitch>();
        protected override void OnAttached()
        {
            base.OnAttached();
            _timer = new Timer(1000);
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            if (string.IsNullOrWhiteSpace(BlownSwitchLogLevel))
            {
                BlownSwitchLogLevel = "Error";
            }
            BlownSwitchLogLevel = BlownSwitchLogLevel.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ArmedSwitchLogLevel))
            {
                ArmedSwitchLogLevel = "Information";
            }
            ArmedSwitchLogLevel = ArmedSwitchLogLevel.Trim().ToLowerInvariant();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var expiredSwitches = Switches.Where(x => x.Value.Trigger < DateTime.Now && x.Value.Armed);
            foreach (KeyValuePair<string, EventSwitch> @switch in expiredSwitches)
            {
                LogMessage($"Switch blown: '{@switch.Key}'", BlownSwitchLogLevel);
                if (Repeat)
                {
                    @switch.Value.Trigger = DateTime.Now.AddSeconds(Timeout);
                }
                else
                {
                    @switch.Value.Armed = false;
                }
            }
        }

        public void On(Event<LogEventData> evt)
        {
            bool newSwitch = false;
            if (!Switches.ContainsKey(evt.Data.RenderedMessage))
            {
                Switches[evt.Data.RenderedMessage] = new EventSwitch();
                newSwitch = true;
            }
            EventSwitch @switch = Switches[evt.Data.RenderedMessage];
            if (!@switch.Armed)
            {
                @switch.Armed = true;
                if (!DisableLogArmingOfSwitch)
                {
                    var message = $"{(newSwitch ? "New switch" : "Switch")} armed: {evt.Data.RenderedMessage}.";
                    LogMessage(message, ArmedSwitchLogLevel);
                }
            }

            @switch.Trigger = DateTime.Now.AddSeconds(Timeout);
        }

        private void LogMessage(string message, string level)
        {
            var firstChar = level[0];
            switch (firstChar)
            {
                case 'v':
                    Log.Verbose(message);
                    break;
                case 'd':
                    Log.Debug(message);
                    break;
                case 'i':
                    Log.Information(message);
                    break;
                case 'w':
                    Log.Warning(message);
                    break;
                case 'e':
                    Log.Error(message);
                    break;
                case 'f':
                    Log.Fatal(message);
                    break;
            }
        }

        private class EventSwitch
        {
            public DateTime Trigger { get; set; }
            public bool Armed { get; set; }
        }
    }
}
