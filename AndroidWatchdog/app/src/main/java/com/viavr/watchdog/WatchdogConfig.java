package com.viavr.watchdog;

import android.util.Log;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.*;

public class WatchdogConfig {

    final String LOG_TAG = "WATCHDOG";

    public String launcherVersion;                                      // launcher_version
    public String watchdogTargetVersion;                                // watchdog_version
    public String targetPackage;                                        // target_package
    public String targetPackageActivity;                                // target_package_activity
    public String runtimeApiUrl;                                        // runtime_api_url
    public int checkProcessEveryMs = 5000;                              // check_every_milliseconds
    public boolean allowStartLauncher = false;                          // allow_start_vrlauncher
    public boolean allowReboot = false;                                 // allow_reboot
    public int rebootAtHour = 6;                                        // reboot_at_hour
    public boolean allowWakeup = false;                                 // allow_wakeup
    public int wakeupEverySeconds = 1800;                               // wakeup_every_seconds
    public int wakeupRandomSeconds = 600;                               // wakeup_random_seconds
    public boolean allowSendRuntime = false;                            // allow_runtime
    public int sendRuntimeEverySeconds = 1800;                          // runtime_every_seconds
    public int sendRuntimeRandomSeconds = 600;                          // runtime_random_seconds
    public boolean allowKillApps = true;                                // allow_kill_apps
    public ArrayList<String> launcherKillWhitelist = new ArrayList<>(); // launcher_kill_whitelist

    public HashMap<String, Long> packagesForegroundTimedWhitelist;
    public String errors;

    @Override
    public String toString() {
        return "[v" + BuildConfig.VERSION_NAME + "] received WatchdogConfig{" +
                "launcherVersion='" + launcherVersion + '\'' +
                ", watchdogTargetVersion='" + watchdogTargetVersion + '\'' +
                ", targetPackage='" + targetPackage + '\'' +
                ", targetPackageActivity='" + targetPackageActivity + '\'' +
                ", runtimeApiUrl='" + runtimeApiUrl + '\'' +
                ", checkProcessEveryMs=" + checkProcessEveryMs +
                ", allowStartLauncher=" + allowStartLauncher +
                ", allowReboot='" + allowReboot + '\'' +
                ", rebootAtHour='" + rebootAtHour + '\'' +
                ", allowWakeup='" + allowWakeup + '\'' +
                ", wakeup_every_seconds='" + wakeupEverySeconds + '\'' +
                ", wakeup_random_seconds='" + wakeupRandomSeconds + '\'' +
                ", allowSendRuntime='" + allowSendRuntime + '\'' +
                ", sendRuntimeEverySeconds='" + sendRuntimeEverySeconds + '\'' +
                ", sendRuntimeRandomSeconds='" + sendRuntimeRandomSeconds + '\'' +
                ", allow_kill_apps='" + allowKillApps + '\'' +
                ", launcher_kill_whitelist=" + (launcherKillWhitelist == null ? "null" : "[" + String.join(",", launcherKillWhitelist) + "]") +
                '}';
    }

    // парсинг конфига
    public static WatchdogConfig create(JSONObject jsonObject){

        WatchdogConfig watchdogConfig = new WatchdogConfig();

        watchdogConfig.errors = null;
        watchdogConfig.packagesForegroundTimedWhitelist = new HashMap<>();

        if(jsonObject == null) return watchdogConfig;

        String errorStart = "WatchdogConfig.create(): json error: ";

        try {
            watchdogConfig.launcherVersion = jsonObject.getString("launcher_version");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.watchdogTargetVersion = jsonObject.getString("watchdog_version");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.targetPackage = jsonObject.getString("target_package");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.targetPackageActivity = jsonObject.getString("target_package_activity");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());;
        }

        try {
            watchdogConfig.runtimeApiUrl = jsonObject.getString("runtime_api_url");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());;
        }

        try {
            watchdogConfig.checkProcessEveryMs = jsonObject.getInt("check_every_milliseconds");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.allowStartLauncher = jsonObject.getBoolean("allow_start_vrlauncher");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.allowReboot = jsonObject.getBoolean("allow_reboot");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.rebootAtHour = jsonObject.getInt("reboot_at_hour");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.allowWakeup = jsonObject.getBoolean("allow_wakeup");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.wakeupEverySeconds = jsonObject.getInt("wakeup_every_seconds");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        if(watchdogConfig.wakeupEverySeconds <= 0) watchdogConfig.allowWakeup = false;

        try {
            watchdogConfig.wakeupRandomSeconds = jsonObject.getInt("wakeup_random_seconds");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.allowSendRuntime = jsonObject.getBoolean("allow_runtime");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.sendRuntimeEverySeconds = jsonObject.getInt("runtime_every_seconds");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        if(watchdogConfig.sendRuntimeEverySeconds <= 0) watchdogConfig.allowSendRuntime = false;

        try {
            watchdogConfig.sendRuntimeRandomSeconds = jsonObject.getInt("runtime_random_seconds");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            watchdogConfig.allowKillApps = jsonObject.getBoolean("allow_kill_apps");
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        try {
            JSONArray whitelist = jsonObject.getJSONArray("launcher_kill_whitelist");

            watchdogConfig.launcherKillWhitelist.clear();

            for(int i = 0; i < whitelist.length(); i++)
                watchdogConfig.launcherKillWhitelist.add(whitelist.getString(i));
        } catch (JSONException e) {
            watchdogConfig.error(errorStart + e.getMessage());
        }

        // если таргет приложение кривое
        if(watchdogConfig.targetPackage == null || watchdogConfig.targetPackageActivity == null){
            watchdogConfig.allowStartLauncher = false;
            watchdogConfig.error("WatchdogConfig.create(): watchdogConfig.allowStartLauncher set to FALSE, please check 'target_package' and 'target_package_activity'");
        }

        return watchdogConfig;
    }

    private void error(String message){
        if(errors == null) errors = "[" + message + "]";
        else errors += "[" + message + "]";

        Log.e(LOG_TAG, message);
    }

    public long getWhitelistEndTimestamp(String packageName){
        if(!packagesForegroundTimedWhitelist.containsKey(packageName)) return 0;

        return packagesForegroundTimedWhitelist.get(packageName);
    }

    public boolean checkIfWhitelisted(String packageName){

        if(!packagesForegroundTimedWhitelist.containsKey(packageName)) return false;

        if(System.currentTimeMillis() > packagesForegroundTimedWhitelist.get(packageName)){
            removeFromWhitelist(packageName);
            return false;
        }

        return true;
    }

    public void addToWhitelist(String packageName, Long accessTimeMilliseconds){
        packagesForegroundTimedWhitelist.put(packageName, System.currentTimeMillis() + accessTimeMilliseconds);
    }

    public void removeFromWhitelist(String packageName){
        packagesForegroundTimedWhitelist.remove(packageName);
    }

    public void clearWhitelist(){
        packagesForegroundTimedWhitelist.clear();
    }

    private static String[] jsonArrayToStringArray(JSONArray array) {
        if(array==null) return null;

        String[] arr = new String[array.length()];

        for(int i=0; i < arr.length; i++)
            arr[i] = array.optString(i);

        return arr;
    }
}
