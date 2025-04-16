package com.viavr.watchdog;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.os.Build;
import android.util.Log;

public class LauncherBroadcastReceiver extends BroadcastReceiver {

    final String LOG_TAG = "WATCHDOG";

    @Override
    public void onReceive(Context context, Intent intent) {

        if(intent == null) return;

        Log.i(LOG_TAG, "LauncherBroadcastReceiver onReceive: " + intent.getAction());

        Intent serviceIntent = new Intent(context, WatchdogService.class);
        serviceIntent.setAction("com.viavr.intent.LAUNCHER_RUNNING");

        context.startForegroundService(serviceIntent);
    }
}
