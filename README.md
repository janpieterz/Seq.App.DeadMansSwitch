# Seq.App.DeadMansSwitch
Dead Mans Switch app

Every event (based on rendered message) will arm a switch that triggers after a certain period unless the event occurs again. 

If the event doesn't return, the switch blows and an event is written to the log. 

When it becomes armed, it can also output a message. This allows flows like knowing when things stopped and started again etc.

Repeat ensures that if it blows it will automatically re-arm it.

Log levels are customizable.

Install the [app](https://www.nuget.org/packages/Seq.App.DeadMansSwitch): `Seq.App.DeadMansSwitch`

App is similar to [Seq.App.Timeout](https://github.com/stayhard/Seq.App.Timeout) but looks to rendered message (I had a lot of timeout apps running) and has logging when armed.
