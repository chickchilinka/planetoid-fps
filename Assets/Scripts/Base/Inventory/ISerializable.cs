namespace Inventory
{
    public interface ISerializable<TData>
    {
        TData GetData();
        void SetData(TData data);
    }
}
