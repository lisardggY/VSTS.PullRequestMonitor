# VSTS.PullRequestMonitor
Background utility to listen for new/updated pull requests on your Visual Studio Team Services project and notify you when they happen.

## Background
Working on a team where no everyone shares the same office hours, I was annoyed when sometimes a pull-request would be pushed to merge a branch into `master` but no-one would notice for a couple of hours. So I wrote a small utility to poll the VSTS server for changes, and speak out a notification using `System.Speech.Synthesis`. 

## Overview
The project is currently divided into several parts:
### PullRequestMonitor
* This project contains the core `Monitor` class, which receives the connection information and polls VSTS in an infinite loop, raising events for new/updated/approved PRs via an `IObservable` which can be subscribed to for changes.
* This project also contains the definition for the INotifier interface, and several default notifiers - Write To Console and Text to Speech. It can be extended with new notifiers in client apps. (See below)
### PullRequestMonitor.Console
This is the main client app for the `Monitor` class - a console app (running without a console window in Release builds) that uses the `Monitor` class and handles new notifications using the ConsoleNotifier and TextToSpeechNotifiers. Can be modified to add more notifiers.
### PullRequestMonitor.Androi
(Not implemented yet)
A Xamarin.Android project to act as an Android client for the `Monitor` class. Will implement a new INotifier - `AndroidNotificationNotifier` - which will notify of new PRs in Android's notification center.

### Contributing
Feel free to fork the project, create a new branch, add features/fix bugs, and create a Pull Request back to my project if you want to push them upstream.
