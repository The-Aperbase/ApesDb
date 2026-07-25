namespace ApesDb.Api;

public static class ApiRoutes
{
    public static class Api
    {
        public const string Prefix = "api";
    }

    public static class Games
    {
        public const string Get = "games";
        public const string ById = "games/{id:long}";
        public const string Top = "games/top";
        public const string Types = "games/types";
        public const string Statuses = "games/statuses";
        public const string Genres = "games/genres";
        public const string Themes = "games/themes";
        public const string Modes = "games/modes";
        public const string PlayerPerspectives = "games/player-perspectives";
        public const string Platforms = "games/platforms";
    }

    public static class Auth
    {
        public const string Prefix = "auth";
        public const string Login = "login";
        public const string Logout = "logout";
        public const string Me = "me";
    }

    public static class Notifications
    {
        public const string Get = "notifications";
        public const string Read = "notifications/read";
        public const string Stream = "notifications/stream";
    }
}
