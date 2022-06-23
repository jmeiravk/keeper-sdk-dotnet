using KeeperSecurity.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sample
{
    [SqlTable(Name = "EnterpriseContext")]
    public class EnterpriseInfo
    {
        public string ContinuationToken { get; set; }
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


}
