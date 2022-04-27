using System;
using System.Collections.Generic;
using Maple2.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Maple2.Database.Storage;

public abstract class DatabaseRequest<TContext> : IDisposable where TContext : DbContext {
    protected readonly TContext context;
    protected readonly ILogger logger;
    
    private IDbContextTransaction transaction;
    public bool IsTransaction => transaction != null;

    public DatabaseRequest(TContext context, ILogger logger) {
        this.context = context;
        this.logger = logger;
    }

    public void BeginTransaction() {
        transaction = context.Database.BeginTransaction();
    }

    public bool Commit() {
        if (!IsTransaction) {
            return false;
        }
        
        transaction.Commit();
        return true;
    }

    public bool SaveChanges() {
        return context.TrySaveChanges();
    }
    
    public void Dispose() {
        Commit();
        context.Dispose();
    }
}
