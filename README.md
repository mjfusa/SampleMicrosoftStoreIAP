# Sample: Microsoft Store IAP

This app is intended to help test in app purchases for .NET Windows Desktop applications using the Microsoft Store APIs. https://docs.microsoft.com/en-us/windows/uwp/monetize/in-app-purchases-and-trials

## Usage

Associate the app or game you have created in [Partner Center](https://partner.microsoft.com/dashboard) with this sample.
1. Right click the Packaging project.
2. Publish > Associate App with the Store
3. Select your application from the list.

Note: 
1. Your app or game must be published (hidden if pre-release) in order for the IAP to be accessible from the app.
2. Your app or game must be **installed from the Store** at least once in order for a license to be provisioned for it on your test machine. The app can be uninstalled and a developer build installed (like this app) after this is done.

Run this app. You will see the Durables, Unmanaged Consumables,and the Subscriptions you have defined in Parnter Center. You can also purchase each if these items.

See here for more information on configuring your game or app for testing in app purchases: https://aka.ms/testmsiap

