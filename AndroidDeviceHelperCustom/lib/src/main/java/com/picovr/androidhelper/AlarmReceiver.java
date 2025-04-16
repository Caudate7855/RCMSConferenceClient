package com.picovr.androidhelper;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.os.PowerManager;
import android.util.Log;

public class AlarmReceiver extends BroadcastReceiver {

    @Override
    public void onReceive(Context context, Intent intent) {

        // врубаем экран
        PowerManager powerManager = (PowerManager) context.getSystemService(Context.POWER_SERVICE);
        PowerManager.WakeLock wakeLock = powerManager.newWakeLock(PowerManager.ACQUIRE_CAUSES_WAKEUP | PowerManager.SCREEN_DIM_WAKE_LOCK, "helper:AlarmReceiver");

        wakeLock.acquire();
        wakeLock.release();

        Log.i("Unity","ALARM WAKEUP id: " + intent.getIntExtra("alarmId", -1));
    }
}