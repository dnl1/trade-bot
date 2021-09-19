namespace TradeBot.Repositories
{
    internal interface ISnapshotRepository
    {
        Snapshot Get(string symbol);
        void Save(Snapshot snapshot);
        IEnumerable<Snapshot> GetAll();
    }
}