using Npgsql;
using Npgsql.Internal;

namespace Utilities;


public class ObservableTransaction : IAsyncDisposable, IDisposable {

    private NpgsqlTransaction _underliedTransaction;
    private NpgsqlConnection _underliedConnection;

    public NpgsqlTransaction Transaction {
        get => _underliedTransaction;
    }
    public NpgsqlConnection Connection {
        get => _underliedConnection;
    }

    private event EventHandler? CommitEvent;
    private event EventHandler? RollbackEvent;

    public ObservableTransaction (NpgsqlTransaction underlying, NpgsqlConnection conn){
        _underliedTransaction = underlying;
        _underliedConnection = conn;
    }

    public void Capture(){
        //Monitor.Enter(this);
        //Console.WriteLine("Поток "+ Thread.CurrentThread.Name +" получил Монитор");
    }

    public void Release(){
        //Monitor.Exit(this);
        //Console.WriteLine("Поток "+ Thread.CurrentThread.Name +" покинул Монитор");

    }


    public void OnCommitSubscribe(EventHandler action){
        CommitEvent+=action;   
    }
    public void OnRollbackSubscribe(EventHandler action){
        RollbackEvent += action;
    } 

    public async Task CommitAsync(){
        await _underliedTransaction.CommitAsync();
        CommitEvent?.Invoke(this, new EventArgs());
    }
    public async Task RollbackAsync(){
        await _underliedTransaction.RollbackAsync();
        RollbackEvent?.Invoke(this, new EventArgs());
    }

    public ValueTask DisposeAsync()
    {
        return _underliedTransaction.DisposeAsync();
    }

    public void Dispose()
    {
        _underliedTransaction.Dispose(); 
    }
}