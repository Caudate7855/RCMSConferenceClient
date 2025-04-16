Сурс: https://github.com/picoxr/AndroidHelper

Добавлена поддержка shell команд системы Android (Примеры: https://gist.github.com/Pulimet/5013acf2cd5b28e55036c82c91bd56d8 - команды, начинающиеся с 'adb shell', при использовании библиотеки 'adb shell' писать не надо)

Билдить лучше через IntelliJ IDEA Community Edition, т.к. Android Studio качает компоненты и насильно обновляет SDK и gradle в папке юнити что может привести к непредсказуемым результатам.

После импорта проекта IntelliJ IDEA градл синхронизируется с проектом,  будет предложено его обновить и т.д., все предложения не принимать.

Для билда нажать Build, для последующих билдов Build > Rebuild.

Сбилженная aar библиотека будет распологаться в папке проекта по пути AndroidDeviceHelperCustom/lib/build/outputs/aar/