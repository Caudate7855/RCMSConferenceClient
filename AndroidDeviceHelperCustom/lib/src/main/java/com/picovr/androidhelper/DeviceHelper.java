package com.picovr.androidhelper;

import android.app.ActivityManager;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.pm.IPackageDeleteObserver;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.RemoteException;
import android.text.TextUtils;
import android.util.Log;

import androidx.core.content.FileProvider;

import com.unity3d.player.UnityPlayer;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStreamReader;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.Arrays;
import java.util.List;

public class DeviceHelper extends AndroidHelper {
    private static final String TAG = "AndroidDeviceHelper";
    private static final String KEY = "pvr.launchertob.openrecenter";

    private Context mContext;

    @Override
    public void init(Context context) {
        mContext = context;
    }

    public String getPUIVersion() {
        Log.d(TAG, "getPUIVersion: " + Build.DISPLAY);
        return Build.DISPLAY;
    }

    public String getDeviceType() {
        Log.d(TAG, "getDeviceType: " + Build.MODEL);
        return Build.MODEL;
    }

    public String getSN() {
        Log.d(TAG, "getSN: " + Build.SERIAL);
        return Build.SERIAL;
    }

    public void executeShell(final String cmdLine){
        Log.d(TAG, "executeShell: " + cmdLine);

        StringBuilder output = new StringBuilder();

        try{
            Process process = Runtime.getRuntime().exec(cmdLine);

            InputStreamReader inputStreamReader = new InputStreamReader(process.getInputStream());
            BufferedReader bufferedReader = new BufferedReader(inputStreamReader);

            String line;

            while((line = bufferedReader.readLine()) != null){
                output.append(line);
                output.append("\n");
            }

            bufferedReader.close();
            inputStreamReader.close();

            UnityPlayer.UnitySendMessage("AndroidDeviceHelper", "ExecuteShellCallback", output.toString());

        }catch (IOException e){
            e.printStackTrace();
            UnityPlayer.UnitySendMessage("AndroidDeviceHelper", "ExecuteShellCallback", e.getMessage());
        }
    }

    public String executeShellString(final String cmdLine){
        Log.d(TAG, "executeShellString: " + cmdLine);

        StringBuilder output = new StringBuilder();

        try{
            Process process = Runtime.getRuntime().exec(cmdLine);

            InputStreamReader inputStreamReader = new InputStreamReader(process.getInputStream());
            BufferedReader bufferedReader = new BufferedReader(inputStreamReader);

            String line;

            while((line = bufferedReader.readLine()) != null){
                output.append(line);
                output.append("\n");
            }

            bufferedReader.close();
            inputStreamReader.close();

            return output.toString();

        }catch (IOException e){
            e.printStackTrace();

            return e.getMessage();
        }
    }

    public void executeShellDefault(String cmdLine){
        Log.d(TAG, "executeShellDefault: " + cmdLine);

        ShellCmd.execute(cmdLine);
    }

    //System
    public void silentInstall(final String apkPath, final String installerPkgName) {
        Log.d(TAG, "silentInstall: " + "apkPath: " + apkPath + ", installerPkgName: " + installerPkgName);
        File file = new File(apkPath);
        if (file.exists()) {
            try {
                SilentInstaller.install(apkPath, installerPkgName, new ShellCmd.ICmdResultCallback() {
                    public void onException(Exception arg0) {
                        Log.e(TAG, "onException silentInstall: " + arg0.getMessage());
                    }

                    public void onError(String arg0) {
                        Log.e(TAG, "onError silentInstall: " + arg0);
                        UnityPlayer.UnitySendMessage("AndroidDeviceHelper", "InstallCallback", arg0);
                    }

                    public void onComplete(String arg0) {
                        Log.e(TAG, "onComplete silentInstall");
                        UnityPlayer.UnitySendMessage("AndroidDeviceHelper", "InstallCallback", "success");
                    }
                }, this);
            } catch (IllegalStateException e) {
                Log.e(TAG, "silentInstall IllegalStateException: " + e);
            }
        } else {
            Log.e(TAG, "silentInstall: " + "Apk file doesn't exist!");
        }
    }

    //System
    public void silentUninstall(String pkgName) {
        Log.d(TAG, "silentUninstall: pkgName: " + pkgName);
        if (mContext.getPackageManager().getLaunchIntentForPackage(pkgName) != null) {
            PackageManager pm = mContext.getPackageManager();
            Class<?>[] uninstalltypes = new Class[]{String.class, IPackageDeleteObserver.class, int.class};
            Method uninstallmethod = null;
            try {
                uninstallmethod = pm.getClass().getMethod("deletePackage", uninstalltypes);
                uninstallmethod.invoke(pm, new Object[]{pkgName, new MyPackageDeleteObserver(), 0});
            } catch (IllegalAccessException | InvocationTargetException | NoSuchMethodException e) {
                e.printStackTrace();
            }
        } else {
            Log.e(TAG, "silentUninstall: " + "The specific package doesn't exist!");
        }
    }

    class MyPackageDeleteObserver extends IPackageDeleteObserver.Stub {

        @Override
        public void packageDeleted(String packageName, int returnCode) throws RemoteException {
            UnityPlayer.UnitySendMessage("AndroidDeviceHelper", "UninstallCallback", String.valueOf(returnCode));
            if (returnCode == 1) {
                Log.i(TAG, "Succeed " + returnCode);
            } else {
                Log.i(TAG, "Failed " + returnCode);
            }
        }
    }

    //System
    public void killApp(String pkgName) {
        Log.d(TAG, "killApp: pkgName: " + pkgName);
        int pid = -1;
        Log.e(TAG, "killapp: ");
        ActivityManager am = (ActivityManager) mContext.getSystemService(Context.ACTIVITY_SERVICE);
        List<ActivityManager.RunningAppProcessInfo> mRunningProcess = am.getRunningAppProcesses();
        Log.e(TAG, mRunningProcess.size() + "");
        for (ActivityManager.RunningAppProcessInfo amProcess : mRunningProcess) {
            Log.e(TAG, amProcess.processName);
            Log.e(TAG, "pid:" + amProcess.pid);
            if (amProcess.processName.equals(pkgName)) {
                pid = amProcess.pid;
                break;
            }
        }
        Log.d(TAG, "picovr.factorytest.cmd" + "kill " + pid);
        setSystemProperties("picovr.factorytest.cmd", "kill " + pid);
    }

    public void launchBrowserWithLinkInFile(int browser, String filePath) {
        Log.d(TAG, "launchBrowserWithLinkInFile: browser: " + browser + ", filePath: " + filePath);
        launchBrowser(browser, readFile(filePath));
    }

    /**
     * @param browser 0: built-in browser
     *                1: WebVR browser
     *                2: Firefox VR browser
     */
    public void launchBrowser(int browser, String link) {
        Log.d(TAG, "launchBrowser: browser: " + browser + ", link: " + link);
        Uri uri = Uri.parse(link);
        Intent intent = new Intent();
        intent.setAction("android.intent.action.VIEW");
        intent.setData(uri);
        switch (browser) {
            case 0:
                if (mContext.getPackageManager().getLaunchIntentForPackage("org.chromium.webview_shell") != null) {
                    intent.setClassName("org.chromium.webview_shell", "org.chromium.webview_shell.WebViewBrowserActivity");
                } else {
                    Log.e(TAG, "launchBrowser: " + "org.chromium.webview_shell doesn't exist!" );
                    return;
                }
                break;
            case 1:
                if (mContext.getPackageManager().getLaunchIntentForPackage("org.chromium.chrome") != null) {
                    intent.setClassName("org.chromium.chrome", "org.chromium.chrome.browser.ChromeTabbedActivity");
//                intent.setClassName("org.chromium.chrome", "org.chromium.chrome.browser.document.ChromeLauncherActivity");
                } else {
                    Log.e(TAG, "launchBrowser: " + "org.chromium.chrome doesn't exist!" );
                    return;
                }
                break;
            case 2:
                if (mContext.getPackageManager().getLaunchIntentForPackage("org.mozilla.vrbrowser") != null) {
                    intent.setClassName("org.mozilla.vrbrowser", "org.mozilla.vrbrowser.VRBrowserActivity");
                } else {
                    Log.e(TAG, "launchBrowser: " + "org.mozilla.vrbrowser doesn't exist!" );
                    return;
                }
                break;
            default:
                break;
        }
        mContext.startActivity(intent);
    }

    private String readFile(String filepath) {
        Log.d(TAG, "readFile: ");
        File file = new File(filepath);
        byte[] buff = new byte[(int) file.length()];
        FileInputStream fileInputStream = null;
        try {
            fileInputStream = new FileInputStream(file);
            fileInputStream.read(buff);
            fileInputStream.close();
            String hello = new String(buff, "utf-8");
            return hello;
        } catch (FileNotFoundException e) {
            e.printStackTrace();
            Log.e(TAG, "readFile: " + e.getMessage());
        } catch (IOException e) {
            e.printStackTrace();
            Log.e(TAG, "readFile: " + e.getMessage());
        }
        return null;
    }

    public void goToApp(String packagename) {
        Log.d(TAG, "goToApp: packageName: " + packagename);
        if (mContext.getPackageManager().getLaunchIntentForPackage(packagename) != null) {
            Intent intent = new Intent();
            PackageManager packageManager = mContext.getPackageManager();
            intent = packageManager.getLaunchIntentForPackage(packagename);
            mContext.startActivity(intent);
        } else {
            Log.e(TAG, "goToApp: " + "The specific package doesn't exist!");
        }
    }

    public void startVRShell(int way, String[] args, int type) {
        Log.d(TAG, "startVRShell: way: " + way + ", args:" + Arrays.toString(args));
        Intent vrsIntent = new Intent();
        vrsIntent.setAction("pvr.intent.action.ADAPTER");
        vrsIntent.setPackage("com.pvr.adapter");
        vrsIntent.putExtra("way", way);
        vrsIntent.putExtra("args", args);
        mContext.startService(vrsIntent);
    }

    public void startVRShell(int way, String[] args) {
        Log.d(TAG, "startVRShell: way: " + way + ", args:" + Arrays.toString(args));
        Intent vrsIntent = new Intent("pvr.intent.action.VRSHELL");
        vrsIntent.putExtra("way", way);
        vrsIntent.putExtra("args", args);
        vrsIntent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_RESET_TASK_IF_NEEDED);
        mContext.startActivity(vrsIntent);
    }

    public String getAppList() {
        GetPackageClass getPackageClass = new GetPackageClass();
        String s = getPackageClass.getAppString(mContext);
        Log.d(TAG, "getAppList: " + s);
        return s;
    }

    //需要Unity端设置接收信息
    public void registerHomeReceiver() {
        Log.d(TAG, "registerHomeReceiver: ");
        HomeKeyReceiverClass.registerHomeReceiver(mContext);
    }

    public void unregisterHomeReceiver() {
        Log.d(TAG, "unregisterHomeReceiver: ");
        HomeKeyReceiverClass.unregisterHomeReceiver(mContext);
    }

    public void openRecenterApp() {
        Log.d(TAG, "openRecenterApp: ");
        if (getSystemProperties(KEY, "0").equals("0")) {
            Log.i(TAG, KEY + " = 0");
            if (checkIfAppInstalled(mContext, "com.picovr.recenter")) {
                setSystemProperties(KEY, "1");
                openAppByComponentName(mContext, "com.picovr.recenter", "com.picovr.recenter.activity.UnityActivity");
            } else {
                Log.i(TAG, "com.picovr.recenter" + "is not installed!");
                return;
            }

        } else {
            Log.i(TAG, "persist.pvrcon.config.visible = 1");
        }
    }

    private static String getSystemProperties(String key, String defaultValue) {
        try {
            final Class<?> systemProperties = Class.forName("android.os.SystemProperties");
            final Method get = systemProperties.getMethod("get", String.class, String.class);
            String result = (String) get.invoke(null, key, defaultValue);

            Log.i("PicoVRLauncherLib", "getSystemProperties--->key=" + key + " value=" + result);

            return TextUtils.isEmpty(result) ? defaultValue : result;
        } catch (Exception e) {
            // This should never happen
            return defaultValue;
        }
    }

    private static void setSystemProperties(String key, String value) {
        try {
            final Class<?> systemProperties = Class.forName("android.os.SystemProperties");
            final Method set = systemProperties.getMethod("set", String.class,
                    String.class);
            set.invoke(null, key, value);
            Log.e(TAG, "setSystemProperties--->key=" + key + " value=" + value);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public void openAppByComponentName(Context context, String packageName, String activityName) {
        Log.d(TAG, "openAppByComponentName-------->" + packageName + "-" + activityName);
        if (!TextUtils.isEmpty(packageName) && !TextUtils.isEmpty(activityName)) {
            Intent in = new Intent(Intent.ACTION_MAIN);
            in.addCategory(Intent.CATEGORY_LAUNCHER);
            ComponentName componentName = new ComponentName(packageName,
                    activityName);
            in.setComponent(componentName);
            in.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK
                    | Intent.FLAG_ACTIVITY_RESET_TASK_IF_NEEDED);
            context.startActivity(in);
        }
    }

    public boolean checkIfAppInstalled(Context context, String packageName) {
        Log.i(TAG, "checkIfAppInstalled-------->" + packageName);
        PackageInfo packageInfo = null;
        try {
            packageInfo = context.getPackageManager().getPackageInfo(packageName, 0);
        } catch (PackageManager.NameNotFoundException e) {
            packageInfo = null;
            e.printStackTrace();
        }
        if (packageInfo == null) {
            return false;
        } else {
            return true;
        }
    }

    public void installApp(String path) {
        Log.d(TAG, "installApp: ");
        int sdk = Build.VERSION.SDK_INT;
        Log.e(TAG, "installApp：" + sdk);
        Intent intent;
        if (sdk < 24) {
            intent = new Intent(Intent.ACTION_VIEW);
            intent.setDataAndType(
                    Uri.fromFile(new File(path)),
                    "application/vnd.android.package-archive"
            );
        } else {
            File file = new File(path);

            Uri apkUri = FileProvider.getUriForFile(mContext, "com.pvr.filemanager.fileprovider", file);
//            Uri apkUri = FileProvider.getUriForFile(mContext, mContext.getApplicationInfo().packageName + ".fileprovider", file);
            intent = new Intent(Intent.ACTION_VIEW);
            intent.setDataAndType(apkUri, "application/vnd.android.package-archive");
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION);

            mContext.startActivity(intent);
        }
    }

    public boolean setSystemProp(String key, String value) {
        Log.d(TAG, "setSystemProp: " + "key: " + key + ", value: " + value);
        try {
            final Class<?> systemProperties = Class.forName("android.os.SystemProperties");
            final Method set = systemProperties.getMethod("set", String.class, String.class);
            set.invoke(null, key, value);

            Log.i(TAG, "setSystemProperties--->key=" + key + " value=" + value);
        } catch (Exception e) {
            e.printStackTrace();
            return false;
        }
        return true;
    }

    public String getSystemProp(String key, String defaultValue) {
        Log.d(TAG, "getSystemProp: " + key);
        try {
            final Class<?> systemProperties = Class.forName("android.os.SystemProperties");
            final Method get = systemProperties.getMethod("get", String.class, String.class);
            String result = (String) get.invoke(null, key, defaultValue);

            Log.i("PicoVRLauncherLib", "getSystemProperties--->key=" + key + " value=" + result);

            return TextUtils.isEmpty(result) ? defaultValue : result;
        } catch (Exception e) {
            // This should never happen
            return defaultValue;
        }
    }

    public void broadcastIntent(String action){
        Log.d(TAG, "broadcastIntent: " + action);

        Intent intent = new Intent();
        intent.setAction(action);

        mContext.sendBroadcast(intent);
    }

    public void broadcastIntentWithExtra(String action, String key, String value){
        Log.d(TAG, "broadcastIntentWithExtra: " + action + ", " + key + ", " + value);

        Intent intent = new Intent();
        intent.setAction(action);
        intent.putExtra(key, value);

        mContext.sendBroadcast(intent);
    }
}
