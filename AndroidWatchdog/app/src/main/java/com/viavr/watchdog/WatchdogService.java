package com.viavr.watchdog;
import android.app.*;
import android.content.*;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.os.*;
import android.text.format.DateFormat;
import android.util.Log;
import androidx.annotation.Nullable;
import androidx.core.app.NotificationCompat;
import com.android.volley.*;
import com.android.volley.toolbox.StringRequest;
import com.android.volley.toolbox.Volley;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.concurrent.*;

// adb shell am startservice com.viavr.watchdog/.WatchdogService API < 26
// adb shell am start-foreground-service com.viavr.watchdog/.WatchdogService API >= 26
// adb shell am force-stop com.viavr.watchdog
public class WatchdogService extends Service {

    final String LOG_TAG = "WATCHDOG";
    final String CONFIG_PATH = "watchdog_config.json";
    final String APPS_URL = "https://api.vrcinema.pro/api/v1/device/apps";

    private static Notification notification;
    private ScheduledFuture<?> scheduler;
    private ScheduledFuture<?> appsScheduler;

    SharedPreferences sharedPreferences;

    private WatchdogConfig watchdogConfig;

    private final Random random = new Random();

    private String lastForegroundApp; // приложуха запущенная из лаунчера

    private long targetRebootTime;

    private long nextWakeupMillis;
    private long nextSendRuntimeMillis;

    private String token = null;

    RequestQueue requestQueue;
    private boolean appsRequestInProcess = false;

    private PowerManager powerManager;
    BatteryManager batteryManager;

    @Override
    public void onCreate() {
        super.onCreate();

        Log.d(LOG_TAG, "Watchdog service v" + BuildConfig.VERSION_NAME + ": onCreate()");

        powerManager = (PowerManager)getSystemService(Context.POWER_SERVICE);
        batteryManager = (BatteryManager)this.getSystemService(BATTERY_SERVICE);

        if (notification != null) return;

        sharedPreferences = getSharedPreferences("watchdog_preferences", Context.MODE_PRIVATE);

        if(sharedPreferences.contains("token"))
            token = sharedPreferences.getString("token", null);

        // https://google.github.io/volley/simple.html
        requestQueue = Volley.newRequestQueue(this);

        // https://commonsware.com/blog/2017/04/11/android-o-implicit-broadcast-ban.html
        registerReceiver(new CommandBroadcastReceiver(), new IntentFilter("com.viavr.intent.WATCHDOG_COMMAND"));
        registerReceiver(new LauncherBroadcastReceiver(), new IntentFilter("com.viavr.intent.LAUNCHER_RUNNING"));

        // читаем конфиг
        initializeConfig();

        // для API >= 26 нужно создавать канал
        NotificationChannel notificationChannel = new NotificationChannel("watchdog", "Watchdog Service", NotificationManager.IMPORTANCE_NONE);
        NotificationManager notificationManager = (NotificationManager)getSystemService(Context.NOTIFICATION_SERVICE);
        notificationManager.createNotificationChannel(notificationChannel);

        notification =
                new NotificationCompat.Builder(this, "watchdog").
                        setOngoing(true).
                        setSmallIcon(R.mipmap.ic_launcher).
                        setContentTitle("Watchdog").
                        setContentText("Service is running").
                        build();

        // старт сервиса
        startForeground(1, notification);

        nextWakeupMillis = getNextWakeupTime();
        nextSendRuntimeMillis = getNextSendRuntimeTime();

        // повторяющаяся таска
        ScheduledExecutorService service = Executors.newSingleThreadScheduledExecutor();

        // предыдущую офаем
        if(scheduler != null)
            scheduler.cancel(true);

        // непосредственно таска
        scheduler = service.scheduleAtFixedRate(() -> {

            // проверяем ребут
            if(watchdogConfig.allowReboot && checkRebootTime()) return;

            // проверяем вейкап
            if(watchdogConfig.allowWakeup) checkWakeupTime();

            // проверяем пересылку данных шлема
            if(watchdogConfig.allowSendRuntime) checkSendRuntimeTime();

            // проверять нечего
            if(watchdogConfig.targetPackage == null) return;

            String foregroundApp = getForegroundApp();

            // если активен targetPackage то всё ок
            if(foregroundApp.equals(watchdogConfig.targetPackage)) {
                // чистим предыдущее приложение
                lastForegroundApp = checkProcessForKill(lastForegroundApp);

                return;
            }

            lastForegroundApp = foregroundApp;

            boolean needLaunch = true;
            String message = null;

            if(!isPackageInstalled(watchdogConfig.targetPackage)){
                // если лаунчер не установлен
                needLaunch = false;

                message = "Target package " + watchdogConfig.targetPackage + " is not installed!";
                Log.e(LOG_TAG, message);

            }else if(watchdogConfig.checkIfWhitelisted(foregroundApp)){
                // если приложение в вайтлисте
                needLaunch = false;

                if(isProcessExists(watchdogConfig.targetPackage) && !watchdogConfig.launcherKillWhitelist.contains(foregroundApp))
                    killPackage(watchdogConfig.targetPackage);

                Log.i(LOG_TAG, foregroundApp + " is whitelisted, milliseconds left: " + (watchdogConfig.getWhitelistEndTimestamp(foregroundApp) - System.currentTimeMillis()));
            }else if(!isProcessExists(watchdogConfig.targetPackage)){
                // если процесс лаунчера не запущен
                message = watchdogConfig.targetPackage + " process not exists";
                Log.i(LOG_TAG, message);
            }

            if(needLaunch){
                if(!watchdogConfig.allowStartLauncher) {
                    // если старт лаунчера НЕ разрешен
                    return;
                }

                Log.i(LOG_TAG, "Active app: " + foregroundApp);

                // запускаем наш лаунчер
                launchPackage(watchdogConfig.targetPackage, watchdogConfig.targetPackageActivity);

                // чистим предыдущее приложение
                lastForegroundApp = checkProcessForKill(lastForegroundApp);
            }

        }, 0, watchdogConfig.checkProcessEveryMs, TimeUnit.MILLISECONDS);


        // повторяющаяся таска
        ScheduledExecutorService appsService = Executors.newSingleThreadScheduledExecutor();

        // предыдущую офаем
        if(appsScheduler != null)
            appsScheduler.cancel(true);

        appsScheduler = appsService.scheduleAtFixedRate(() -> {

            if(!appsRequestInProcess && token != null && !getForegroundApp().equals(watchdogConfig.targetPackage))
            {
                appsRequestInProcess = true;

                StringRequest stringRequest = new StringRequest(Request.Method.GET, APPS_URL,
                        (Response.Listener<String>) response -> {

                            try {
                                JSONArray jsonArray = new JSONArray(response); // gson почему-то не парсит поля объектов

                                if(jsonArray.length() > 0){

                                    String foregroundApp = getForegroundApp();

                                    //Log.i(LOG_TAG, "foregroundApp: '" + foregroundApp + "'");

                                    for (int i = 0; i < jsonArray.length(); i++) {

                                        JSONObject jsonObject = jsonArray.getJSONObject(i);

                                        String appPackage = jsonObject.getString("name");
                                        boolean isActive = jsonObject.getBoolean("is_active");

                                        //Log.i(LOG_TAG, "appResult: '" + appPackage + "', is_active == " + isActive);

                                        if (!foregroundApp.equals(appPackage)) continue;

                                        if (!isActive) {

                                            watchdogConfig.removeFromWhitelist(appPackage);

                                            Log.i(LOG_TAG, appPackage + " is_active == false");

                                            // запускаем наш лаунчер
                                            launchPackage(watchdogConfig.targetPackage, watchdogConfig.targetPackageActivity);
                                            // чистим предыдущее приложение
                                            lastForegroundApp = checkProcessForKill(lastForegroundApp);
                                        }
                                    }
                                }

                            }catch (Exception e){
                                Log.e(LOG_TAG, e.getMessage());
                            }finally {
                                appsRequestInProcess = false;
                            }
                        },
                        (Response.ErrorListener) error -> {
                            Log.e(LOG_TAG, error.getMessage());

                            appsRequestInProcess = false;
                        }){

                    @Override
                    public Map<String, String> getHeaders() {

                        Map<String, String> params = new HashMap<>();
                        params.put("token", token);

                        return params;
                    }
                };

                requestQueue.add(stringRequest);

            }

        }, 0, 10, TimeUnit.SECONDS);
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {

        Log.d(LOG_TAG, "Watchdog service onStartCommand() " + intent.getAction());

        switch (intent.getAction()){
            // этот эвент прилетает если наш лаунчер стартует а вачдог уже работает (например если лаунчер обновился или лаунчер запущен вачдогом)
            case "com.viavr.intent.LAUNCHER_RUNNING":
                Log.i(LOG_TAG,"Reloading watchdog config...");

                // перезагружаем конфиг так как возможно он обновился (файл конфига ререзаписывается при каждом запуске лаунчера)
                watchdogConfig = readConfig(CONFIG_PATH);

                checkConfig(watchdogConfig);
                break;

            case "android.intent.action.MAIN":
            case "android.intent.action.BOOT_COMPLETED":
                // отрабатывает однократно в onCreate
                break;

            case "com.viavr.intent.WATCHDOG_COMMAND":
                // команды вачдогу через интент
                processIntentCommand(intent.getExtras());
                break;

            default:
                Log.e(LOG_TAG, "Unhandled onStartCommand action: " + intent.getAction());
                break;
        }

        return START_STICKY;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();

        if(scheduler != null)
            scheduler.cancel(true);

        Log.d(LOG_TAG, "Watchdog service onDestroy()");
    }

    @Nullable
    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    void initializeConfig(){
        watchdogConfig = readConfig(CONFIG_PATH);

        checkConfig(watchdogConfig);

        initRebootTime(watchdogConfig);
    }

    private void initRebootTime(WatchdogConfig watchdogConfig){
        Calendar calendar = Calendar.getInstance(Locale.getDefault());

        //выставляем время ребута, по умолчанию 6 часов утра
        calendar.set(Calendar.HOUR_OF_DAY, watchdogConfig.rebootAtHour);
        calendar.set(Calendar.MINUTE, 0);
        calendar.set(Calendar.SECOND, 0);

        // если время уже прошло то выставляем ребут на следующий день
        if(System.currentTimeMillis() > calendar.getTimeInMillis())
            calendar.add(Calendar.DAY_OF_MONTH, 1);

        targetRebootTime = calendar.getTimeInMillis();

        Log.i(LOG_TAG, "Next reboot at: " + DateFormat.format("dd.MM.yyyy HH:mm:ss", calendar));
    }

    boolean rebooting = false;

    private boolean checkRebootTime(){

        if(rebooting) return true;

        if(System.currentTimeMillis() >= targetRebootTime){
            rebooting = true;

            powerManager.reboot("");
        }

        return rebooting;
    }

    private void checkSendRuntimeTime(){
        if(token != null && System.currentTimeMillis() >= nextSendRuntimeMillis && !powerManager.isInteractive()){

            if(watchdogConfig.runtimeApiUrl == null || watchdogConfig.runtimeApiUrl.equals("")) return;

            try {
                String URL = watchdogConfig.runtimeApiUrl;

                JSONObject jsonBody = new JSONObject();

                jsonBody.put("battery", getBatteryLevel());
                jsonBody.put("firmware", getPackageVersion(watchdogConfig.targetPackage));
                jsonBody.put("charging", isDeviceCharging());

                final String requestBody = jsonBody.toString();

                StringRequest stringRequest = new StringRequest(Request.Method.POST, URL, new Response.Listener<String>() {
                    @Override
                    public void onResponse(String response) {}
                }, new Response.ErrorListener() {
                    @Override
                    public void onErrorResponse(VolleyError error) { Log.e(LOG_TAG, error.toString()); }
                }) {
                    @Override
                    public String getBodyContentType() {
                        return "application/json; charset=utf-8";
                    }

                    @Override
                    public byte[] getBody() {
                        return requestBody.getBytes(StandardCharsets.UTF_8);
                    }

                    @Override
                    public Map<String, String> getHeaders() {

                        Map<String, String> params = new HashMap<>();
                        params.put("token", token);

                        return params;
                    }
                };

                requestQueue.add(stringRequest);

            } catch (JSONException e) {
                e.printStackTrace();
            }

            nextSendRuntimeMillis = getNextSendRuntimeTime();
        }
    }

    private void checkWakeupTime(){
        if(System.currentTimeMillis() >= nextWakeupMillis){

            PowerManager.WakeLock wakeLock = powerManager.newWakeLock(PowerManager.ACQUIRE_CAUSES_WAKEUP | PowerManager.SCREEN_DIM_WAKE_LOCK, "helper:AlarmReceiver");

            wakeLock.acquire(60*1000L /*1 minute*/);
            wakeLock.release();

            Log.i(LOG_TAG, "WAKEUP!");

            nextWakeupMillis = getNextWakeupTime();
        }
    }

    // обработка кастомных команд которые прилетают с интентом com.viavr.intent.WATCHDOG_COMMAND
    void processIntentCommand(Bundle extras){

        if(watchdogConfig == null) return;

        for (String key : extras.keySet()) {

            String message = "";
            String value = (extras.get(key) != null ? extras.get(key).toString() : null);

            Log.i(LOG_TAG, "Processing intent key: " + key + " value: " + value);

            if(value == null) continue;

            switch (key){
                case "add_whitelist": // формат строки: com.random.package:60000 или массив: com.random.package:60000&com.another.package:30000
                    String[] apps = value.split("&");

                    for (String app : apps){
                        String[] splitted = app.split(":", 2);
                        if(splitted.length != 2) continue;

                        String packageName = splitted[0];
                        long timerValue = 0L;

                        try {
                            timerValue = Long.parseLong(splitted[1]);
                        }catch (NumberFormatException e){
                            continue;
                        }

                        watchdogConfig.addToWhitelist(packageName, timerValue);

                        message = "addToWhitelist: " + packageName + " for " + timerValue + "ms";
                        Log.i(LOG_TAG, message);
                    }
                    break;

                case "remove_whitelist":
                    watchdogConfig.removeFromWhitelist(value);

                    message = "removeFromWhitelist: " + value;
                    Log.i(LOG_TAG, message);
                    break;

                case "clear_whitelist":
                    watchdogConfig.clearWhitelist();

                    message = "clearWhitelist";
                    Log.i(LOG_TAG, message);
                    break;

                case "set_token":
                    token = value;

                    SharedPreferences.Editor editor = sharedPreferences.edit();
                    editor.putString("token", token);
                    editor.apply();

                    message = "set_token: " + value;
                    Log.i(LOG_TAG, message);
                    break;

                default:
                    message = "Unhandled key: " + key + " with value " + value;
                    Log.e(LOG_TAG, message);
                    break;
            }
        }
    }

    String checkProcessForKill(String backgroundApp){
        if(backgroundApp == null) return null;

        if(!backgroundApp.contains(watchdogConfig.targetPackage) && isProcessExists(backgroundApp))
        {
            killPackage(backgroundApp);
            return backgroundApp;
        }
        else
        {
            return null;
        }
    }

    // чек конфига
    void checkConfig(WatchdogConfig watchdogConfig){

        if(watchdogConfig == null){
            // ошибка при загрузке файла, создаем пустой конфиг по дефолту, запуск лаунчера и сентри отключены
            watchdogConfig = new WatchdogConfig();

            String message = "No valid config found, using default watchdog config\n" + watchdogConfig.toString();
            Log.e(LOG_TAG, message);
        }
        else if(watchdogConfig.errors != null){
            // какие-то поля распарсились криво
            String message = "Config created with errors!\nerrors: " + watchdogConfig.errors + "\n" + watchdogConfig.toString();
            Log.e(LOG_TAG, message);
        }
        else{
            // всё ок
            Log.i(LOG_TAG, "Watchdog config loaded without errors\n" + watchdogConfig.toString());
        }
    }

    // чтение и парсинг конфига из внутренней памяти
    WatchdogConfig readConfig(String relativeConfigFilePath){
        // корневая папка внутренней памяти
        String internalStorageRoot = Environment.getExternalStorageDirectory().getPath();

        File configFile = new File(internalStorageRoot, relativeConfigFilePath);

        if(!configFile.exists()){
            Log.e(LOG_TAG,"Config file '" + configFile.getAbsolutePath() + "' not exists!");
            return null;
        }

        StringBuilder text = new StringBuilder();

        Log.i(LOG_TAG,"Reading config '" + configFile.getAbsolutePath() + "'...");

        try {
            BufferedReader br = new BufferedReader(new FileReader(configFile));
            String line;

            while ((line = br.readLine()) != null) {
                text.append(line);
                text.append('\n');
            }
            br.close();
        }
        catch (IOException e) {
            Log.e(LOG_TAG,"Error reading '" + configFile.getAbsolutePath() + "': " + e.getMessage());
            return null;
        }

        Log.i(LOG_TAG,"Config '" + configFile.getAbsolutePath() + "' read success");

        try {
            return WatchdogConfig.create(new JSONObject(text.toString()));
        } catch (JSONException e) {
            Log.e(LOG_TAG,"Error parsing '" + configFile.getAbsolutePath() + "': " + e.getMessage());
            return null;
        }
    }

    // запуск приложения (в данном случае юзаем для запуска лаунчера)
    private void launchPackage(String packageToLaunch, String activityToLaunch){
        Intent launchIntent = new Intent(Intent.ACTION_MAIN);
        launchIntent.setComponent(new ComponentName(packageToLaunch, activityToLaunch));

        Log.i(LOG_TAG, "Launching package " + packageToLaunch);
        startActivity(launchIntent);
    }

    private void killPackage(String packageToKill){

        if(!watchdogConfig.allowKillApps || watchdogConfig.launcherKillWhitelist.contains(packageToKill)) return;

        Log.i(LOG_TAG, "Killing package " + packageToKill);

        ActivityManager activityManager = (ActivityManager)getSystemService(Activity.ACTIVITY_SERVICE);

        activityManager.killBackgroundProcesses(packageToKill); // убивает "приложуху" но не убивает процесс

        List<ActivityManager.RunningAppProcessInfo> runningAppProcesses = activityManager.getRunningAppProcesses();

        for(ActivityManager.RunningAppProcessInfo runningProInfo:runningAppProcesses){
            if(runningProInfo.processName.equals(packageToKill))
            {
                Log.i(LOG_TAG, "Killing pid " + runningProInfo.pid);
                // убиваем процесс. Без предварительного activityManager.killBackgroundProcesses не будет работать
                android.os.Process.killProcess(runningProInfo.pid);
            }
        }
    }

    private String getPackageVersion(String targetPackage){
        try {
            PackageInfo packageInfo = getPackageManager().getPackageInfo(targetPackage, 0);
            return packageInfo.versionName;
        } catch (PackageManager.NameNotFoundException e) {
            return "unknown";
        }
    }


    private int getBatteryLevel(){
        return batteryManager.getIntProperty(BatteryManager.BATTERY_PROPERTY_CAPACITY);
    }

    public boolean isDeviceCharging(){
        return batteryManager.isCharging();
    }

    ArrayList<String> runningProcesses = new ArrayList<>();

    // список процессов
    private ArrayList<String> getRunningProcesses(){
        ActivityManager activityManager = (ActivityManager)getSystemService(ACTIVITY_SERVICE);

        List<ActivityManager.RunningAppProcessInfo> runningAppProcesses = activityManager.getRunningAppProcesses();

        runningProcesses.clear();

        for(ActivityManager.RunningAppProcessInfo runningProInfo:runningAppProcesses){
            runningProcesses.add(runningProInfo.processName);
        }

        return runningProcesses;
    }

    private String getForegroundApp(){
        ActivityManager activityManager = (ActivityManager)getSystemService(ACTIVITY_SERVICE);

        ActivityManager.RunningTaskInfo foregroundTaskInfo = activityManager.getRunningTasks(1).get(0);

        if(foregroundTaskInfo == null ||
           foregroundTaskInfo.topActivity == null ||
           foregroundTaskInfo.topActivity.getPackageName() == null) return "NULL";

        return foregroundTaskInfo.topActivity.getPackageName();
    }

    // запущен ли процесс (у свёрнутого приложения процесс остается висеть)
    private boolean isProcessExists(String packageName){

        if(packageName == null || !packageName.contains(".")) return false;

        ArrayList<String> ps = getRunningProcesses();

        return ps != null && ps.contains(packageName);
    }

    // установлено ли приложения (в нашем случае лаунчер)
    private boolean isPackageInstalled(String packageName) {
        try {
            getPackageManager().getPackageInfo(packageName, 0);
            return true;
        } catch (PackageManager.NameNotFoundException e) {
            return false;
        }
    }

    private long getNextWakeupTime(){
        if(watchdogConfig == null) return System.currentTimeMillis() + 600 * 1000;

        long nextWakeup = System.currentTimeMillis() + (watchdogConfig.wakeupEverySeconds * 1000L) + random.nextInt(watchdogConfig.wakeupRandomSeconds * 1000);

        Calendar calendar = Calendar.getInstance();
        calendar.setTimeInMillis(nextWakeup);

        Log.i(LOG_TAG, "NextWakeupTime: " + DateFormat.format("dd.MM.yyyy HH:mm:ss", calendar));

        return nextWakeup;
    }

    private long getNextSendRuntimeTime(){
        if(watchdogConfig == null) return System.currentTimeMillis() + 600 * 1000;

        long nextWakeup = System.currentTimeMillis() + (watchdogConfig.sendRuntimeEverySeconds * 1000L) + random.nextInt(watchdogConfig.sendRuntimeRandomSeconds * 1000);

        Calendar calendar = Calendar.getInstance();
        calendar.setTimeInMillis(nextWakeup);

        Log.i(LOG_TAG, "NextRuntimeTime: " + DateFormat.format("dd.MM.yyyy HH:mm:ss", calendar));

        return nextWakeup;
    }
}