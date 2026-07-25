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
        public const string ById = $"{Get}/{{id:long}}";
        public const string Top = $"{Get}/top";
        public const string Types = $"{Get}/types";
        public const string Statuses = $"{Get}/statuses";
        public const string Genres = $"{Get}/genres";
        public const string Themes = $"{Get}/themes";
        public const string Modes = $"{Get}/modes";
        public const string PlayerPerspectives = $"{Get}/player-perspectives";
        public const string Platforms = $"{Get}/platforms";
    }

    public static class Boards
    {
        public const string List = "boards";
        public const string Create = List;
        public const string ById = $"{List}/{{boardId:guid}}";
        public const string Entries = $"{ById}/entries";
        public const string EntryByGame = $"{Entries}/{{gameId:long}}";
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
        public const string Read = $"{Get}/read";
        public const string Stream = $"{Get}/stream";
    }
}
