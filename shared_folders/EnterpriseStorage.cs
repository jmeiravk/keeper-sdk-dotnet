using Enterprise;
using Google.Protobuf;
using KeeperSecurity.Enterprise;
using KeeperSecurity.OfflineStorage.Sqlite;
using KeeperSecurity.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace SharedFolderPermissions
{
    [SqlTable(Name = "EnterpriseContext")]
    public class EnterpriseInfo
    {
        [SqlColumn]
        public string ContinuationToken { get; set; }
        [SqlColumn]
        public string KeeperConfiguration { get; set; }
    }


    [SqlTable(Name = "Users", PrimaryKey = new[] { "EnterpriseUserId" }, Index1 = new[] { "Username" })]
    public class EnterpriseUser
    {
        [SqlColumn]
        public long EnterpriseUserId { get; set; }

        [SqlColumn]
        public long NodeId { get; set; }

        [SqlColumn]
        public string EncryptedData { get; set; }

        [SqlColumn]
        public string KeyType { get; set; }

        [SqlColumn]
        public string Username { get; set; }

        [SqlColumn]
        public string Status { get; set; }

        [SqlColumn]
        public int Lock { get; set; }

        [SqlColumn]
        public int UserId { get; set; }

        [SqlColumn]
        public long AccountShareExpiration { get; set; }

        [SqlColumn]
        public string FullName { get; set; }

        [SqlColumn]
        public string JobTitle { get; set; }
    }

    public abstract class EnterpriseSqliteEntity<TD, TK> : SqliteDataStorage<TD>, IKeeperEnterpriseEntity
        where TD : class, new()
        where TK : IMessage<TK>
    {
        protected string EntityColumnName { get; }
        private readonly MessageParser<TK> _parser;

        public EnterpriseSqliteEntity(EnterpriseDataEntity dataEntity, Func<IDbConnection> getConnection, Tuple<string, object> owner)
            : base(getConnection, owner)
        {
            DataEntity = dataEntity;

            EntityColumnName = Schema.PrimaryKey[0];

            var keeperType = typeof(TK);
            var parser = keeperType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
            if (parser == null) throw new Exception($"Cannot get Parser for {keeperType.Name} Google Profobuf class");
            _parser = (MessageParser<TK>)(parser.GetMethod.Invoke(null, null));

        }

        public EnterpriseDataEntity DataEntity { get; }

        public void Clear()
        {
            var cmd = GetDeleteStatement();
            using var txn = GetConnection().BeginTransaction();
            cmd.Transaction = txn;
            cmd.ExecuteNonQuery();
            txn.Commit();
        }

        private void DeleteEntities(IEnumerable<long> entityIds)
        {
            var cmd = GetDeleteStatement(new[] { EntityColumnName });
            var entityParameter = (IDbDataParameter)cmd.Parameters[$"@{EntityColumnName}"];
            using (var txn = GetConnection().BeginTransaction())
            {
                cmd.Transaction = txn;
                foreach (var entityId in entityIds)
                {
                    entityParameter.Value = entityId;
                    cmd.ExecuteNonQuery();
                }
                txn.Commit();
            }
        }

        public void PutEntities(IEnumerable<TD> entities)
        {
            var cmd = GetPutStatement();
            using (var txn = GetConnection().BeginTransaction())
            {
                cmd.Transaction = txn;
                foreach (var entity in entities)
                {
                    PopulateCommandParameters(cmd, entity);
                    cmd.ExecuteNonQuery();
                }

                txn.Commit();
            }
        }

        protected TK Parse(ByteString data)
        {
            return _parser.ParseFrom(data);
        }

        protected abstract long GetEntityId(TK keeperData);
        protected abstract TD FromKeeper(TK keeper);


        public void ProcessKeeperEnterpriseData(Enterprise.EnterpriseData entityData)
        {
            var toDelete = new List<long>();
            var toPut = new List<TD>();

            foreach (var data in entityData.Data)
            {
                var keeperEntity = Parse(data);
                var id = GetEntityId(keeperEntity);
                if (entityData.Delete)
                {
                    toDelete.Add(id);
                }
                else
                {
                    var db = new TD();
                    db = FromKeeper(keeperEntity);
                    toPut.Add(db);
                }
            }

            if (toDelete.Count > 0)
            {
                DeleteEntities(toDelete);
            }

            if (toPut.Count > 0)
            {
                PutEntities(toPut);
            }
        }
    }

    public class EnterpriseUserEntity : EnterpriseSqliteEntity<EnterpriseUser, User>
    {
        public EnterpriseUserEntity(Func<IDbConnection> getConnection, Tuple<string, object> owner)
            : base(EnterpriseDataEntity.Users, getConnection, owner)
        { }

        protected override long GetEntityId(User keeperData)
        {
            return keeperData.EnterpriseUserId;
        }

        protected override EnterpriseUser FromKeeper(User keeper)
        {
            return new EnterpriseUser
            {
                EnterpriseUserId = keeper.EnterpriseUserId,
                NodeId = keeper.NodeId,
                EncryptedData = keeper.EncryptedData,
                KeyType = keeper.KeyType,
                Username = keeper.Username,
                Status = keeper.Status,
                Lock = keeper.Lock,
                UserId = keeper.UserId,
                AccountShareExpiration = keeper.AccountShareExpiration,
                FullName = keeper.FullName,
                JobTitle = keeper.JobTitle,
            };
        }
    }

    public class EnterpriseUserPlugin : EnterpriseDataPlugin
    {
        public readonly EnterpriseUserEntity _users;
        public EnterpriseUserPlugin(Func<IDbConnection> getConnection, Tuple<string, object> owner)
        {
            _users = new EnterpriseUserEntity(getConnection, owner);
            Entities = new[] { _users };
        }
        public override IEnumerable<IKeeperEnterpriseEntity> Entities { get; }
    }
}
