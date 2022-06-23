using System;
using System.IO;
using System.Threading.Tasks;
using Cli;
using KeeperSecurity.Configuration;
using System.Data.SQLite;
using KeeperSecurity.OfflineStorage.Sqlite;
using KeeperSecurity.Authentication.Async;
using KeeperSecurity.Utils;
using KeeperSecurity.Enterprise;
using System.Linq;

namespace SharedFolderPermissions
{

    public class CallbackJsonLoader : IJsonConfigurationLoader
    {
        private readonly Func<byte[]> _loader;
        private readonly Action<byte[]> _storer;

        public CallbackJsonLoader(Func<byte[]> loader, Action<byte[]> storer)
        {
            _loader = loader;
            _storer = storer;
        }
        public byte[] LoadJson()
        {
            return _loader();
        }

        public void StoreJson(byte[] json)
        {
            _storer(json);
        }
    }

    internal partial class MainMenuCliContext : StateCommands
    {
        private string _databaseFile;

        public const string OwnerColumnName = "Name";

        public MainMenuCliContext()
        {
            var keeperLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".keeper");

            if (!Directory.Exists(keeperLocation))
            {
                Directory.CreateDirectory(keeperLocation);
            }

            _databaseFile = Path.Combine(keeperLocation, "keeper.db");

            using (var connection = new SQLiteConnection($"Data Source={_databaseFile};"))
            {
                connection.Open();
                var tables = new[] { typeof(EnterpriseInfo), typeof(EnterpriseUser) };
                var isValid = DatabaseUtils.VerifyDatabase(true, connection,
                    tables.Select(x => new TableSchema(x, OwnerColumnName)), null);
                if (!isValid)
                {
                    throw new Exception("Cannot create database");
                }
            }

            Commands.Add("sync",
                new SimpleCommand
                {
                    Order = 10,
                    Description = "connects to Keeper Server and gets enterprise users.",
                    Action = ExecuteSyncCommand,
                });
        }

        private async Task ExecuteSyncCommand(string _)
        {
            await using var connection = new SQLiteConnection($"Data Source={_databaseFile};");
            connection.Open();

            var owner = Tuple.Create<string, object>(OwnerColumnName, "default");

            var userStorage = new SqliteRecordStorage<EnterpriseInfo>(() => connection, owner);
            var info = userStorage.Get();
            if (info == null)
            {
                info = new EnterpriseInfo();
            }

            var jc = new JsonConfigurationCache(
                new CallbackJsonLoader(
                    () =>
                    {
                        if (!string.IsNullOrEmpty(info.KeeperConfiguration))
                            return System.Text.Encoding.UTF8.GetBytes(info.KeeperConfiguration);
                        return null;
                    },
                    (x) => info.KeeperConfiguration = System.Text.Encoding.UTF8.GetString(x))
                );
            var storage = new JsonConfigurationStorage(jc);
            using var auth = new Auth(new ConsoleAuthUi(Program.GetInputManager()), storage)
            {
                Endpoint = { DeviceName = "Enterprise Sync", ClientVersion = "c16.1.0" }
            };

            var server = storage.LastServer;
            if (string.IsNullOrEmpty(server))
            {
                Console.Write("Keeper Server (keepersecurity.com): ");
                server = await Program.GetInputManager().ReadLine();
                auth.Endpoint.Server = server;
            }

            var loginName = storage.LastLogin;
            if (string.IsNullOrEmpty(loginName))
            {
                Console.Write("Keeper username: ");
                loginName = await Program.GetInputManager().ReadLine();
            }
            await auth.Login(loginName);

            if (!auth.AuthContext.IsEnterpriseAdmin)
            {
                throw new Exception("Not Enterprise Admin");
            }

            byte[] token = string.IsNullOrEmpty(info.ContinuationToken) ? new byte[0] : info.ContinuationToken.Base64UrlDecode();

            var plugin = new EnterpriseUserPlugin(() => connection, owner);
            var loader = new EnterpriseLoader(auth, new[] { plugin })
            {
                ContinuationToken = token
            };
            await loader.Load();
            info.ContinuationToken = loader.ContinuationToken.Base64UrlEncode();
            userStorage.Put(info);
        }
        public override string GetPrompt()
        {
            return "Main Menu";
        }
    }
}