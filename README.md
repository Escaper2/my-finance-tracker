# my-finance-tracker

Личный финансовый трекер: учёт доходов и расходов, бюджеты и отчёты.

## Стек

C#, .NET 10, Blazor Interactive Server, MudBlazor и EF Core.
По умолчанию используется SQLite; также предусмотрена поддержка PostgreSQL.

## Возможности

- Управление счетами и категориями.
- Добавление, редактирование и удаление операций.
- Фильтрация, поиск, сортировка и пагинация.
- Месячные бюджеты и контроль расходов.
- Отчёты и графики.
- Импорт CSV с предпросмотром, обработкой ошибок и защитой от дублей.
- Уведомления с историей и отметкой о прочтении.

## Запуск

Требуется .NET 10 SDK.

- Склонируйте проект
- Запустите приложение:

    ```bash
    dotnet run --project Web/FinanceTracker.Web
    ```

- Для запуска с демоданными:
  
  ```bash
  dotnet run --project Web/FinanceTracker.Web -- "--ConnectionStrings:Finance=Data Source=App_Data/demo.db" --Database:SeedDemo=true
  ```

- Откройте http://localhost:5180/.
