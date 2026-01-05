# [v0.61] - 01/05/2026

## Added

* Login Screen
* Login Logic
* User Model
* SQLite Database
* User info in Parameter View

# [v0.6] - 12/26/2025

## Added

* Implemented proper IDisposable pattern for AdsService
* Input validation in Parameters View
* IsLoaded verification in HomeView to prevent timer to keep running while it is unloaded
* AppSettings for Ip and Port, with method to read from App.Settings
* App.Settings.Example

## Fixed

* Handles added in Home and Parameters views
* Inconsistency in namespace HMI_PanelSaw.Models

## Removed

* Hardcoded Ip and Port in MainWindow code
* App.Settings

# [v0.5] - 12/23/25

## Added

* Active button method for all the buttons
* Responsive design layout

## Fixed

* Button logic for activation
* Twitching in Clamp and Pressure Beam status due to conflicting commands

## Removed

* HighLightButton method
