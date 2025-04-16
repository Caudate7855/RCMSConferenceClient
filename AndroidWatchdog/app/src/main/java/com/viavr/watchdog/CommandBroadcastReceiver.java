package com.viavr.watchdog;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

public class CommandBroadcastReceiver extends BroadcastReceiver {

    final String LOG_TAG = "WATCHDOG";

    @Override
    public void onReceive(Context context, Intent intent) {

        if(intent == null) return;

        Log.i(LOG_TAG, "CommandBroadcastReceiver onReceive: " + intent.getAction());

        Bundle extras = intent.getExtras();

        if(extras == null){
            Log.d(LOG_TAG, "CommandBroadcastReceiver intent has no extras");
            return;
        }

        Intent serviceIntent = new Intent(context, WatchdogService.class);
        serviceIntent.setAction(intent.getAction());
        serviceIntent.putExtras(extras);

        context.startForegroundService(serviceIntent);
    }
}