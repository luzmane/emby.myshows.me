# emby.myshows.me

Scrob tvshows to MyShows.me. This site is popular in the Russian-speaking community and contains almost no English-language information so further description will be in Russian.

Плагин для Emby для скроббинга сериалов на сайт MyShows.me. Оригинал [jellyfin-plugin-myshows](https://github.com/shemanaev/jellyfin-plugin-myshows) был написан для Jellyfin. Это портирование для Emby.

Если что-то не работает - смело создавай новый issue. Я не пользуюсь плагином 24/7 - могу и не знать о сломавшейся функциональности.

## Установка

* Положить dll в папку plugins.

## Настройка

* Параметры плагина искать в "Управление Emby Server" - Расширенное - Плагины - вкладка "Мои плагины (My Plugins)" - MyShows.Me.
* Для корректной работы плагина необходима регистрация на сайте [myShows.me](https://myShows.me) ( что не удивительно :wink: ).

## Использование
* Плагин поддерживает несколько пользователей
* Конфигурация пользователей происходит в настройках плагина (должен быть доступ)

## Требования

* Плагин тестировался на версии 4.8.11
* Собирался c .Net 7.0 для .NetStandard 2.0
