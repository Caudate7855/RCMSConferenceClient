package com.picovr.androidhelper;

import android.util.Log;

public class SilentInstaller {
	private static final String TAG = "SilentInstaller";

	public static void install(String apkPath, String installerPkgName, ShellCmd.ICmdResultCallback callback, DeviceHelper deviceHelper) {
		Log.d(TAG, "AndroidDeviceHelper: install: " + "apkPath: " + apkPath + ", installPkgName: " + installerPkgName);

		ShellCmd.execute("pm install -r -g -i " + installerPkgName + " --user 0 --dont-kill " + apkPath, callback);
	}
}
