using System.Data.Common;

namespace GDB.App.Infrastructure.Repositories.Contracts;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
