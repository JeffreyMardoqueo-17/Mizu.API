using System;
using System.Data;
using Muzu.Api.Core.Interfaces;

namespace Muzu.Api.Core.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }
        ITenantRepository TenantRepo { get; }
        IUsuarioRepository UsuarioRepo { get; }
        ITenantConfigRepository TenantConfigRepo { get; }
        void Begin();
        void Commit();
        void Rollback();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ITenantRepository _tenantRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly ITenantConfigRepository _tenantConfigRepo;
        private IDbConnection? _connection;
        private IDbTransaction? _transaction;

        public UnitOfWork()
        {
            _tenantRepo = new TenantRepository();
            _usuarioRepo = new UsuarioRepository();
            _tenantConfigRepo = new TenantConfigRepository();
        }

        public IDbConnection Connection => _connection ?? DbConnectionFactory.Instance.CreateConnection();
        
        public IDbTransaction Transaction => _transaction ?? throw new InvalidOperationException("Transaction not started. Call Begin() first.");

        public ITenantRepository TenantRepo => _tenantRepo;
        public IUsuarioRepository UsuarioRepo => _usuarioRepo;
        public ITenantConfigRepository TenantConfigRepo => _tenantConfigRepo;

        public void Begin()
        {
            _connection = DbConnectionFactory.Instance.CreateConnection();
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }

        public void Commit()
        {
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;
            _connection?.Close();
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
            _connection?.Close();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
    }
}
